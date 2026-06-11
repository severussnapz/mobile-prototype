using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Configuration;
using Genesis.AI.Infrastructure.Services;
using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Genesis.AI.Api.Features.Conversations;

[ApiController]
[Route("api/v1/conversations")]
[Authorize(Policy = AuthorisationPolicies.ConversationWrite)]
[Produces("application/json")]
[Consumes("application/json")]
public class ConversationStreamController : ControllerBase
{
    private const int ToolExecutionRetryCount = 2;
    private const int Pipeline02CompletionPhaseNumber = 6;
    private const string PrototypeHtmlArtefactPath = "prototype/index.html";
    private const string PrototypeNotesArtefactPath = "prototype/PROTOTYPE_NOTES.md";

    private readonly IConversationRepository _conversationRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IAiService _aiService;
    private readonly IPromptService _promptService;
    private readonly ISkillContentService _skillContentService;
    private readonly IFoundationService _foundationService;
    private readonly IPrototypeAssemblyService _prototypeAssemblyService;
    private readonly TokenOptimisationOptions _tokenOptimisationOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ConversationStreamController> _logger;

    public ConversationStreamController(
        IConversationRepository conversationRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IAiService aiService,
        IPromptService promptService,
        ISkillContentService skillContentService,
        IFoundationService foundationService,
        IPrototypeAssemblyService prototypeAssemblyService,
        IOptions<TokenOptimisationOptions> tokenOptimisationOptions,
        TimeProvider timeProvider,
        ILogger<ConversationStreamController> logger)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _skillContentService = skillContentService ?? throw new ArgumentNullException(nameof(skillContentService));
        _foundationService = foundationService ?? throw new ArgumentNullException(nameof(foundationService));
        _prototypeAssemblyService = prototypeAssemblyService ?? throw new ArgumentNullException(nameof(prototypeAssemblyService));
        _tokenOptimisationOptions = tokenOptimisationOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptimisationOptions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a user message and streams the AI response via Server-Sent Events.
    /// </summary>
    [HttpPost("{id:guid}/stream")]
    [Produces("text/event-stream")]
    public async Task StreamAiResponse(Guid id, [FromBody] StreamMessageRequest request, CancellationToken cancellationToken)
    {
        await ExecuteStreamAsync(id, request, cancellationToken);
    }

    private async Task ExecuteStreamAsync(Guid id, StreamMessageRequest request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(id, cancellationToken);

        if (conversation is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // Runtime stage-type authorisation check
        var stageType = await _conversationRepository.GetStageTypeByStageIdAsync(conversation.StageId, cancellationToken);
        if (stageType is not null && !User.CanConverseOnStage(stageType.Value))
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                "403",
                "Insufficient scope",
                $"You do not have permission to converse on {stageType.Value} stages."), cancellationToken);
            return;
        }

        // Add the user message (skip on retry — message already persisted from the original attempt)
        if (!request.Retry)
        {
            var userErn = User.GetUserErn();
            var givenName = User.GetGivenName();
            var familyName = User.GetFamilyName();
            var messageImages = request.Images?.Select(image => new MessageImage
            {
                Data = image.Data,
                MediaType = image.MediaType
            }).ToList();
            var messageDocuments = request.Documents?.Select(document => new MessageDocument
            {
                Data = document.Data,
                MediaType = document.MediaType,
                FileName = document.FileName
            }).ToList();
            conversation.AddMessage(MessageRole.User, request.Content, null, _timeProvider, userErn, givenName, familyName, messageImages, messageDocuments);
            await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Build message history for AI
        var messages = conversation.Messages
            .OrderBy(message => message.CreatedAt)
            .Select(message => new AiMessage(
                message.Role,
                message.Content,
                Images: message.Images?.Select(image => new AiImageContent(image.Data, image.MediaType)).ToList(),
                Documents: message.Documents?.Select(document => new AiDocumentContent(document.Data, document.MediaType, document.FileName)).ToList()))
            .ToList();

        // Configure SSE response
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        // Resolve the system prompt from the stage type (stageType already loaded for auth check above)
        var basePrompt = stageType is not null
            ? _promptService.GetSystemPrompt(stageType.Value)
            : GetFallbackPrompt();

        // Inject current API-managed state into the system prompt
        var conversationWithParkingLot = await _conversationRepository.GetByIdWithParkingLotAsync(id, cancellationToken);
        var projectContext = await _conversationRepository.GetProjectContextByStageIdAsync(conversation.StageId, cancellationToken);
        var projectId = projectContext?.ProjectId ?? Guid.Empty;

        // Load project-level parking lot (all conversations) for context and deduplication
        var projectParkingLotItems = await _conversationRepository.GetParkingLotByProjectIdAsync(projectId, cancellationToken);

        var stateContext = BuildStateContext(conversationWithParkingLot!, projectParkingLotItems);

        // Inject project context (name, code, description, compliance domain)
        var projectContextSection = BuildProjectContext(projectContext);

        // Build lightweight artefact manifest (file paths + versions only — LLM uses tools to read content)
        var artefactManifest = await _artefactRepository.GetProjectArtefactManifestAsync(projectId, cancellationToken);
        var artefactManifestSection = BuildArtefactManifest(artefactManifest);

        // Detect staleness: were artefacts modified since the last message in this conversation?
        var stalenessNotice = string.Empty;
        if (conversation.Messages.Count > 0)
        {
            var lastMessageTime = conversation.Messages.Max(message => message.CreatedAt);
            var latestArtefactTime = await _artefactRepository.GetLatestArtefactTimestampAsync(projectId, cancellationToken);
            if (latestArtefactTime.HasValue && latestArtefactTime.Value > lastMessageTime)
            {
                stalenessNotice = "\n\n⚠️ **ARTEFACTS UPDATED:** Project artefacts have been modified since your last message in this conversation. " +
                                  "Use `list_artefacts` and `get_artefact` to reload any files you previously referenced — they may have changed.";
            }
        }

        // Build split AiSystemPrompt: stable part (cached) + mutable part (fresh each turn)
        AiSystemPrompt aiSystemPrompt;
        if (_tokenOptimisationOptions.FoundationPrefixEnabled && stageType.HasValue)
        {
            var foundationContent = await _foundationService.BuildFoundationContentAsync(
                projectId, stageType.Value, cancellationToken);

            var stablePart = string.IsNullOrEmpty(foundationContent)
                ? basePrompt
                : $"{basePrompt}\n\n---\n\n{foundationContent}";

            var mutablePart = $"## PROJECT CONTEXT (from project creation)\n\n{projectContextSection}\n\n---\n\n## CURRENT SESSION STATE (managed by API)\n\n{stateContext}";

            if (!string.IsNullOrEmpty(artefactManifestSection))
            {
                mutablePart += $"\n\n---\n\n## PROJECT ARTEFACTS (live manifest — use get_artefact for unlisted files)\n\n{artefactManifestSection}";
            }

            mutablePart += stalenessNotice;

            aiSystemPrompt = new AiSystemPrompt(StablePart: stablePart, MutablePart: mutablePart);

            _logger.LogInformation(
                "Foundation prefix active for stage {StageType}: stable part {StableChars} chars, mutable part {MutableChars} chars",
                stageType.Value,
                stablePart.Length,
                mutablePart.Length);
        }
        else
        {
            // Foundation prefix disabled — legacy single-prompt path
            var systemPrompt = $"{basePrompt}\n\n---\n\n## PROJECT CONTEXT (from project creation)\n\n{projectContextSection}\n\n---\n\n## CURRENT SESSION STATE (managed by API)\n\n{stateContext}";

            if (!string.IsNullOrEmpty(artefactManifestSection))
            {
                systemPrompt += $"\n\n---\n\n## PROJECT ARTEFACTS (use get_artefact tool to read content)\n\n{artefactManifestSection}";
            }

            systemPrompt += stalenessNotice;

            aiSystemPrompt = AiSystemPrompt.FromFullPrompt(systemPrompt);
        }

        var fullResponse = new System.Text.StringBuilder();
        var savedArtefacts = new List<Artefact>();
        var savedParkingLotItems = new List<ParkingLotItem>();
        var createdBy = User.GetUserErn() ?? User.FindFirstValue("sub") ?? "system";

        // Mutable message list for multi-turn tool use
        var aiMessages = new List<AiMessage>(messages);
        var totalInputTokens = 0;
        var totalOutputTokens = 0;

        try
        {
            const int defaultMaxToolTurns = 40; // Safety limit to prevent infinite tool loops (needs headroom for Phase 11 saving 15+ files one-at-a-time)
            var maxToolTurns = stageType.HasValue &&
                _tokenOptimisationOptions.StageToolTurnLimits.TryGetValue(stageType.Value.ToString(), out var stageLimit)
                ? stageLimit
                : defaultMaxToolTurns;
            var turnsRemaining = maxToolTurns;

            while (turnsRemaining > 0)
            {
                var toolCallsThisTurn = new List<AiToolCall>();
                var turnText = new StringBuilder(); // Text produced in THIS turn only

                // Insert a newline between turns so post-tool text doesn't run into pre-tool text
                var needsNewlineSeparator = fullResponse.Length > 0 && fullResponse[^1] != '\n';

                await foreach (var streamEvent in _aiService.StreamWithToolsAsync(
                    aiSystemPrompt, aiMessages, PipelineToolDefinitions.GetTools(_tokenOptimisationOptions), cancellationToken))
                {
                    switch (streamEvent)
                    {
                        case AiTextChunk textChunk:
                            var text = textChunk.Text;
                            if (needsNewlineSeparator)
                            {
                                needsNewlineSeparator = false;
                                text = "\n" + text;
                            }
                            fullResponse.Append(text);
                            turnText.Append(text);
                            var eventData = JsonSerializer.Serialize(new { text });
                            await Response.WriteAsync($"data: {eventData}\n\n", cancellationToken);
                            await Response.Body.FlushAsync(cancellationToken);
                            break;

                        case AiToolCall toolCall:
                            toolCallsThisTurn.Add(toolCall);
                            break;

                        case AiStreamError streamError:
                            var errorEventData = JsonSerializer.Serialize(new { error = streamError.Message, reason = streamError.Reason });
                            await Response.WriteAsync($"event: error\ndata: {errorEventData}\n\n", cancellationToken);
                            await Response.Body.FlushAsync(cancellationToken);
                            break;

                        case AiTokenUsage tokenUsage:
                            totalInputTokens += tokenUsage.InputTokens;
                            totalOutputTokens += tokenUsage.OutputTokens;
                            conversation.RecordTokenUsage(
                                tokenUsage.InputTokens,
                                tokenUsage.OutputTokens,
                                tokenUsage.CacheReadInputTokens,
                                tokenUsage.CacheWriteInputTokens,
                                _timeProvider);
                            await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
                            var usageEventData = JsonSerializer.Serialize(new
                            {
                                inputTokens = tokenUsage.InputTokens,
                                outputTokens = tokenUsage.OutputTokens,
                                totalTokens = tokenUsage.TotalTokens,
                                cacheReadInputTokens = tokenUsage.CacheReadInputTokens,
                                cacheWriteInputTokens = tokenUsage.CacheWriteInputTokens,
                                cumulativeInputTokens = totalInputTokens,
                                cumulativeOutputTokens = totalOutputTokens
                            });
                            await Response.WriteAsync($"event: usage\ndata: {usageEventData}\n\n", cancellationToken);
                            await Response.Body.FlushAsync(cancellationToken);
                            break;

                        case AiTurnComplete:
                            // Handled below after stream completes
                            break;
                    }
                }

                // If no tool calls were made, the AI is done
                if (toolCallsThisTurn.Count == 0)
                    break;

                // Execute all tool calls and collect results
                var toolResults = new List<AiToolResult>();

                foreach (var toolCall in toolCallsThisTurn)
                {
                    await SendToolStartSseEventAsync(toolCall, cancellationToken);

                    string result;
                    try
                    {
                        result = await ExecuteToolCallWithRetryAsync(
                            toolCall,
                            conversation,
                            savedArtefacts,
                            savedParkingLotItems,
                            projectParkingLotItems,
                            createdBy,
                            projectId,
                            stageType,
                            cancellationToken);
                    }
                    catch (ToolExecutionFailedException exception)
                    {
                        _logger.LogError(
                            exception,
                            "Tool failure with fail-closed policy for conversation {ConversationId}: {ToolName}",
                            conversation.Id,
                            toolCall.ToolName);

                        var toolErrorEvent = JsonSerializer.Serialize(new
                        {
                            error = "Tool execution failed",
                            reason = exception.Message,
                            tool = toolCall.ToolName,
                            retryCount = ToolExecutionRetryCount + 1
                        });

                        await Response.WriteAsync($"event: error\ndata: {toolErrorEvent}\n\n", cancellationToken);
                        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                        return;
                    }

                    toolResults.Add(new AiToolResult(toolCall.ToolUseId, result));

                    await SendToolSseEventAsync(toolCall, conversation, savedArtefacts, savedParkingLotItems, cancellationToken);
                }

                // Build proper structured continuation messages for the Bedrock API.
                // Assistant message: text (if any) + tool_use blocks
                var assistantText = turnText.ToString();
                aiMessages.Add(new AiMessage(
                    MessageRole.Assistant,
                    assistantText,
                    ToolCalls: toolCallsThisTurn));

                // User message: tool_result blocks
                aiMessages.Add(new AiMessage(
                    MessageRole.User,
                    string.Empty,
                    ToolResults: toolResults));

                turnsRemaining--;

                // Near-limit telemetry: warn when only 5 turns remain so the user can safely checkpoint
                if (turnsRemaining == 5)
                {
                    _logger.LogWarning(
                        "Tool loop near limit for conversation {ConversationId}, stage {StageType}, requirement {RequirementId}: {TurnsUsed}/{MaxTurns} turns used",
                        conversation.Id, stageType, conversation.RequirementId ?? "(none)", maxToolTurns - turnsRemaining, maxToolTurns);

                    var nearLimitEventData = JsonSerializer.Serialize(new
                    {
                        turnsUsed = maxToolTurns - turnsRemaining,
                        turnsRemaining,
                        conversationId = conversation.Id,
                        requirementId = conversation.RequirementId
                    });
                    await Response.WriteAsync($"event: near_limit\ndata: {nearLimitEventData}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }

            // Hard-limit telemetry: loop exited without the AI finishing — response was cut off
            if (turnsRemaining == 0)
            {
                _logger.LogError(
                    "Tool loop hard limit hit for conversation {ConversationId}, stage {StageType}, requirement {RequirementId}: all {MaxTurns} turns exhausted — response was cut off",
                    conversation.Id, stageType, conversation.RequirementId ?? "(none)", maxToolTurns);

                var limitHitEventData = JsonSerializer.Serialize(new
                {
                    turnsUsed = maxToolTurns,
                    conversationId = conversation.Id,
                    requirementId = conversation.RequirementId
                });
                await Response.WriteAsync($"event: tool_limit_hit\ndata: {limitHitEventData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            // Store the full AI text response (skip if AI only produced tool calls with no text)
            var finalResponse = fullResponse.ToString();
            if (!string.IsNullOrWhiteSpace(finalResponse))
            {
                conversation.AddMessage(MessageRole.Assistant, finalResponse, null, _timeProvider);
                await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Send completion event
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stream cancelled for conversation {ConversationId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming AI response for conversation {ConversationId}", id);
            var errorData = System.Text.Json.JsonSerializer.Serialize(new { error = "AI generation failed" });
            await Response.WriteAsync($"data: {errorData}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    private static string GetFallbackPrompt()
    {
        return """
            You are an AI requirements analyst helping to discover and document software requirements.
            Ask focused questions one at a time to understand the product being specified.
            Cover four dimensions for each requirement: clinical safety, information governance, security, and observability.
            Be concise and professional. Use the save_artefact tool when you have complete file content ready.
            Use advance_phase when transitioning between interview phases.
            Use add_parking_lot_item for topics to revisit later.
            """;
    }

    private async Task SendToolStartSseEventAsync(AiToolCall toolCall, CancellationToken cancellationToken)
    {
        var description = toolCall.ToolName switch
        {
            PipelineToolDefinitions.SaveArtefact => $"Saving {GetToolInputString(toolCall, "file_path") ?? "artefact"}...",
            PipelineToolDefinitions.ListArtefacts => "Reading project artefacts...",
            PipelineToolDefinitions.GetArtefact => $"Reading {GetToolInputString(toolCall, "file_path") ?? "artefact"}...",
            PipelineToolDefinitions.AdvancePhase => "Advancing to next phase...",
            PipelineToolDefinitions.UpdateProgress => "Updating progress...",
            PipelineToolDefinitions.AddParkingLotItem => "Adding parking lot item...",
            PipelineToolDefinitions.ResolveParkingLotItem => "Resolving parking lot item...",
            PipelineToolDefinitions.GetGuardrailDetails => $"Loading {GetToolInputString(toolCall, "skill_name") ?? "guardrail"} guidelines...",
            PipelineToolDefinitions.SetOrchestrationMode => "Entering cross-check mode...",
            PipelineToolDefinitions.AdvanceRequirement => $"Completing requirement {GetToolInputString(toolCall, "requirement_id") ?? string.Empty}...",
            PipelineToolDefinitions.EditArtefact => $"Editing {GetToolInputString(toolCall, "file_path") ?? "artefact"}...",
            _ => $"Running {toolCall.ToolName}..."
        };

        var toolStartEvent = JsonSerializer.Serialize(new { tool = toolCall.ToolName, description });
        await Response.WriteAsync($"event: tool_start\ndata: {toolStartEvent}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static string? GetToolInputString(AiToolCall toolCall, string propertyName)
    {
        if (toolCall.Input.RootElement.TryGetProperty(propertyName, out var value))
            return value.GetString();
        return null;
    }

    private async Task SendToolSseEventAsync(
        AiToolCall toolCall,
        Conversation conversation,
        List<Artefact> savedArtefacts,
        List<ParkingLotItem> savedParkingLotItems,
        CancellationToken cancellationToken)
    {
        switch (toolCall.ToolName)
        {
            case PipelineToolDefinitions.UpdateProgress:
            case PipelineToolDefinitions.AdvancePhase:
            {
                var progressEvent = JsonSerializer.Serialize(new
                {
                    currentPhase = conversation.CurrentPhase,
                    phaseName = conversation.PhaseName,
                    totalPhases = conversation.TotalPhases,
                    questionsAsked = conversation.QuestionsAsked,
                    estimatedTotalQuestions = conversation.EstimatedTotalQuestions,
                    requirementsCaptured = conversation.RequirementsCaptured
                });
                await Response.WriteAsync($"event: progress\ndata: {progressEvent}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                break;
            }

            case PipelineToolDefinitions.SaveArtefact:
            {
                var lastArtefact = savedArtefacts[^1];
                var artefactEvent = JsonSerializer.Serialize(new
                {
                    filePath = lastArtefact.FilePath,
                    version = lastArtefact.Version,
                    id = lastArtefact.Id
                });
                await Response.WriteAsync($"event: artefact\ndata: {artefactEvent}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                break;
            }

            case PipelineToolDefinitions.AddParkingLotItem:
            {
                if (savedParkingLotItems.Count > 0)
                {
                    var lastItem = savedParkingLotItems[^1];
                    var itemEvent = JsonSerializer.Serialize(new
                    {
                        id = lastItem.Id,
                        content = lastItem.Content,
                        priority = lastItem.Priority.ToString().ToLowerInvariant(),
                        status = lastItem.Status.ToString().ToLowerInvariant(),
                        sourcePhase = lastItem.SourcePhase
                    });
                    await Response.WriteAsync($"event: parking_lot_item\ndata: {itemEvent}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
                break;
            }

            case PipelineToolDefinitions.ResolveParkingLotItem:
            {
                var resolvedItemId = toolCall.Input.RootElement.GetProperty("item_id").GetString()!;
                if (Guid.TryParse(resolvedItemId, out var resolvedGuid))
                {
                    var resolveEvent = JsonSerializer.Serialize(new
                    {
                        id = resolvedGuid,
                        status = "resolved"
                    });
                    await Response.WriteAsync($"event: parking_lot_resolved\ndata: {resolveEvent}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
                break;
            }

            case PipelineToolDefinitions.EditArtefact:
            {
                if (savedArtefacts.Count > 0)
                {
                    var lastArtefact = savedArtefacts[^1];
                    var artefactEvent = JsonSerializer.Serialize(new
                    {
                        filePath = lastArtefact.FilePath,
                        version = lastArtefact.Version,
                        id = lastArtefact.Id
                    });
                    await Response.WriteAsync($"event: artefact\ndata: {artefactEvent}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
                break;
            }
        }
    }

    private async Task<string> ExecuteToolCallWithRetryAsync(
        AiToolCall toolCall,
        Conversation conversation,
        List<Artefact> savedArtefacts,
        List<ParkingLotItem> savedParkingLotItems,
        IReadOnlyList<ParkingLotItem> projectParkingLotItems,
        string createdBy,
        Guid projectId,
        StageType? stageType,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= ToolExecutionRetryCount + 1; attempt++)
        {
            try
            {
                return await ExecuteToolCallAsync(
                    toolCall,
                    conversation,
                    savedArtefacts,
                    savedParkingLotItems,
                    projectParkingLotItems,
                    createdBy,
                    projectId,
                    stageType,
                    cancellationToken);
            }
            catch (Exception exception) when (attempt <= ToolExecutionRetryCount)
            {
                _logger.LogWarning(
                    exception,
                    "Tool {ToolName} attempt {Attempt}/{MaxAttempts} failed. Retrying.",
                    toolCall.ToolName,
                    attempt,
                    ToolExecutionRetryCount + 1);
            }
            catch (Exception exception)
            {
                throw new ToolExecutionFailedException(
                    toolCall.ToolName,
                    $"Tool '{toolCall.ToolName}' failed after {ToolExecutionRetryCount + 1} attempts. Reason: {exception.Message}",
                    exception);
            }
        }

        throw new ToolExecutionFailedException(
            toolCall.ToolName,
            $"Tool '{toolCall.ToolName}' failed after {ToolExecutionRetryCount + 1} attempts.");
    }

    private async Task<string> ExecuteToolCallAsync(
        AiToolCall toolCall,
        Conversation conversation,
        List<Artefact> savedArtefacts,
        List<ParkingLotItem> savedParkingLotItems,
        IReadOnlyList<ParkingLotItem> projectParkingLotItems,
        string createdBy,
        Guid projectId,
        StageType? stageType,
        CancellationToken cancellationToken)
    {
        var root = toolCall.Input.RootElement;

        switch (toolCall.ToolName)
        {
            case PipelineToolDefinitions.SaveArtefact:
            {
                var filePath = root.GetProperty("file_path").GetString()!;
                var content = root.GetProperty("content").GetString()!;

                if (stageType == StageType.Prototype)
                {
                    var validation = ValidatePipeline02SaveContract(filePath, content);
                    if (!validation.IsValid)
                    {
                        _logger.LogWarning(
                            "Tool save_artefact rejected for pipeline02 contract failure: {Reason}",
                            validation.Reason);
                        return $"Error: pipeline02_output_contract_failed: {validation.Reason}";
                    }
                }

                var contentType = ResolveArtefactContentType(filePath);

                var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
                    projectId, filePath, cancellationToken);

                var storageKey = await _artefactStorageService.SaveContentAsync(
                    projectId, filePath, nextVersion, content, contentType, cancellationToken);

                var artefact = Artefact.CreateS3Artefact(
                    projectId,
                    nextVersion,
                    filePath,
                    storageKey,
                    contentType,
                    System.Text.Encoding.UTF8.GetByteCount(content),
                    createdBy,
                    _timeProvider);

                // Save new version first, then delete old ones
                await _artefactRepository.AddAsync(artefact, cancellationToken);
                await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
                await _artefactRepository.DeletePreviousVersionsAsync(
                    projectId, filePath, nextVersion, cancellationToken);

                savedArtefacts.Add(artefact);

                // Trigger prototype assembly if fragment path and flag enabled
                if (_tokenOptimisationOptions.PrototypeFragmentsEnabled &&
                    filePath.StartsWith("prototype/fragments/", StringComparison.OrdinalIgnoreCase))
                {
                    await _prototypeAssemblyService.AssemblePrototypeAsync(projectId, cancellationToken);
                }

                _logger.LogInformation(
                    "Tool save_artefact: saved {FilePath} v{Version} ({Length} chars, {ContentType})",
                    filePath, nextVersion, content.Length, contentType);
                return $"Saved {filePath} (version {nextVersion}, {content.Length} chars, {contentType})";
            }

            case PipelineToolDefinitions.AdvancePhase:
            {
                var phaseNumber = root.GetProperty("phase_number").GetInt32();
                var phaseName = root.GetProperty("phase_name").GetString()!;

                if (stageType == StageType.Prototype && phaseNumber >= Pipeline02CompletionPhaseNumber)
                {
                    var completionValidation = await ValidatePipeline02CompletionGateAsync(projectId, cancellationToken);
                    if (!completionValidation.IsValid)
                    {
                        _logger.LogWarning(
                            "Tool advance_phase blocked by pipeline02 completion gate: {Reason}",
                            completionValidation.Reason);
                        return $"Error: pipeline02_completion_gate_failed: {completionValidation.Reason}";
                    }
                }

                conversation.SetPhase(phaseNumber, phaseName);

                _logger.LogInformation(
                    "Tool advance_phase: → Phase {Phase} ({Name})",
                    phaseNumber, phaseName);
                return $"Advanced to phase {phaseNumber} ({phaseName})";
            }

            case PipelineToolDefinitions.AddParkingLotItem:
            {
                var priorityStr = root.GetProperty("priority").GetString()!;
                var content = root.GetProperty("content").GetString()!;

                // Server-side deduplication: check against ALL open items across the project (not just this conversation)
                var normalizedContent = content.ToUpperInvariant().Trim();
                var allOpenItems = projectParkingLotItems
                    .Where(item => item.Status == Domain.Enums.ParkingLotStatus.Open)
                    .Concat(savedParkingLotItems.Where(item => item.Status == Domain.Enums.ParkingLotStatus.Open))
                    .ToList();

                var isDuplicate = allOpenItems.Any(existing =>
                {
                    var existingNormalized = existing.Content.ToUpperInvariant().Trim();
                    // Exact match or one contains the other (covers rephrasing)
                    return existingNormalized == normalizedContent
                        || existingNormalized.Contains(normalizedContent, StringComparison.OrdinalIgnoreCase)
                        || normalizedContent.Contains(existingNormalized, StringComparison.OrdinalIgnoreCase);
                });

                if (isDuplicate)
                {
                    _logger.LogInformation(
                        "Tool add_parking_lot_item: DUPLICATE skipped — [{Priority}] {Content}",
                        priorityStr, content);
                    return "Item already exists in parking lot (duplicate skipped)";
                }

                if (Enum.TryParse<ParkingLotPriority>(priorityStr, ignoreCase: true, out var priority))
                {
                    var parkingLotItem = conversation.AddParkingLotItem(content, priority, _timeProvider);
                    savedParkingLotItems.Add(parkingLotItem);

                    _logger.LogInformation(
                        "Tool add_parking_lot_item: [{Priority}] {Content}",
                        priority, content);
                }
                return "Item added to parking lot";
            }

            case PipelineToolDefinitions.ResolveParkingLotItem:
            {
                var itemIdStr = root.GetProperty("item_id").GetString()!;
                if (!Guid.TryParse(itemIdStr, out var itemId))
                {
                    return "Error: invalid item_id — must be a valid UUID";
                }

                // Search across project-level items and items saved in this session
                var targetItem = projectParkingLotItems.FirstOrDefault(item => item.Id == itemId)
                    ?? savedParkingLotItems.FirstOrDefault(item => item.Id == itemId);

                if (targetItem is null)
                {
                    return $"Error: parking lot item {itemIdStr} not found";
                }

                if (targetItem.Status == Domain.Enums.ParkingLotStatus.Resolved)
                {
                    return "Item is already resolved";
                }

                targetItem.Resolve(_timeProvider);
                await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Tool resolve_parking_lot_item: [{ItemId}] {Content}",
                    itemId, targetItem.Content);

                return $"Parking lot item resolved: {targetItem.Content}";
            }

            case PipelineToolDefinitions.UpdateProgress:
            {
                var questionsAsked = root.GetProperty("questions_asked").GetInt32();
                var estimatedTotal = root.GetProperty("estimated_total").GetInt32();
                var requirementsCaptured = root.TryGetProperty("requirements_captured", out var reqProp)
                    ? reqProp.GetInt32()
                    : (int?)null;

                conversation.UpdateProgress(questionsAsked, estimatedTotal, requirementsCaptured);

                _logger.LogInformation(
                    "Tool update_progress: Q{QuestionsAsked}/{EstimatedTotal}, reqs={Reqs}",
                    questionsAsked, estimatedTotal, requirementsCaptured ?? 0);
                return $"Progress updated: {questionsAsked}/{estimatedTotal} questions";
            }

            case PipelineToolDefinitions.GetGuardrailDetails:
            {
                var skillName = root.GetProperty("skill_name").GetString()!;
                var content = _skillContentService.GetSkillContent(skillName);

                if (content is null)
                {
                    _logger.LogWarning("Tool get_guardrail_details: skill not found: {SkillName}", skillName);
                    var available = string.Join(", ", _skillContentService.GetAvailableSkills());
                    return $"Skill '{skillName}' not found. Available skills: {available}";
                }

                _logger.LogInformation(
                    "Tool get_guardrail_details: returned {SkillName} ({Length} chars)",
                    skillName, content.Length);
                return content;
            }

            case PipelineToolDefinitions.ListArtefacts:
            {
                var manifest = await _artefactRepository.GetProjectArtefactManifestAsync(projectId, cancellationToken);

                if (manifest.Count == 0)
                {
                    _logger.LogInformation("Tool list_artefacts: no artefacts found for project {ProjectId}", projectId);
                    return "No artefacts have been saved to this project yet.";
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Found {manifest.Count} artefact(s):\n");
                foreach (var artefact in manifest)
                {
                    sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"- {artefact.FilePath} (v{artefact.Version}, {artefact.CreatedAt:yyyy-MM-dd HH:mm})");
                }

                _logger.LogInformation("Tool list_artefacts: returned {Count} files", manifest.Count);
                return sb.ToString();
            }

            case PipelineToolDefinitions.GetArtefact:
            {
                var filePath = root.GetProperty("file_path").GetString()!;
                var artefact = await _artefactRepository.GetByProjectAndFilePathAsync(projectId, filePath, cancellationToken);

                if (artefact is null)
                {
                    _logger.LogWarning("Tool get_artefact: file not found: {FilePath}", filePath);
                    return $"Artefact '{filePath}' not found. Use list_artefacts to see available files.";
                }

                var artefactContent = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);
                if (artefactContent is null)
                {
                    _logger.LogWarning("Tool get_artefact: content unavailable for {FilePath}", filePath);
                    return $"Artefact '{filePath}' content could not be retrieved.";
                }

                _logger.LogInformation(
                    "Tool get_artefact: returned {FilePath} v{Version} ({Length} chars)",
                    filePath, artefact.Version, artefactContent.Length);
                return $"## {filePath} (v{artefact.Version})\n\n{artefactContent}";
            }

            case PipelineToolDefinitions.AdvanceRequirement:
            {
                var requirementIdInput = root.TryGetProperty("requirement_id", out var reqProp)
                    ? reqProp.GetString()
                    : null;
                var summary = root.TryGetProperty("summary", out var summaryProp)
                    ? summaryProp.GetString()
                    : null;

                // Contract 1 gate: at least one requirements/REQ-*.md must exist for this project,
                // either saved via tool in this session or persisted in a prior session.
                var hasRequirementArtefact =
                    savedArtefacts.Any(artefact =>
                        artefact.FilePath.StartsWith("requirements/REQ-", StringComparison.OrdinalIgnoreCase)
                        && artefact.FilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    || await _artefactRepository.HasRequirementArtefactAsync(projectId, cancellationToken);

                if (!hasRequirementArtefact)
                {
                    _logger.LogWarning(
                        "Tool advance_requirement blocked by completion gate for conversation {ConversationId}, requirement {RequirementId}: no requirement artefact saved in this session",
                        conversation.Id, requirementIdInput ?? "(unknown)");
                    return "Error: requirement_completion_gate_failed: You must save the requirement artefact (requirements/REQ-xxx.md) before signalling completion. Save the artefact first, then call advance_requirement.";
                }

                conversation.Complete();
                await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Tool advance_requirement: requirement {RequirementId} completed for conversation {ConversationId}. Summary: {Summary}",
                    requirementIdInput ?? "(none)", conversation.Id, summary ?? "(none)");

                // Emit SSE here (inside the gate-passed path) so it only fires on success
                var requirementCompletedEvent = JsonSerializer.Serialize(new
                {
                    requirementId = conversation.RequirementId,
                    conversationId = conversation.Id
                });
                await Response.WriteAsync($"event: requirement_complete\ndata: {requirementCompletedEvent}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                return $"Requirement {requirementIdInput ?? conversation.RequirementId ?? "(unknown)"} marked complete.";
            }

            case PipelineToolDefinitions.SetOrchestrationMode:
            {
                var modeValue = root.GetProperty("mode").GetString()!;
                var justification = root.TryGetProperty("justification", out var jProp) ? jProp.GetString() : null;

                // Only cross_check is a valid transition target
                if (!string.Equals(modeValue, "cross_check", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Tool set_orchestration_mode: invalid mode requested: {Mode}", modeValue);
                    return $"Invalid mode '{modeValue}'. Only 'cross_check' is a valid transition target via this tool.";
                }

                // Guard: only valid for P6/P7/P8 (ClinicalSafety, InformationGovernance, Security)
                if (stageType is not (StageType.ClinicalSafety or StageType.InformationGovernance or StageType.Security))
                {
                    _logger.LogWarning(
                        "Tool set_orchestration_mode: cross_check requested on invalid stage {StageType}",
                        stageType);
                    return $"Cross-check mode is only valid for P6 (clinical_safety), P7 (information_governance), and P8 (security). Current stage: {stageType}.";
                }

                conversation.EnterCrossCheckMode();
                await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Orchestration mode set to cross_check for conversation {ConversationId}, stage {StageType}. Justification: {Justification}",
                    conversation.Id, stageType, justification ?? "(none provided)");

                return $"Orchestration mode set to cross_check. Forward sweep is complete. Beginning cross-requirement consistency check.";
            }

            case PipelineToolDefinitions.EditArtefact:
            {
                if (!_tokenOptimisationOptions.EditArtefactEnabled)
                    return "Error: edit_artefact is not enabled on this server.";

                var filePath = root.GetProperty("file_path").GetString()!;
                var oldStr = root.GetProperty("old_str").GetString()!;
                var newStr = root.GetProperty("new_str").GetString()!;

                if (string.IsNullOrEmpty(oldStr))
                    return "Error: edit_artefact_invalid_anchor: old_str must not be empty.";

                // Load latest version
                var existingArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
                    projectId, filePath, cancellationToken);

                if (existingArtefact is null)
                    return $"Error: FILE_NOT_FOUND: No artefact found at path '{filePath}'. Use list_artefacts to see available files.";

                var existingContent = await _artefactStorageService.GetContentAsync(
                    existingArtefact.S3Key, cancellationToken);

                if (existingContent is null)
                    return $"Error: FILE_NOT_FOUND: Artefact content could not be retrieved for '{filePath}'.";

                // Count occurrences - exact match, no normalisation
                var occurrences = CountOccurrences(existingContent, oldStr);

                if (occurrences == 0)
                {
                    _logger.LogWarning(
                        "Tool edit_artefact: ANCHOR_NOT_FOUND for {FilePath} (old_str length {Length})",
                        filePath, oldStr.Length);
                    return $"Error: ANCHOR_NOT_FOUND: The anchor string was not found in '{filePath}'. " +
                           "Use get_artefact to re-read the current content and retry with a corrected anchor.";
                }

                if (occurrences > 1)
                {
                    _logger.LogWarning(
                        "Tool edit_artefact: ANCHOR_AMBIGUOUS for {FilePath} ({Count} occurrences, old_str length {Length})",
                        filePath, occurrences, oldStr.Length);
                    return $"Error: ANCHOR_AMBIGUOUS: The anchor string appears {occurrences} times in '{filePath}'. " +
                           "Use a longer, more unique anchor string.";
                }

                // Apply edit
                var updatedContent = existingContent.Replace(oldStr, newStr, StringComparison.Ordinal);
                var bytesChanged = Math.Abs(
                    System.Text.Encoding.UTF8.GetByteCount(updatedContent) -
                    System.Text.Encoding.UTF8.GetByteCount(existingContent));

                var contentType = existingArtefact.ContentType;
                var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
                    projectId, filePath, cancellationToken);

                var storageKey = await _artefactStorageService.SaveContentAsync(
                    projectId, filePath, nextVersion, updatedContent, contentType, cancellationToken);

                var editedArtefact = Artefact.CreateS3Artefact(
                    projectId,
                    nextVersion,
                    filePath,
                    storageKey,
                    contentType,
                    System.Text.Encoding.UTF8.GetByteCount(updatedContent),
                    createdBy,
                    _timeProvider);

                await _artefactRepository.AddAsync(editedArtefact, cancellationToken);
                await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
                await _artefactRepository.DeletePreviousVersionsAsync(
                    projectId, filePath, nextVersion, cancellationToken);

                savedArtefacts.Add(editedArtefact);

                _logger.LogInformation(
                    "Tool edit_artefact: edited {FilePath} v{Version} ({BytesChanged} bytes changed, total {Total} bytes, {ContentType})",
                    filePath, nextVersion, bytesChanged, System.Text.Encoding.UTF8.GetByteCount(updatedContent), contentType);

                // Trigger prototype assembly if fragment path and flag enabled
                if (_tokenOptimisationOptions.PrototypeFragmentsEnabled &&
                    filePath.StartsWith("prototype/fragments/", StringComparison.OrdinalIgnoreCase))
                {
                    await _prototypeAssemblyService.AssemblePrototypeAsync(projectId, cancellationToken);
                }

                return $"Edited {filePath} (version {nextVersion}, {bytesChanged} bytes changed, total {System.Text.Encoding.UTF8.GetByteCount(updatedContent)} bytes)";
            }

            default:
                _logger.LogWarning("Unknown tool call: {ToolName}", toolCall.ToolName);
                return "Unknown tool";
        }
    }

    private async Task<ValidationResult> ValidatePipeline02CompletionGateAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var prototypeHtml = await _artefactRepository.GetByProjectAndFilePathAsync(projectId, PrototypeHtmlArtefactPath, cancellationToken);
        if (prototypeHtml is null)
        {
            return ValidationResult.Fail("Missing required artefact: prototype/index.html");
        }

        var prototypeNotes = await _artefactRepository.GetByProjectAndFilePathAsync(projectId, PrototypeNotesArtefactPath, cancellationToken);
        if (prototypeNotes is null)
        {
            return ValidationResult.Fail("Missing required artefact: prototype/PROTOTYPE_NOTES.md");
        }

        var htmlContent = await _artefactStorageService.GetContentAsync(prototypeHtml.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return ValidationResult.Fail("prototype/index.html content could not be retrieved");
        }

        var notesContent = await _artefactStorageService.GetContentAsync(prototypeNotes.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(notesContent))
        {
            return ValidationResult.Fail("prototype/PROTOTYPE_NOTES.md content could not be retrieved");
        }

        var htmlValidation = ValidatePrototypeHtmlContract(htmlContent);
        if (!htmlValidation.IsValid)
        {
            return htmlValidation;
        }

        var notesValidation = ValidatePrototypeNotesContract(notesContent);
        if (!notesValidation.IsValid)
        {
            return notesValidation;
        }

        return ValidationResult.Ok();
    }

    private static ValidationResult ValidatePipeline02SaveContract(string filePath, string content)
    {
        if (filePath.Equals(PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase))
        {
            return ValidatePrototypeHtmlContract(content);
        }

        if (filePath.Equals(PrototypeNotesArtefactPath, StringComparison.OrdinalIgnoreCase))
        {
            return ValidatePrototypeNotesContract(content);
        }

        return ValidationResult.Ok();
    }

    private static ValidationResult ValidatePrototypeHtmlContract(string htmlContent)
    {
        const string metadataStartTag = "<script id=\"prototype-metadata\" type=\"application/json\">";
        const string metadataEndTag = "</script>";

        var startIndex = htmlContent.IndexOf(metadataStartTag, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return ValidationResult.Fail("prototype/index.html is missing required metadata script tag");
        }

        var jsonStart = startIndex + metadataStartTag.Length;
        var endIndex = htmlContent.IndexOf(metadataEndTag, jsonStart, StringComparison.OrdinalIgnoreCase);
        if (endIndex < 0)
        {
            return ValidationResult.Fail("prototype/index.html metadata script is not closed correctly");
        }

        var metadataJson = htmlContent[jsonStart..endIndex].Trim();
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return ValidationResult.Fail("prototype/index.html metadata JSON is empty");
        }

        try
        {
            using var metadataDocument = JsonDocument.Parse(metadataJson);
            var metadataRoot = metadataDocument.RootElement;

            if (!metadataRoot.TryGetProperty("contractVersion", out var contractVersion)
                || contractVersion.GetString() != "1.0")
            {
                return ValidationResult.Fail("prototype/index.html metadata must include contractVersion='1.0'");
            }

            if (!metadataRoot.TryGetProperty("stageCode", out var stageCode)
                || !string.Equals(stageCode.GetString(), "prototype", StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Fail("prototype/index.html metadata must include stageCode='prototype'");
            }

            if (!metadataRoot.TryGetProperty("prototypeOnly", out var prototypeOnly)
                || prototypeOnly.ValueKind != JsonValueKind.True)
            {
                return ValidationResult.Fail("prototype/index.html metadata must include prototypeOnly=true");
            }

            if (!metadataRoot.TryGetProperty("generatedAtUtc", out var generatedAtUtc)
                || generatedAtUtc.ValueKind != JsonValueKind.String
                || !DateTimeOffset.TryParse(generatedAtUtc.GetString(), out _))
            {
                return ValidationResult.Fail("prototype/index.html metadata must include generatedAtUtc as ISO datetime");
            }

            if (!metadataRoot.TryGetProperty("requirementsCovered", out var requirementsCovered)
                || requirementsCovered.ValueKind != JsonValueKind.Array
                || requirementsCovered.GetArrayLength() == 0)
            {
                return ValidationResult.Fail("prototype/index.html metadata must include non-empty requirementsCovered array");
            }

            if (!metadataRoot.TryGetProperty("flows", out var flows)
                || flows.ValueKind != JsonValueKind.Array
                || flows.GetArrayLength() == 0)
            {
                return ValidationResult.Fail("prototype/index.html metadata must include non-empty flows array");
            }

            if (!metadataRoot.TryGetProperty("privacySafetyConstraints", out var constraints)
                || constraints.ValueKind != JsonValueKind.Array
                || constraints.GetArrayLength() == 0)
            {
                return ValidationResult.Fail("prototype/index.html metadata must include non-empty privacySafetyConstraints array");
            }
        }
        catch (JsonException)
        {
            return ValidationResult.Fail("prototype/index.html metadata must contain valid JSON");
        }

        return ValidationResult.Ok();
    }

    private static ValidationResult ValidatePrototypeNotesContract(string notesContent)
    {
        var requiredHeaders = new[]
        {
            "# Prototype Validation Notes",
            "## Summary",
            "## Requirements Validation",
            "## Output Contract",
            "## Open Questions"
        };

        foreach (var requiredHeader in requiredHeaders)
        {
            if (!notesContent.Contains(requiredHeader, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Fail($"prototype/PROTOTYPE_NOTES.md is missing section '{requiredHeader}'");
            }
        }

        var requiredContractLines = new[]
        {
            "output_contract_version: 1.0",
            "stage_code: prototype",
            "html_artefact_path: prototype/index.html",
            "completion_decision:"
        };

        foreach (var requiredContractLine in requiredContractLines)
        {
            if (!notesContent.Contains(requiredContractLine, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Fail($"prototype/PROTOTYPE_NOTES.md is missing contract field '{requiredContractLine}'");
            }
        }

        return ValidationResult.Ok();
    }

    private static string ResolveArtefactContentType(string filePath)
    {
        if (filePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            return "text/html";
        }

        return "text/markdown";
    }

    internal static int CountOccurrences(string source, string target)
    {
        if (string.IsNullOrEmpty(target))
            return 0;

        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(target, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += target.Length;
        }
        return count;
    }

    private static string BuildStateContext(Conversation conversation, IReadOnlyList<ParkingLotItem> projectParkingLotItems)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append("**Current Phase:** ").Append(conversation.CurrentPhase).Append(" (").Append(conversation.PhaseName).AppendLine(")");
        sb.Append("**Total Phases:** ").AppendLine(conversation.TotalPhases.ToString(System.Globalization.CultureInfo.InvariantCulture));
        sb.Append("**Questions Asked:** ").AppendLine(conversation.QuestionsAsked.ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (conversation.EstimatedTotalQuestions.HasValue)
            sb.Append("**Estimated Total Questions:** ").AppendLine(conversation.EstimatedTotalQuestions.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var openItems = projectParkingLotItems.Where(item => item.Status == Domain.Enums.ParkingLotStatus.Open).ToList();
        if (openItems.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**Open Parking Lot Items (project-wide — do NOT re-add these):**");
            foreach (var item in openItems)
            {
                var emoji = item.Priority switch
                {
                    Domain.Enums.ParkingLotPriority.Critical => "🔴",
                    Domain.Enums.ParkingLotPriority.High => "🟡",
                    _ => "🟢"
                };
                sb.Append("- ").Append(emoji).Append(" [").Append(item.Id).Append("] ").Append(item.Content).Append(" (from phase ").Append(item.SourcePhase).AppendLine(")");
            }
        }

        sb.AppendLine();
        sb.AppendLine("**IMPORTANT:** You do NOT manage the parking lot or phase tracking. The API handles that. Focus on asking questions, analyzing answers, and generating content. Do NOT include progress bars or parking lot summaries in your responses — the UI displays those from API data.");

        return sb.ToString();
    }

    private static string BuildProjectContext(ProjectContext? project)
    {
        if (project is null) return "Project context unavailable.";

        var sb = new System.Text.StringBuilder();
        sb.Append("**Project Code:** ").AppendLine(project.Code);
        sb.Append("**Product Name:** ").AppendLine(project.Name);
        if (!string.IsNullOrWhiteSpace(project.Description))
            sb.Append("**Description:** ").AppendLine(project.Description);
        sb.Append("**Compliance Domain:** ").AppendLine(project.ComplianceDomain.ToString());
        sb.AppendLine();
        sb.AppendLine("These values were captured at project creation. Do NOT re-ask the user for project name, code, or compliance domain — they are already set.");

        return sb.ToString();
    }

    private static string BuildArtefactManifest(IReadOnlyList<Artefact> artefacts)
    {
        if (artefacts.Count == 0) return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"The project has {artefacts.Count} saved artefact(s). Use `get_artefact` tool to read their content.\n");

        foreach (var artefact in artefacts)
        {
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"- `{artefact.FilePath}` (v{artefact.Version}, {artefact.CreatedAt:yyyy-MM-dd HH:mm})");
        }

        return sb.ToString();
    }

    private readonly record struct ValidationResult(bool IsValid, string? Reason)
    {
        public static ValidationResult Ok()
        {
            return new ValidationResult(true, null);
        }

        public static ValidationResult Fail(string reason)
        {
            return new ValidationResult(false, reason);
        }
    }

    private sealed class ToolExecutionFailedException : Exception
    {
        public string ToolName { get; }

        public ToolExecutionFailedException(string toolName, string message)
            : base(message)
        {
            ToolName = toolName;
        }

        public ToolExecutionFailedException(string toolName, string message, Exception innerException)
            : base(message, innerException)
        {
            ToolName = toolName;
        }
    }
}
