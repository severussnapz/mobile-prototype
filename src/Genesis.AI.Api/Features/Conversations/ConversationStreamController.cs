using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Core.Extensions;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Commands.ProposeRequirementChange;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Configuration;
using Genesis.AI.Infrastructure.Services;
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
    private static readonly Regex _injectedSectionHeadingRegex = new(
        @"^## (?<heading>.+ \(Added by [^)]+\))$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly IConversationRepository _conversationRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IAiService _aiService;
    private readonly IPromptService _promptService;
    private readonly ISkillContentService _skillContentService;
    private readonly IActiveSkillsService _activeSkillsService;
    private readonly IFoundationService _foundationService;
    private readonly ISessionCloseContextBuilder _sessionCloseContextBuilder;
    private readonly IPrototypeAssemblyService _prototypeAssemblyService;
    private readonly IPrototypeFragmentMigrationService _prototypeFragmentMigrationService;
    private readonly IPrototypeDomSearchService? _prototypeDomSearchService;
    private readonly IPrototypeDomMutationService? _prototypeDomMutationService;
    private readonly TokenOptimisationOptions _tokenOptimisationOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ConversationStreamController> _logger;
    private readonly ProposeRequirementChangeCommandHandler _proposeRequirementChangeHandler;

    public ConversationStreamController(
        ProposeRequirementChangeCommandHandler proposeRequirementChangeHandler,
        IConversationRepository conversationRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IAiService aiService,
        IPromptService promptService,
        ISkillContentService skillContentService,
        IActiveSkillsService activeSkillsService,
        IFoundationService foundationService,
        ISessionCloseContextBuilder sessionCloseContextBuilder,
        IPrototypeAssemblyService prototypeAssemblyService,
        IPrototypeFragmentMigrationService prototypeFragmentMigrationService,
        IOptions<TokenOptimisationOptions> tokenOptimisationOptions,
        TimeProvider timeProvider,
        ILogger<ConversationStreamController> logger,
        IPrototypeDomSearchService? prototypeDomSearchService = null,
        IPrototypeDomMutationService? prototypeDomMutationService = null)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _skillContentService = skillContentService ?? throw new ArgumentNullException(nameof(skillContentService));
        _activeSkillsService = activeSkillsService ?? throw new ArgumentNullException(nameof(activeSkillsService));
        _foundationService = foundationService ?? throw new ArgumentNullException(nameof(foundationService));
        _sessionCloseContextBuilder = sessionCloseContextBuilder ?? throw new ArgumentNullException(nameof(sessionCloseContextBuilder));
        _prototypeAssemblyService = prototypeAssemblyService ?? throw new ArgumentNullException(nameof(prototypeAssemblyService));
        _prototypeFragmentMigrationService = prototypeFragmentMigrationService ?? throw new ArgumentNullException(nameof(prototypeFragmentMigrationService));
        _prototypeDomSearchService = prototypeDomSearchService;
        _prototypeDomMutationService = prototypeDomMutationService;
        _tokenOptimisationOptions = tokenOptimisationOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptimisationOptions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _proposeRequirementChangeHandler = proposeRequirementChangeHandler ?? throw new ArgumentNullException(nameof(proposeRequirementChangeHandler));
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

        // Capture the pre-send message count so continuation handover can detect
        // a newly created continuation conversation even after the first user
        // message is persisted below.
        var messageCountBeforeSend = conversation.Messages.Count;

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

        // Build message history for AI — the system prompt already injects full session state
        // (phase, parking lot, artefact manifest) so the LLM does not need the full conversation
        // to re-orient. We only send the last 4 messages (2 exchanges) for immediate conversational
        // context. Sending more causes linear input-token growth with no correctness benefit.
        const int maxHistoryMessages = 4;
        var orderedMessages = conversation.Messages
            .OrderBy(message => message.CreatedAt)
            .ToList();

        var windowedMessages = orderedMessages.Count <= maxHistoryMessages
            ? orderedMessages
            : orderedMessages.Skip(orderedMessages.Count - maxHistoryMessages).ToList();

        var messages = windowedMessages
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

        // Resolve the system prompt from the stage type (stageType already loaded for auth check above).
        // Single-file prototype mode (flag + Prototype stage) swaps in the PrototypeDemoGeneration.md
        // prompt (with EMIS-X UI kit) instead of the fragment-pipeline Pipeline02Prototype.md.
        var prototypeSingleFile = _tokenOptimisationOptions.PrototypeSingleFileEnabled
            && stageType == StageType.Prototype;
        var basePrompt = prototypeSingleFile
            ? _promptService.GetPrototypeSingleFilePrompt()
            : stageType is not null
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
        var prototypeIntentDirective = BuildPrototypeIntentRoutingDirective(
            stageType,
            request.Content,
            artefactManifest,
            prototypeSingleFile);

        // Prototype fragment migration — runs before LLM initialises, fully awaited (no race condition).
        // Detection: prototype/fragments/_shell.html exists → skip. Monolith present → split into fragments.
        // Pure C#, no LLM call, deterministic. Safe to call on every Prototype conversation.
        // Skipped entirely in single-file mode — that path never produces fragments.
        if (stageType == StageType.Prototype && !prototypeSingleFile)
        {
            await _prototypeFragmentMigrationService.MigrateIfNeededAsync(
                projectId, initiatedBy: User.GetUserErn() ?? "system", cancellationToken);

            // Refresh manifest so LLM sees the newly created fragment artefacts
            artefactManifest = await _artefactRepository.GetProjectArtefactManifestAsync(projectId, cancellationToken);
            artefactManifestSection = BuildArtefactManifest(artefactManifest);
        }

        // Build handover block if this conversation continues from a previous one that hit the tool limit.
        // Injected into the mutable system prompt so the AI knows where the previous session left off.
        var handoverBlock = string.Empty;
        if (conversation.ContinuedFromConversationId.HasValue && messageCountBeforeSend == 0)
        {
            var priorConversation = await _conversationRepository.GetByIdWithMessagesAsync(
                conversation.ContinuedFromConversationId.Value, cancellationToken);

            if (priorConversation is not null)
            {
                var priorOrderedMessages = priorConversation.Messages
                    .OrderBy(message => message.CreatedAt)
                    .ToList();

                var lastUserMessage = priorOrderedMessages
                    .LastOrDefault(message => message.Role == MessageRole.User);

                var lastAssistantMessage = priorOrderedMessages
                    .LastOrDefault(message => message.Role == MessageRole.Assistant);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("## CONTINUATION CONTEXT (previous conversation hit the tool-use limit)");
                sb.AppendLine();
                sb.AppendLine(CultureInfo.InvariantCulture, $"This is a continuation of conversation `{priorConversation.Id}`.");
                sb.AppendLine(CultureInfo.InvariantCulture, $"The previous session was at phase {priorConversation.CurrentPhase} ({priorConversation.PhaseName}).");
                sb.AppendLine(CultureInfo.InvariantCulture, $"Questions asked in prior session: {priorConversation.QuestionsAsked}.");

                if (lastUserMessage is not null)
                {
                    // Include the last user instruction — this is the task that was in progress when the limit hit
                    var userContent = lastUserMessage.Content.Length > 500
                        ? lastUserMessage.Content[..500] + "..."
                        : lastUserMessage.Content;

                    sb.AppendLine();
                    sb.AppendLine("**Last user instruction from previous session (the task that was interrupted):**");
                    sb.AppendLine("```");
                    sb.AppendLine(userContent);
                    sb.AppendLine("```");
                }

                if (lastAssistantMessage is not null)
                {
                    // Truncate to last 2000 chars to keep context bounded
                    var content = lastAssistantMessage.Content.Length > 2000
                        ? "..." + lastAssistantMessage.Content[^2000..]
                        : lastAssistantMessage.Content;

                    sb.AppendLine();
                    sb.AppendLine("**Last assistant message from previous session:**");
                    sb.AppendLine("```");
                    sb.AppendLine(content);
                    sb.AppendLine("```");
                }

                sb.AppendLine();
                sb.AppendLine("**IMPORTANT:** Resume naturally from where the previous session stopped. " +
                              "Do NOT re-introduce yourself or re-explain your purpose. " +
                              "Acknowledge the continuation briefly and continue the work.");

                var checkpointFilePath = BuildContinuationCheckpointFilePath(priorConversation.Id);
                var checkpointArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
                    projectId,
                    checkpointFilePath,
                    cancellationToken);

                if (checkpointArtefact is not null)
                {
                    var checkpointContent = await _artefactStorageService.GetContentAsync(
                        checkpointArtefact.S3Key,
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(checkpointContent))
                    {
                        sb.AppendLine();
                        sb.AppendLine("**Persisted checkpoint summary (source of truth for done/next):**");
                        sb.AppendLine("```");
                        sb.AppendLine(checkpointContent);
                        sb.AppendLine("```");
                    }
                }

                handoverBlock = $"\n\n---\n\n{sb}";
            }
        }

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
        var sessionCloseContext = stageType.HasValue
            ? await _sessionCloseContextBuilder.BuildSessionCloseContextAsync(projectId, stageType.Value, cancellationToken)
            : string.Empty;

        AiSystemPrompt aiSystemPrompt;
        if (_tokenOptimisationOptions.FoundationPrefixEnabled && stageType.HasValue)
        {
            var foundationContent = await _foundationService.BuildFoundationContentAsync(
                projectId, stageType.Value, cancellationToken);

            var stablePart = string.IsNullOrEmpty(foundationContent)
                ? basePrompt
                : $"{basePrompt}\n\n---\n\n{foundationContent}";

            if (_tokenOptimisationOptions.ActiveSkillInjectionEnabled)
            {
                var activeSkillContent = await _activeSkillsService.BuildActiveSkillsAsync(
                    stageType.Value, conversation.CurrentPhase, cancellationToken);

                if (!string.IsNullOrEmpty(activeSkillContent))
                {
                    stablePart += $"\n\n---\n\n## ACTIVE SKILLS (phase {conversation.CurrentPhase})\n\n{activeSkillContent}";
                }
            }

            var mutablePart = $"## PROJECT CONTEXT (from project creation)\n\n{projectContextSection}\n\n---\n\n## CURRENT SESSION STATE (managed by API)\n\n{stateContext}";

            if (!string.IsNullOrEmpty(artefactManifestSection))
            {
                mutablePart += $"\n\n---\n\n## PROJECT ARTEFACTS (live manifest — use get_artefact for unlisted files)\n\n{artefactManifestSection}";
            }

            if (!string.IsNullOrEmpty(prototypeIntentDirective))
            {
                mutablePart += $"\n\n---\n\n## REQUEST INTENT ROUTING (API-ENFORCED)\n\n{prototypeIntentDirective}";
            }

            if (!string.IsNullOrEmpty(sessionCloseContext))
            {
                mutablePart += $"\n\n---\n\n{sessionCloseContext}";
            }

            mutablePart += stalenessNotice;
            mutablePart += handoverBlock;

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

            if (!string.IsNullOrEmpty(prototypeIntentDirective))
            {
                systemPrompt += $"\n\n---\n\n## REQUEST INTENT ROUTING (API-ENFORCED)\n\n{prototypeIntentDirective}";
            }

            if (!string.IsNullOrEmpty(sessionCloseContext))
            {
                systemPrompt += $"\n\n---\n\n{sessionCloseContext}";
            }

            systemPrompt += stalenessNotice;
            systemPrompt += handoverBlock;

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
            const int defaultMaxToolTurns = 60; // Safety limit to prevent infinite tool loops (needs headroom for multi-file tasks and bulk guardrail application)
            const int maxAnchorNotFoundRetriesPerRequest = 4; // Prevent long-running edit loops on bad anchors
            var maxToolTurns = stageType.HasValue &&
                _tokenOptimisationOptions.StageToolTurnLimits.TryGetValue(stageType.Value.ToString(), out var stageLimit)
                ? stageLimit
                : defaultMaxToolTurns;
            var turnsRemaining = maxToolTurns;
            var anchorNotFoundRetries = 0;
            // Track which files have been read via get_artefact this request.
            // edit_artefact is blocked until the target file has been read, ensuring Claude
            // always anchors against the real file content rather than memory.
            var filesReadThisRequest = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Track total search_in_artefact calls per turn. After 5 searches without a mutation,
            // hard-stop the agent to prevent context window exhaustion from search loops.
            var searchCountThisTurn = new StrongBox<int>(0);

            // Post-search read block: after DOM search returns matches, prevent re-reads and REQ reads
            // until a mutation (apply_to_scope or save_artefact) succeeds. Blocks budget-burning thrashing.
            var postSearchReadBlocked = new StrongBox<bool>(false);

            // Zero-match hard block: once DOM search returns zero matches, block all subsequent
            // tool calls for this request to force a user clarification instead of tool thrashing.
            var zeroMatchToolBlocked = new StrongBox<bool>(false);

            // Read budget: cap get_artefact calls on non-prototype files per request.
            // Prevents the LLM reading all 13 REQ files before writing anything.
            // The artefact manifest in the system prompt already lists every file —
            // the LLM should read selectively, not exhaustively.
            const int maxReadBudget = 5;
            var readBudgetUsed = 0;

            // Prototype stage: cap total fragment saves per request to prevent the LLM
            // cycling through all fragments repeatedly after the initial build.
            // 15 saves allows a full build (8 fragments) plus one full refinement pass (7 more).
            const int maxPrototypeFragmentSaves = 15;

            while (turnsRemaining > 0)
            {
                var toolCallsThisTurn = new List<AiToolCall>();
                var turnText = new StringBuilder(); // Text produced in THIS turn only
                var maxTokens = prototypeSingleFile ? 64000 : 32768;

                // Insert a newline between turns so post-tool text doesn't run into pre-tool text
                var needsNewlineSeparator = fullResponse.Length > 0 && fullResponse[^1] != '\n';

                await foreach (var streamEvent in _aiService.StreamWithToolsAsync(
                    aiSystemPrompt,
                    aiMessages,
                    PipelineToolDefinitions.GetTools(_tokenOptimisationOptions, stageType),
                    cancellationToken,
                    maxTokens: maxTokens))
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

                // Detect same-turn get_artefact + edit_artefact on the same file.
                // Claude cannot anchor against a file it just requested in the same turn —
                // the result won't be in its context until the next turn.
                // Block the edit now; Claude will retry after seeing the file content.
                var filesReadThisTurn = toolCallsThisTurn
                    .Where(tc => tc.ToolName == PipelineToolDefinitions.GetArtefact)
                    .Select(tc =>
                    {
                        try { return tc.Input.RootElement.GetProperty("file_path").GetString(); }
                        catch { return null; }
                    })
                    .Where(path => path is not null)
                    .Select(path => path!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var toolCall in toolCallsThisTurn)
                {
                    // Enforce read budget inline — block get_artefact on non-prototype files
                    // once the budget is exhausted. This prevents the LLM reading all REQ files
                    // before writing anything, regardless of what the prompt directive says.
                    if (toolCall.ToolName == PipelineToolDefinitions.GetArtefact)
                    {
                        var requestedPath = toolCall.Input.RootElement.TryGetProperty("file_path", out var fpProp)
                            ? fpProp.GetString() ?? string.Empty
                            : string.Empty;
                        var isPrototypePath = requestedPath.StartsWith("prototype/", StringComparison.OrdinalIgnoreCase);
                        if (!isPrototypePath && !prototypeSingleFile)
                        {
                            if (readBudgetUsed >= maxReadBudget)
                            {
                                _logger.LogWarning(
                                    "Tool get_artefact blocked: read budget exhausted ({Budget}) — forcing write mode. File: {FilePath}",
                                    maxReadBudget, requestedPath);
                                toolResults.Add(new AiToolResult(toolCall.ToolUseId,
                                    $"READ BUDGET EXHAUSTED: You have already read {maxReadBudget} files. " +
                                    "Stop reading and call save_artefact now to produce output."));
                                await SendToolSseEventAsync(toolCall, conversation, savedArtefacts, savedParkingLotItems, cancellationToken);
                                continue;
                            }
                            readBudgetUsed++;
                        }
                    }

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
                            filesReadThisRequest,
                            filesReadThisTurn,
                            searchCountThisTurn,
                            postSearchReadBlocked,
                            zeroMatchToolBlocked,
                            prototypeSingleFile,
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

                    if (toolCall.ToolName == PipelineToolDefinitions.EditArtefact &&
                        result.StartsWith("Error: ANCHOR_NOT_FOUND", StringComparison.Ordinal))
                    {
                        anchorNotFoundRetries++;

                        if (anchorNotFoundRetries >= maxAnchorNotFoundRetriesPerRequest)
                        {
                            _logger.LogWarning(
                                "Terminating stream after repeated ANCHOR_NOT_FOUND errors for conversation {ConversationId} ({Count} attempts)",
                                conversation.Id,
                                anchorNotFoundRetries);

                            var anchorErrorEvent = JsonSerializer.Serialize(new
                            {
                                error = "Repeated anchor failures while editing artefact.",
                                reason = "edit_anchor_retry_limit"
                            });
                            await Response.WriteAsync($"event: error\ndata: {anchorErrorEvent}\n\n", cancellationToken);
                            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
                            await Response.Body.FlushAsync(cancellationToken);
                            return;
                        }
                    }
                    else if (toolCall.ToolName == PipelineToolDefinitions.EditArtefact)
                    {
                        // Reset on successful edit or non-anchor edit errors.
                        anchorNotFoundRetries = 0;
                    }

                    toolResults.Add(new AiToolResult(toolCall.ToolUseId, result));

                    await SendToolSseEventAsync(toolCall, conversation, savedArtefacts, savedParkingLotItems, cancellationToken);
                }

                // Build proper structured continuation messages for the Bedrock API.
                // Assistant message: text (if any) + tool_use blocks.
                // Strip large inputs (old_str/new_str from edit_artefact, content from search inputs)
                // from tool calls stored in history — the LLM already acted on them; keeping the
                // verbatim file content in every assistant message re-sends thousands of chars per turn.
                var assistantText = turnText.ToString();
                var compactedToolCalls = toolCallsThisTurn.Select(toolCall =>
                {
                    if (toolCall.ToolName != PipelineToolDefinitions.EditArtefact)
                        return toolCall;

                    // Strip old_str and new_str — keep only file_path so history is legible
                    try
                    {
                        var inputRoot = toolCall.Input.RootElement;
                        var filePath = inputRoot.TryGetProperty("file_path", out var fp) ? fp.GetString() ?? "" : "";
                        var oldLen = inputRoot.TryGetProperty("old_str", out var oldProp) ? oldProp.GetString()?.Length ?? 0 : 0;
                        var newLen = inputRoot.TryGetProperty("new_str", out var newProp) ? newProp.GetString()?.Length ?? 0 : 0;
                        var strippedJson = $"{{\"file_path\":\"{filePath}\",\"old_str\":\"[{oldLen} chars — stripped from history]\",\"new_str\":\"[{newLen} chars — stripped from history]\"}}";
                        var strippedDoc = System.Text.Json.JsonDocument.Parse(strippedJson);
                        return new AiToolCall(toolCall.ToolName, toolCall.ToolUseId, strippedDoc);
                    }
                    catch
                    {
                        return toolCall;
                    }
                }).ToList();

                aiMessages.Add(new AiMessage(
                    MessageRole.Assistant,
                    assistantText,
                    ToolCalls: compactedToolCalls));

                // User message: tool_result blocks
                // Truncate large tool results (get_artefact / get_guardrail_details) to a brief summary
                // Compact all tool results in history — see compaction logic below.
                var compactedToolResults = toolResults.Select(toolResult =>
                {
                    var matchingCall = toolCallsThisTurn
                        .FirstOrDefault(tc => tc.ToolUseId == toolResult.ToolUseId);

                        // Compact ALL tool results in history unconditionally.
                    // The LLM sees full content on the turn it is returned — that is sufficient.
                    // Keeping results in history (even small ones like search_in_artefact at ~200 chars)
                    // causes unbounded growth: 50+ searches × 200 chars = 10k chars re-sent every turn.
                    // If the LLM needs to re-read something, it calls the tool again.
                    // Only exception: ANCHOR_NOT_FOUND contains real file context needed for retries.
                    if (!toolResult.Content.StartsWith("Error: ANCHOR_NOT_FOUND", StringComparison.Ordinal))
                    {
                        var toolName = matchingCall?.ToolName ?? "tool";
                        return new AiToolResult(
                            toolResult.ToolUseId,
                            $"[Result returned {toolResult.Content.Length:N0} chars — content was provided to you in this turn only and is not repeated here to save context. Use {toolName} again if you need to re-read it.]");
                    }

                    return toolResult;
                }).ToList();

                aiMessages.Add(new AiMessage(
                    MessageRole.User,
                    string.Empty,
                    ToolResults: compactedToolResults));

                // Trim in-memory tool-loop history to prevent unbounded growth within a single request.
                // DB messages are already capped to 4. Keep only the last 4 tool-loop pairs (8 messages)
                // within the current request — tool results are already compacted so the LLM has context.
                const int maxToolLoopPairsInMemory = 4;
                var dbMessageCount = messages.Count;
                var toolLoopMessages = aiMessages.Count - dbMessageCount;
                var maxToolLoopMessages = maxToolLoopPairsInMemory * 2;
                if (toolLoopMessages > maxToolLoopMessages)
                {
                    var excess = toolLoopMessages - maxToolLoopMessages;
                    // Remove pairs from the start of the tool loop history (oldest first).
                    // Always remove in pairs (assistant + user) to maintain alternating message structure.
                    var toRemove = excess % 2 == 0 ? excess : excess + 1;
                    aiMessages.RemoveRange(dbMessageCount, toRemove);
                }

                turnsRemaining--;

                // Prototype stage: exit early once the fragment save budget is exhausted.
                // Prevents the LLM cycling through all fragments repeatedly after the initial build.
                // 15 saves = full build (8 fragments) + one full refinement pass (7 more).
                // Count is derived from savedArtefacts (accumulated this request) filtered to fragment paths.
                if (stageType == StageType.Prototype)
                {
                    var fragmentsSaved = savedArtefacts.Count(
                        artefact => artefact.FilePath.StartsWith("prototype/fragments/", StringComparison.OrdinalIgnoreCase));
                    if (fragmentsSaved >= maxPrototypeFragmentSaves)
                    {
                        _logger.LogInformation(
                            "Prototype fragment save budget exhausted ({Count}/{Max}) — exiting tool loop early to prevent refinement loop",
                            fragmentsSaved, maxPrototypeFragmentSaves);
                        break;
                    }
                }

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
                try
                {
                    await SaveContinuationCheckpointAsync(
                        conversation,
                        projectId,
                        request.Content,
                        fullResponse.ToString(),
                        savedArtefacts,
                        savedParkingLotItems,
                        createdBy,
                        cancellationToken);
                }
                catch (Exception checkpointException)
                {
                    _logger.LogWarning(
                        checkpointException,
                        "Failed to persist continuation checkpoint for conversation {ConversationId}",
                        conversation.Id);
                }

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
            else
            {
                _logger.LogWarning(
                    "AI stream completed with no assistant text for conversation {ConversationId}",
                    conversation.Id);

                var emptyResponseEvent = JsonSerializer.Serialize(new
                {
                    error = "No response returned from AI",
                    reason = "empty_assistant_output"
                });
                await Response.WriteAsync($"event: error\ndata: {emptyResponseEvent}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
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

    private static string GetStageTypePgName(Domain.Enums.StageType stageType)
    {
        var field = stageType.GetType().GetField(stageType.ToString());
        var attr = field?.GetCustomAttributes(typeof(NpgsqlTypes.PgNameAttribute), false)
            .FirstOrDefault() as NpgsqlTypes.PgNameAttribute;
        return attr?.PgName ?? stageType.ToString().ToLowerInvariant();
    }

    private static Domain.AggregatesModel.RequirementChangeAggregate.ImpactLevel ParseImpactLevel(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "possible" => Domain.AggregatesModel.RequirementChangeAggregate.ImpactLevel.Possible,
            "definite" => Domain.AggregatesModel.RequirementChangeAggregate.ImpactLevel.Definite,
            _ => Domain.AggregatesModel.RequirementChangeAggregate.ImpactLevel.None,
        };
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

    internal async Task<string> ExecuteToolCallWithRetryAsync(
        AiToolCall toolCall,
        Conversation conversation,
        List<Artefact> savedArtefacts,
        List<ParkingLotItem> savedParkingLotItems,
        IReadOnlyList<ParkingLotItem> projectParkingLotItems,
        string createdBy,
        Guid projectId,
        StageType? stageType,
        HashSet<string> filesReadThisRequest,
        HashSet<string> filesReadThisTurn,
        StrongBox<int> searchCountThisTurn,
        StrongBox<bool> postSearchReadBlocked,
        StrongBox<bool> zeroMatchToolBlocked,
        bool prototypeSingleFile,
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
                    filesReadThisRequest,
                    filesReadThisTurn,
                    searchCountThisTurn,
                    postSearchReadBlocked,
                    zeroMatchToolBlocked,
                    prototypeSingleFile,
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

    internal async Task<string> ExecuteToolCallAsync(
        AiToolCall toolCall,
        Conversation conversation,
        List<Artefact> savedArtefacts,
        List<ParkingLotItem> savedParkingLotItems,
        IReadOnlyList<ParkingLotItem> projectParkingLotItems,
        string createdBy,
        Guid projectId,
        StageType? stageType,
        HashSet<string> filesReadThisRequest,
        HashSet<string> filesReadThisTurn,
        StrongBox<int> searchCountThisTurn,
        StrongBox<bool> postSearchReadBlocked,
        StrongBox<bool> zeroMatchToolBlocked,
        bool prototypeSingleFile,
        CancellationToken cancellationToken)
    {
        const int maxSearchesPerTurn = 5;
        var root = toolCall.Input.RootElement;

        _logger.LogInformation(
            "Tool guard check: zeroMatchToolBlocked={ZeroMatchToolBlocked}, tool={ToolName}",
            zeroMatchToolBlocked.Value,
            toolCall.ToolName);

        // Zero-match hard block: after a DOM zero-match result, block apply_to_scope calls.
        // Other tools are allowed to pass through.
        if (zeroMatchToolBlocked.Value && toolCall.ToolName == PipelineToolDefinitions.ApplyToScope)
        {
            return "HARD STOP ALREADY TRIGGERED: DOM search returned zero matches. " +
                   "Do not call more tools in this turn. Ask the user for the exact CSS class " +
                   "name or pasted HTML from browser inspector, then retry from that input.";
        }

        switch (toolCall.ToolName)
        {
            case PipelineToolDefinitions.SaveArtefact:
            {
                var filePath = root.GetProperty("file_path").GetString()!;
                var content = root.GetProperty("content").GetString()!;

                // Single-file prototype corruption guard: reject saves of prototype/index.html
                // that are not complete valid HTML documents. Prevents context-truncation from
                // writing partial content (e.g. 40 bytes) that corrupts the prototype.
                if (prototypeSingleFile &&
                    filePath.Equals(PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase) &&
                    (!content.TrimStart().StartsWith("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase) ||
                     !content.TrimEnd().EndsWith("</html>", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning(
                        "Tool save_artefact rejected: prototype/index.html is not a complete HTML document ({Length} chars)",
                        content.Length);
                    return "Error: INVALID_PROTOTYPE_HTML: The content must be a complete HTML document — " +
                           "starting with <!DOCTYPE html> and ending with </html>. " +
                           "Your response was truncated. Do not save partial HTML — regenerate the complete prototype.";
                }

                // Single-file prototype banner guard: reject saves of prototype/index.html
                // that are missing the required PROTOTYPE ONLY safety banner.
                // The banner identifies the artefact as a throwaway prototype, not production UI.
                if (prototypeSingleFile &&
                    filePath.Equals(PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase) &&
                    !content.Contains("PROTOTYPE ONLY", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Tool save_artefact rejected: prototype/index.html is missing the PROTOTYPE ONLY banner ({Length} chars)",
                        content.Length);
                    return "Error: MISSING_PROTOTYPE_BANNER: The prototype HTML must include a visible 'PROTOTYPE ONLY' banner. " +
                           "Add an amber full-width banner at the very top of <body> containing the text 'PROTOTYPE ONLY'. " +
                           "This is a mandatory safety marker that identifies the artefact as a throwaway prototype.";
                }

                // Clinical safety guard: reject saves of prototype/index.html containing
                // format-plausible NHS numbers. A 10-digit number matching NNN NNN NNNN
                // could be mistaken for a real patient identifier if the prototype is
                // shared outside the team.
                if (prototypeSingleFile &&
                    filePath.Equals(PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase) &&
                    System.Text.RegularExpressions.Regex.IsMatch(
                        content,
                        @"\d{3}\s?\d{3}\s?\d{4}",
                        System.Text.RegularExpressions.RegexOptions.None))
                {
                    _logger.LogWarning(
                        "Tool save_artefact rejected: prototype/index.html contains a format-plausible NHS number ({Length} chars)",
                        content.Length);
                    return "Error: PLAUSIBLE_NHS_NUMBER_DETECTED: The prototype HTML contains a number matching " +
                           "the NHS number format (NNN NNN NNNN). Use obviously fake identifiers such as NHS: XXXX or Patient-001 — never real or plausible NHS numbers. " +
                           "Remove all format-plausible NHS numbers before saving.";
                }

                var duplicateInjectedHeading = FindDuplicateInjectedSectionHeading(content);
                if (filePath.StartsWith("requirements/REQ-", StringComparison.OrdinalIgnoreCase) &&
                    duplicateInjectedHeading is not null)
                {
                    _logger.LogWarning(
                        "Tool save_artefact rejected for duplicate injected section heading: {FilePath} / {Heading}",
                        filePath, duplicateInjectedHeading);
                    return $"Error: DUPLICATE_SECTION_HEADING: '{duplicateInjectedHeading}' appears more than once in '{filePath}'. " +
                           "Do not append the same stage-added section twice. Keep one canonical section and remove or replace duplicates before saving.";
                }

                if (stageType == StageType.Prototype &&
                    filePath.Equals(PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase))
                {
                    var existingPrototype = await _artefactRepository.GetByProjectAndFilePathAsync(
                        projectId, filePath, cancellationToken);

                    if (existingPrototype is not null)
                    {
                        // Allow full regeneration when the existing prototype is too large to edit via edit_artefact.
                        // edit_artefact requires exact old_str matches which is not feasible on files >100KB.
                        var existingSizeBytes = existingPrototype.SizeBytes ?? 0;
                        var contentIsLargeForEditing = existingSizeBytes > 50_000 || content.Length > 50_000;

                        if (ShouldBlockPrototypeRegenerationSave(
                                stageType,
                                filePath,
                                prototypeSingleFile,
                                prototypeAlreadyExists: true,
                                contentIsLargeForEditing))
                        {
                            _logger.LogWarning(
                                "Tool save_artefact rejected for existing prototype HTML regeneration: {FilePath}",
                                filePath);
                            return "Error: PROTOTYPE_REGENERATION_BLOCKED: 'prototype/index.html' already exists. " +
                                   "For iterative changes, you must use edit_artefact with exact anchor strings. " +
                                   "Only the initial prototype creation may use save_artefact for prototype/index.html.";
                        }

                        if (!prototypeSingleFile && contentIsLargeForEditing)
                        {
                            _logger.LogInformation(
                                "Tool save_artefact: allowing full regeneration of prototype/index.html " +
                                "(existing size: {ExistingBytes} bytes — too large for targeted edit_artefact)",
                                existingSizeBytes);
                        }
                    }
                }

                if (stageType == StageType.Prototype && !prototypeSingleFile)
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
                    _timeProvider,
                    true);

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

                searchCountThisTurn.Value = 0; // Reset after successful mutation
                // Clear post-search read block after successful save (a form of mutation)
                postSearchReadBlocked.Value = false;
                zeroMatchToolBlocked.Value = false;

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

                // Post-search read block: after DOM search found matches, prevent re-reading fragments
                // and REQ files until a mutation succeeds. This stops the agent from burning budget
                // on reads it shouldn't be making.
                if (postSearchReadBlocked.Value)
                {
                    var isFragmentOrReq = filePath.StartsWith("prototype/fragments/", StringComparison.OrdinalIgnoreCase) ||
                                         filePath.StartsWith("requirements/", StringComparison.OrdinalIgnoreCase);
                    if (isFragmentOrReq)
                    {
                        _logger.LogWarning(
                            "Tool get_artefact blocked: post-search read attempted after DOM search found matches — {FilePath}",
                            filePath);
                        return "BLOCKED: You have a search result with matches. Call apply_to_scope immediately to write the mutation. " +
                               "Do not read more files until the mutation completes. Proceeding otherwise burns your read budget. " +
                               "Use the selector from your search result and call apply_to_scope now.";
                    }
                }

                // Block direct reads of the assembled prototype — it is the output, not a fragment to edit.
                // The LLM should only read/edit fragment files (prototype/fragments/*).
                if (ShouldBlockPrototypeRegenerationRead(stageType, filePath, prototypeSingleFile))
                {
                    _logger.LogWarning("Tool get_artefact: blocked read of assembled prototype/index.html — redirect to fragments");
                    return "Error: prototype/index.html is the assembled output and cannot be read directly. " +
                           "To edit the prototype, read and modify the fragment files under prototype/fragments/ instead. " +
                           "Use list_artefacts to see all fragments.";
                }

                // Structural guard: once the prototype is built, requirements/* are unreadable
                // during a Prototype edit. _shell.html mirrors the migration step's own build
                // detection — an edit works on the fragments, never the requirements.
                if (stageType == StageType.Prototype &&
                    !prototypeSingleFile &&
                    filePath.StartsWith("requirements/", StringComparison.OrdinalIgnoreCase))
                {
                    var shellFragment = await _artefactRepository.GetByProjectAndFilePathAsync(
                        projectId, "prototype/fragments/_shell.html", cancellationToken);
                    var readGuardError = PrototypeReadGuard.ValidateGetArtefact(
                        stageType,
                        filePath,
                        prototypeAlreadyBuilt: shellFragment is not null,
                        prototypeSingleFile: prototypeSingleFile);
                    if (readGuardError is not null)
                    {
                        _logger.LogWarning(
                            "Tool get_artefact blocked: requirements read during built-prototype edit — {FilePath}", filePath);
                        return readGuardError;
                    }
                }

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

                // Build the result. Large non-prototype HTML/CSS files return a structural
                // outline; prototype HTML fragments are always returned in full (the outline is
                // a CSS digest that misreads markup-heavy fragments as near-empty stubs, and a
                // faithful full rewrite needs the complete current markup). Prototype fragments
                // are exempt from the read budget, so a repeated full read within one request is
                // replaced by a pointer back to the agent's existing context.
                const int largeFileThreshold = 50_000;
                var alreadyReadThisRequest = filesReadThisRequest.Contains(filePath);
                var getArtefactResult = BuildGetArtefactResult(
                    filePath, artefactContent, artefact.Version, alreadyReadThisRequest, largeFileThreshold, prototypeSingleFile);
                filesReadThisRequest.Add(filePath);

                _logger.LogInformation(
                    "Tool get_artefact: returned {FilePath} v{Version} ({Length} chars, alreadyRead={AlreadyRead})",
                    filePath, artefact.Version, artefactContent.Length, alreadyReadThisRequest);
                return getArtefactResult;
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
                var requirementCompletedEvent = new System.Text.Json.Nodes.JsonObject
                {
                    ["requirementId"] = conversation.RequirementId,
                    ["conversationId"] = conversation.Id.ToString()
                }.ToJsonString();
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

            case PipelineToolDefinitions.SearchInArtefact:
            {
                var filePath = root.GetProperty("file_path").GetString()!;
                var query = root.GetProperty("query").GetString()!;

                // Plan 3f: serve searches of HTML prototype artefacts from the structured DOM
                // search, which returns elements with their real ClassList — so the agent receives
                // an authoritative selector instead of mining class names out of raw HTML lines
                // (where a neighbouring element's class can be lifted and mangled). Non-HTML
                // fragments (_styles.css, _app.js, data.js) keep the text search.
                if (PrototypeSearchRouter.ShouldRouteToDomSearch(filePath, _tokenOptimisationOptions.PrototypeDomModeEnabled)
                    && _prototypeDomSearchService is not null)
                {
                    var domResult = await _prototypeDomSearchService.SearchAsync(
                        new PrototypeDomSearchRequest(projectId, filePath, query, createdBy),
                        cancellationToken);

                    _logger.LogInformation(
                        "Tool search_in_artefact: DOM search across fragments for '{Query}' — {Count} matches",
                        query, domResult.Matches.Count);

                    if (domResult.Matches.Count == 0)
                    {
                        zeroMatchToolBlocked.Value = true;
                        return $"No elements found matching '{query}' in prototype fragments. " +
                               "STOP — do not guess a selector or retry with variations. Tell the user you " +
                               "could not find a matching element, and ask them to provide the exact CSS class " +
                               "name (e.g. \".urgency-arrow\") or paste the HTML element from the browser " +
                               "inspector (right-click element → Inspect → copy the element) so you can " +
                               "identify the exact selector.";
                    }

                    // Post-search read block: set flag after successful search with matches
                    // This prevents re-reads and REQ reads until a mutation completes
                    postSearchReadBlocked.Value = true;

                    if (domResult.Matches.Count == 1)
                    {
                        var match = domResult.Matches[0];
                        var scope = System.IO.Path.GetFileNameWithoutExtension(match.FragmentPath);
                        var selector = match.ClassList.Count > 0
                            ? $".{match.ClassList[0]}"
                            : (match.CssSelector ?? $"#{match.NodeKey.Split('|').Last()}");
                        return $"Found 1 match for '{query}':\n" +
                               $"  node_id: {match.NodeKey}\n" +
                               $"  tag: {match.TagName} | text: {match.TextSnippet} | fragment: {match.FragmentPath}\n\n" +
                               $"Ready to apply. Use this exact call:\n" +
                               $"  apply_to_scope(scope=\"{scope}\", selector=\"{selector}\", ...)\n\n" +
                               "Replace ... with operation, attribute, strategy as needed.";
                    }

                    var candidateMatches = domResult.Matches;
                    var singleFragment = candidateMatches
                        .Select(match => match.FragmentPath)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count() == 1;
                    var confirmedSelector = singleFragment
                        ? _prototypeDomSearchService.ResolveConfirmedSelectorFromMatches(candidateMatches)
                        : null;
                    return BuildDomSearchMultiMatchResult(query, candidateMatches, confirmedSelector);
                }

                // Enforce non-DOM search limit — after 5 non-DOM searches without a mutation,
                // return a hard stop to force the agent to act rather than keep searching.
                searchCountThisTurn.Value++;
                if (searchCountThisTurn.Value > maxSearchesPerTurn)
                    return $"HARD STOP: You have called search_in_artefact {searchCountThisTurn.Value} non-DOM times in this turn without making an edit. " +
                           "Stop searching. You already have the anchor text you need. " +
                           "Call edit_artefact or save_artefact now. Do not search again.";

                var artefact = await _artefactRepository.GetByProjectAndFilePathAsync(projectId, filePath, cancellationToken);
                if (artefact is null)
                    return $"Artefact '{filePath}' not found. Use list_artefacts to see available files.";

                var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);
                if (content is null)
                    return $"Artefact '{filePath}' content could not be retrieved.";

                var normalisedContent = ToNfc(content);
                var searchResult = BuildSearchResult(normalisedContent, query, filePath, artefact.Version);

                _logger.LogInformation(
                    "Tool search_in_artefact: searched {FilePath} v{Version} for '{Query}' ({Length} chars file)",
                    filePath, artefact.Version, query, content.Length);

                _logger.LogInformation(
                    "Tool search_in_artefact: result preview = {Preview}",
                    searchResult.Length > 300 ? searchResult[..300] : searchResult);

                // Unblock edit_artefact for this file — the result contains real verbatim snippets
                filesReadThisRequest.Add(filePath);

                return searchResult;
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

                // Require get_artefact to be called first this request so Claude anchors against
                // the real file content, not memory or a previous cached version.
                if (!filesReadThisRequest.Contains(filePath) &&
                    !(prototypeSingleFile && !string.IsNullOrEmpty(oldStr)))
                {
                    _logger.LogWarning(
                        "Tool edit_artefact blocked: {FilePath} not read this request — forcing get_artefact first",
                        filePath);
                    return $"Error: FILE_NOT_READ: You must call get_artefact on '{filePath}' before editing it. " +
                           "Read the file first to anchor your edit against the real content, then call edit_artefact.";
                }

                // Block edits where get_artefact was called in the same turn — the file content
                // is not yet in Claude's context window and the anchor will fail.
                if (filesReadThisTurn.Contains(filePath))
                {
                    _logger.LogWarning(
                        "Tool edit_artefact blocked: {FilePath} was read in the same turn — must wait for next turn",
                        filePath);
                    return $"Error: FILE_READ_SAME_TURN: You called search_in_artefact and edit_artefact for '{filePath}' " +
                           "in the same response. The search results are not in your context yet. " +
                           "On your next response, copy a verbatim snippet from the search_in_artefact result you just received and use it as old_str.";
                }

                // Load latest version
                var existingArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
                    projectId, filePath, cancellationToken);

                if (existingArtefact is null)
                    return $"Error: FILE_NOT_FOUND: No artefact found at path '{filePath}'. Use list_artefacts to see available files.";

                var existingContent = await _artefactStorageService.GetContentAsync(
                    existingArtefact.S3Key, cancellationToken);

                if (existingContent is null)
                    return $"Error: FILE_NOT_FOUND: Artefact content could not be retrieved for '{filePath}'.";

                // Normalise to NFC so emoji and multi-codepoint characters compare correctly
                // regardless of whether Claude or the stored file used a different normalisation form.
                var normalisedContent = ToNfc(existingContent);
                var normalisedOldStr = ToNfc(oldStr);

                var occurrences = CountOccurrences(normalisedContent, normalisedOldStr);

                if (occurrences == 0)
                {
                    _logger.LogWarning(
                        "Tool edit_artefact: ANCHOR_NOT_FOUND for {FilePath} (old_str length {Length})",
                        filePath, oldStr.Length);

                    // Remove the file from filesReadThisRequest so Claude is forced to search again
                    filesReadThisRequest.Remove(filePath);

                    return $"Error: ANCHOR_NOT_FOUND: The anchor string was not found in '{filePath}'. " +
                           "Do NOT retry with a different guess. Instead: call search_in_artefact with a distinctive keyword " +
                           "from the area you want to change (e.g. 'nav', 'background-colour', 'header', 'banner'). " +
                           "Copy old_str verbatim character-for-character from the search results — never reconstruct from memory.";
                }

                if (occurrences > 1)
                {
                    _logger.LogWarning(
                        "Tool edit_artefact: ANCHOR_AMBIGUOUS for {FilePath} ({Count} occurrences, old_str length {Length})",
                        filePath, occurrences, oldStr.Length);
                    return $"Error: ANCHOR_AMBIGUOUS: The anchor string appears {occurrences} times in '{filePath}'. " +
                           "Use a longer, more unique anchor string.";
                }

                // Apply edit — replace on the normalised content so the substitution site is correct
                var normalisedNewStr = ToNfc(newStr);
                var updatedContent = normalisedContent.Replace(normalisedOldStr, normalisedNewStr, StringComparison.Ordinal);
                var duplicateInjectedHeading = FindDuplicateInjectedSectionHeading(updatedContent);
                if (filePath.StartsWith("requirements/REQ-", StringComparison.OrdinalIgnoreCase) &&
                    duplicateInjectedHeading is not null)
                {
                    _logger.LogWarning(
                        "Tool edit_artefact rejected for duplicate injected section heading: {FilePath} / {Heading}",
                        filePath, duplicateInjectedHeading);
                    return $"Error: DUPLICATE_SECTION_HEADING: '{duplicateInjectedHeading}' appears more than once in '{filePath}' after the edit. " +
                           "Do not append the same stage-added section twice. Read the file, keep one canonical section, and remove duplicates instead of adding another copy.";
                }

                var bytesChanged = Math.Abs(
                    System.Text.Encoding.UTF8.GetByteCount(updatedContent) -
                    System.Text.Encoding.UTF8.GetByteCount(normalisedContent));

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
                    _timeProvider,
                    true);

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

                searchCountThisTurn.Value = 0; // Reset after successful mutation

                return $"Edited {filePath} (version {nextVersion}, {bytesChanged} bytes changed, total {System.Text.Encoding.UTF8.GetByteCount(updatedContent)} bytes)";
            }


            case PipelineToolDefinitions.ApplyToScope:
            {
                if (!_tokenOptimisationOptions.PrototypeDomModeEnabled || _prototypeDomSearchService is null || _prototypeDomMutationService is null)
                    return "Error: apply_to_scope is only available when PrototypeDomModeEnabled is enabled.";

                var scope = root.TryGetProperty("scope", out var scopeProp) ? scopeProp.GetString() : null;
                var selector = root.TryGetProperty("selector", out var selectorProp) ? selectorProp.GetString() : null;
                var operation = root.TryGetProperty("operation", out var operationProp) ? operationProp.GetString() : null;
                var strategy = root.TryGetProperty("strategy", out var strategyProp) ? strategyProp.GetString() : null;
                var value = root.TryGetProperty("value", out var valueProp) ? valueProp.GetString() : null;
                var attribute = root.TryGetProperty("attribute", out var attrProp) ? attrProp.GetString() : null;

                if (string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(selector) ||
                    string.IsNullOrWhiteSpace(operation) || string.IsNullOrWhiteSpace(strategy))
                    return "Error: apply_to_scope requires scope, selector, operation, and strategy.";

                // Scope is a fragment filename. Resolve it directly against the loaded fragment
                // set (filename match) — never a content search, which after Phase 4 returns no
                // node for a filename and silently degraded the listing to every fragment.
                var listResult = await _prototypeDomSearchService.ListAllAsync(
                    new PrototypeDomListRequest(projectId, selector, scope, createdBy),
                    cancellationToken);

                if (listResult.Matches.Count == 0)
                {
                    _logger.LogWarning("apply_to_scope: no elements matched selector='{Selector}' scope='{Scope}'", selector, scope);

                    // Plan 3f enforcement: refuse to write. Return the elements ACTUALLY present in the
                    // scope so the correct selector is discoverable. A wrong selector can never be
                    // silently written, and the agent cannot narrate false success.
                    var actualElements = await _prototypeDomSearchService.ListAllInScopeAsync(
                        projectId, scope, cancellationToken);

                    if (actualElements.Matches.Count == 0)
                        return $"apply_to_scope: no elements matched selector='{selector}' and scope='{scope}' contains no editable elements. " +
                               "Verify the scope is a real fragment name (filename without extension). Ask the user to paste the HTML element from the browser inspector.";

                    // If the scope's elements collapse to one shared class, name it as the confirmed
                    // selector — the agent must retry with exactly this, never its own guess.
                    // Heuristic note: the single shared class may also appear on sibling or nested
                    // elements when nodes carry multiple classes. This is acceptable for LLM guidance,
                    // but not a guarantee of one-to-one targeting.
                    var confirmedSelector = _prototypeDomSearchService.ResolveConfirmedSelectorFromMatches(
                        actualElements.Matches);
                    if (confirmedSelector is not null)
                    {
                        return $"NOTHING WAS WRITTEN. selector='{selector}' matched 0 elements in scope='{scope}'. " +
                               $"The confirmed selector for this scope is '{confirmedSelector}'. " +
                               $"Retry apply_to_scope now with selector='{confirmedSelector}' and the same operation/attribute/strategy. " +
                               "Do not respond to the user until a write succeeds.";
                    }

                    var present = actualElements.Matches.Take(10).Select(match =>
                    {
                        var cls = match.ClassList.Count > 0 ? "." + string.Join(".", match.ClassList) : "(no class)";
                        return $"  {match.TagName} {cls} — \"{match.TextSnippet}\"";
                    });
                    return $"NOTHING WAS WRITTEN. selector='{selector}' matched 0 elements in scope='{scope}'. " +
                           $"Do NOT claim success. The elements actually present in this scope are:\n" +
                           string.Join("\n", present) + "\n\n" +
                           "Use a selector taken from the list above (do not invent one), or ask the user to paste the exact HTML element.";
                }

                // Structural guard (Phase 2): invented classes are physically unwritable and
                // CSS authoring is rejected. Class existence is a set-membership test, so the full
                // uncapped class set for the scope is used (not the capped element listing).
                var existingScopeClasses = await _prototypeDomSearchService.GetClassNamesInScopeAsync(
                    projectId, scope, cancellationToken);
                var guardError = PrototypeApplyToScopeGuard.Validate(
                    scope, selector, operation, value, existingScopeClasses);
                if (guardError is not null)
                {
                    _logger.LogWarning("apply_to_scope guard rejected: {GuardError}", guardError);
                    return guardError;
                }

                // Reject invalid strategy/operation combinations
                if (operation.Equals("insert_adjacent_html", StringComparison.OrdinalIgnoreCase) &&
                    strategy.Equals("generate_from_context", StringComparison.OrdinalIgnoreCase))
                {
                    const string insertAdjacentHtmlError = "Error: insert_adjacent_html requires strategy=literal. generate_from_context generates text values and cannot produce HTML to insert. Provide the HTML to insert as the value parameter and use strategy=literal.";
                    return insertAdjacentHtmlError;
                }

                // Derive values using the selected strategy
                IReadOnlyList<ApplyToScopeValueResult> valueResults;
                switch (strategy)
                {
                    case "literal":
                        if (string.IsNullOrWhiteSpace(value))
                            return "Error: apply_to_scope with strategy=literal requires a value parameter.";
                        valueResults = await new LiteralStrategy().DeriveValuesAsync(listResult.Matches, value, cancellationToken);
                        break;
                    case "derive_from_text_content":
                        valueResults = await new DeriveFromTextContentStrategy().DeriveValuesAsync(listResult.Matches, null, cancellationToken);
                        break;
                    case "generate_from_context":
                        valueResults = await new GenerateFromContextStrategy(_aiService).DeriveValuesAsync(listResult.Matches, null, cancellationToken);
                        break;
                    default:
                        return $"Error: apply_to_scope strategy='{strategy}' is not valid. Use literal, derive_from_text_content, or generate_from_context.";
                }

                // Parse operation
                if (!Enum.TryParse<PrototypeDomMutationOperation>(
                    string.Concat(operation.Split('_').Select(word => char.ToUpperInvariant(word[0]) + word[1..])),
                    out var mutationOperation))
                {
                    return $"Error: apply_to_scope operation='{operation}' is not valid.";
                }

                // Apply mutations
                var requests = valueResults.Select(valueResult => new PrototypeDomMutationRequest(
                    ProjectId: projectId,
                    FragmentPath: valueResult.FragmentPath,
                    NodeKey: valueResult.NodeKey,
                    Operation: mutationOperation,
                    Attribute: attribute,
                    Value: valueResult.Value,
                    CreatedBy: createdBy)).ToList();

                var batchResult = await _prototypeDomMutationService.ApplyBatchMutationAsync(requests, cancellationToken);

                _logger.LogInformation(
                    "apply_to_scope: scope='{Scope}' selector='{Selector}' operation='{Operation}' strategy='{Strategy}' — applied {Success}/{Total}",
                    scope, selector, operation, strategy, batchResult.SuccessfulMutations, batchResult.TotalMutations);

                // Trigger assembly after successful DOM mutations so index.html stays current.
                // Regression fix: apply_to_scope was completing mutations without reassembling.
                if (batchResult.SuccessfulMutations > 0 && _tokenOptimisationOptions.PrototypeFragmentsEnabled)
                {
                    await _prototypeAssemblyService.AssemblePrototypeAsync(projectId, cancellationToken);
                }

                if (batchResult.SuccessfulMutations == batchResult.TotalMutations)
                {
                    searchCountThisTurn.Value = 0; // Reset after successful mutation
                    postSearchReadBlocked.Value = false; // Clear post-search read block after mutation completes
                    zeroMatchToolBlocked.Value = false;
                    return $"Applied {batchResult.SuccessfulMutations} of {batchResult.TotalMutations} mutations successfully.";
                }

                // Log failures for debugging
                foreach (var failedResult in batchResult.Results.Where(result => !result.Success))
                    _logger.LogWarning("apply_to_scope failure: node={NodeKey} message={Message}", failedResult.NodeKey, failedResult.Message);

                var failures = batchResult.Results
                    .Where(result => !result.Success)
                    .Select(result => $"{result.NodeKey}: {result.Message}")
                    .Take(5)
                    .ToList();
                return $"PARTIAL FAILURE: applied {batchResult.SuccessfulMutations} of {batchResult.TotalMutations}. Failures: {string.Join("; ", failures)}. " +
                    "Consider using save_artefact to fully rebuild this fragment instead of surgical edits.";
            }

            case PipelineToolDefinitions.ProposeRequirementChange:
            {
                var reqId = root.GetProperty("req_id").GetString()!;
                var changeTypeStr = root.GetProperty("change_type").GetString()!;
                var rationale = root.GetProperty("rationale").GetString()!;
                var proposedAcText = root.TryGetProperty("proposed_ac_text", out var acProp)
                    ? acProp.GetString() : null;

                var changeType = changeTypeStr.ToLowerInvariant() switch
                {
                    "gap" => Domain.AggregatesModel.RequirementChangeAggregate.ChangeType.Gap,
                    "clarification" => Domain.AggregatesModel.RequirementChangeAggregate.ChangeType.Clarification,
                    "contradiction" => Domain.AggregatesModel.RequirementChangeAggregate.ChangeType.Contradiction,
                    _ => Domain.AggregatesModel.RequirementChangeAggregate.ChangeType.Gap
                };

                var raisingPipeline = stageType.HasValue
                    ? $"pipeline_{(int)stageType.Value + 1:D2}_{GetStageTypePgName(stageType.Value)}"
                    : "pipeline_unknown";

                var csImpact = root.TryGetProperty("clinical_safety_impact", out var csProp)
                    ? ParseImpactLevel(csProp.GetString()) : Domain.AggregatesModel.RequirementChangeAggregate.ImpactLevel.None;
                var igImpact = root.TryGetProperty("ig_impact", out var igProp)
                    ? ParseImpactLevel(igProp.GetString()) : Domain.AggregatesModel.RequirementChangeAggregate.ImpactLevel.None;
                var secImpact = root.TryGetProperty("security_impact", out var secProp)
                    ? ParseImpactLevel(secProp.GetString()) : Domain.AggregatesModel.RequirementChangeAggregate.ImpactLevel.None;

                var command = new ProposeRequirementChangeCommand(
                    ProjectId: projectId,
                    ReqId: reqId,
                    ChangeType: changeType,
                    RaisingPipeline: raisingPipeline,
                    RaisingPipelineConversationId: conversation.Id,
                    ProposedAcText: proposedAcText,
                    Rationale: rationale,
                    CreatedBy: createdBy,
                    ClinicalSafetyImpact: csImpact,
                    IgImpact: igImpact,
                    SecurityImpact: secImpact);

                var result = await _proposeRequirementChangeHandler.Handle(command, cancellationToken);

                return $"CHANGE_PROPOSED: change_id={result.ChangeId}\n" +
                       "Your proposed change is pending human approval in the UI.\n" +
                       "Do not apply this change yourself. Continue your current pipeline work.";
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

    private static string BuildFileOutline(string content, string filePath)
    {
        var sb = new System.Text.StringBuilder();
        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        var isHtmlOrCss = ext is ".html" or ".htm" or ".css";

        if (!isHtmlOrCss)
        {
            // For non-HTML/CSS files, return first 2000 chars as a preview
            sb.AppendLine(content.Length > 2000 ? content[..2000] + "\n\n[...truncated...]" : content);
            return sb.ToString();
        }

        sb.AppendLine("### CSS Custom Properties (`:root` variables)");
        var rootMatch = System.Text.RegularExpressions.Regex.Match(
            content, @":root\s*\{([^}]+)\}", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (rootMatch.Success)
        {
            // Extract just the property names (no values) to keep it compact
            var props = System.Text.RegularExpressions.Regex.Matches(
                rootMatch.Groups[1].Value, @"--[\w-]+");
            foreach (System.Text.RegularExpressions.Match prop in props)
                sb.Append("  ").AppendLine(prop.Value);
        }
        else
        {
            sb.AppendLine("  (none found)");
        }

        sb.AppendLine();
        sb.AppendLine("### CSS Selectors (classes and IDs)");
        var selectorMatches = System.Text.RegularExpressions.Regex.Matches(
            content,
            @"(?:^|\})\s*((?:[.#][\w-]+(?:\s+[.#>+~]?[\w-]+)*(?::[:\w-]+)?(?:\s*,\s*)?)+)\s*\{",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match m in selectorMatches)
        {
            var selector = m.Groups[1].Value.Trim();
            if (selector.Length > 0 && seen.Add(selector))
                sb.Append("  ").AppendLine(selector);
        }

        sb.AppendLine();
        sb.AppendLine("### HTML Section Comments");
        var commentMatches = System.Text.RegularExpressions.Regex.Matches(
            content, @"<!--\s*([A-Z][A-Z\s/\-]{2,60}?)\s*-->");
        foreach (System.Text.RegularExpressions.Match m in commentMatches)
            sb.Append("  <!-- ").Append(m.Groups[1].Value.Trim()).AppendLine(" -->");

        return sb.ToString();
    }

    internal static string BuildPrototypeIntentRoutingDirective(
        StageType? stageType,
        string? latestUserMessage,
        IReadOnlyList<Artefact> artefactManifest,
        bool prototypeSingleFile)
    {
        if (stageType != StageType.Prototype ||
            prototypeSingleFile ||
            string.IsNullOrWhiteSpace(latestUserMessage))
        {
            return string.Empty;
        }

        var hasExistingPrototypeHtml = artefactManifest.Any(artefact =>
            artefact.FilePath.Equals(PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase));

        if (!hasExistingPrototypeHtml)
        {
            return string.Empty;
        }

        var normalisedRequest = latestUserMessage.ToLowerInvariant();
        var targetedChangeHints = new[]
        {
            "replace", "swap", "update", "change", "fix", "tweak", "adjust", "refine", "improve",
            // Styling / reskin requests — these are always targeted edits, never regenerations
            "apply", "style", "restyle", "theme", "colour", "design", "emis", "token",
            "font", "spacing", "layout", "nav", "sidebar", "header", "button", "badge", "card"
        };
        var fullRegenerationHints = new[]
        {
            "regenerate", "from scratch", "start over", "full redesign", "full rewrite", "rebuild"
        };

        // If a prototype already exists, default to targeted edit mode unless the user explicitly
        // asked for a full regeneration. "Apply EMIS-X UI", "style the nav", "change font" etc.
        // are all restyling tasks — the LLM should NOT re-read requirements files for these.
        var isTargetedChange = !fullRegenerationHints.Any(normalisedRequest.Contains) &&
                               (targetedChangeHints.Any(normalisedRequest.Contains) ||
                                // Default: if prototype exists and no regeneration hint, assume targeted
                                true);

        if (!isTargetedChange)
        {
            return string.Empty;
        }

        return @"**IMPORTANT — API-ENFORCED ROUTING: User intent is a targeted update to an existing prototype.**
- Do NOT call get_artefact or list_artefacts on requirements files (REQ-*.md, manifest.md etc.)
- Do NOT read project requirements to understand what to build — the structure already exists
- DO call get_artefact on prototype/index.html to get its structural outline, then use search_in_artefact for specific sections
- DO apply changes as surgical edit_artefact calls targeting only the affected CSS/HTML sections
- This is a RESTYLE task: apply design tokens, update colours/fonts/layout. The existing HTML structure stays.";
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

    internal static bool ShouldBlockPrototypeRegenerationRead(
        StageType? stageType,
        string filePath,
        bool prototypeSingleFile)
    {
        return stageType == StageType.Prototype &&
               !prototypeSingleFile &&
               filePath.Equals(PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ShouldBlockPrototypeRegenerationSave(
        StageType? stageType,
        string filePath,
        bool prototypeSingleFile,
        bool prototypeAlreadyExists,
        bool contentIsLargeForEditing)
    {
        return stageType == StageType.Prototype &&
               !prototypeSingleFile &&
               filePath.Equals(PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase) &&
               prototypeAlreadyExists &&
               !contentIsLargeForEditing;
    }

    private static string ToNfc(string text)
    {
        return text.ToNfc();
    }

    /// <summary>
    /// Builds the get_artefact tool result. Large non-prototype HTML/CSS files return a
    /// compact structural outline; prototype HTML fragments are always returned in full —
    /// the outline is a CSS digest that misreads markup-heavy fragments as near-empty stubs,
    /// and a faithful full rewrite needs the complete current markup. Because prototype
    /// fragments are exempt from the read budget, a repeated full read of the same fragment
    /// within one request is replaced by a pointer back to the agent's existing context to
    /// avoid re-dumping tens of thousands of tokens across the tool loop.
    /// </summary>
    internal static string BuildGetArtefactResult(
        string filePath,
        string artefactContent,
        int version,
        bool alreadyReadThisRequest,
        int largeFileThreshold,
        bool prototypeSingleFile = false)
    {
        var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        var isPrototypeHtmlFragment =
            (filePath.StartsWith("prototype/fragments/", StringComparison.OrdinalIgnoreCase)
             || (prototypeSingleFile && filePath.Equals("prototype/index.html", StringComparison.OrdinalIgnoreCase)))
            && extension is ".html" or ".htm";

        if (isPrototypeHtmlFragment && alreadyReadThisRequest)
        {
            return $"## {filePath} (v{version}) — ALREADY READ\n\n" +
                   "You have already read this fragment in full earlier in this turn. " +
                   "Its full content is in your context above. Do not read it again — " +
                   "make your edit or save the rewritten fragment now.";
        }

        if (artefactContent.Length > largeFileThreshold && !isPrototypeHtmlFragment)
        {
            var outline = BuildFileOutline(artefactContent, filePath);
            return $"## {filePath} (v{version}) — STRUCTURAL OUTLINE\n\n" +
                   $"File is {artefactContent.Length:N0} chars (too large to return in full). " +
                   $"Use search_in_artefact to retrieve specific sections, or edit_artefact directly using selectors from this outline.\n\n" +
                   outline;
        }

        return $"## {filePath} (v{version})\n\n{artefactContent}";
    }

    /// <summary>
    /// Builds the search_in_artefact result for the multi-match case. When every match is in
    /// one fragment and the matches collapse to a single confirmed selector, this returns a
    /// ready-to-run apply_to_scope call so the agent acts immediately. The "ask the user / paste
    /// the HTML" instruction is emitted only when matches span multiple fragments OR no confirmed
    /// selector can be derived — never when scope and selector are determinable from the matches.
    /// </summary>
    internal static string BuildDomSearchMultiMatchResult(
        string query,
        IReadOnlyList<PrototypeDomSearchMatch> matches,
        string? confirmedSelector)
    {
        var candidateLines = matches.Take(5).Select((match, index) =>
            $"  [{index + 1}] node_id: {match.NodeKey} | tag: {match.TagName} | text: {match.TextSnippet}");
        var header = $"Found {matches.Count} multiple/ambiguous matches for '{query}':\n" +
                     string.Join("\n", candidateLines) + "\n\n";

        if (confirmedSelector is not null)
        {
            var scope = System.IO.Path.GetFileNameWithoutExtension(matches[0].FragmentPath);
            return header +
                   $"All {matches.Count} matches are in fragment \"{scope}\" and share one selector. " +
                   "Ready to apply. Use this exact call:\n" +
                   $"  apply_to_scope(scope=\"{scope}\", selector=\"{confirmedSelector}\", ...)\n\n" +
                   "Replace ... with operation, attribute, strategy as needed.";
        }

        return header +
               "Ask the user which element they mean, or ask them to paste the HTML element " +
               "from the browser inspector so you can identify the exact selector.";
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

    internal static string? FindDuplicateInjectedSectionHeading(string content)
    {
        var headingCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (Match match in _injectedSectionHeadingRegex.Matches(content))
        {
            var heading = match.Groups["heading"].Value;
            if (!headingCounts.TryAdd(heading, 1))
            {
                headingCounts[heading]++;
            }
        }

        foreach (var headingCount in headingCounts)
        {
            if (headingCount.Value > 1)
            {
                return headingCount.Key;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns up to 5 regions from <paramref name="fileContent"/> that contain <paramref name="query"/>,
    /// each with ±5 lines of context. Result is a compact excerpt for Claude to pick a verbatim anchor
    /// without loading the full file.
    /// </summary>
    internal static string BuildSearchResult(string fileContent, string query, string filePath, int version)
    {
        const int contextLines = 5;
        const int maxRegions = 5;
        const int maxResultChars = 4000;

        if (string.IsNullOrWhiteSpace(query))
            return "Error: query must not be empty.";

        var lines = fileContent.Split('\n');

        // Primary match: the query appears as an exact contiguous substring on a single line.
        var exactIndices = new List<int>();
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (lines[lineIndex].Contains(query, StringComparison.OrdinalIgnoreCase))
                exactIndices.Add(lineIndex);
        }

        var fuzzy = false;
        List<int> matchedIndices;

        if (exactIndices.Count > 0)
        {
            matchedIndices = exactIndices;
        }
        else
        {
            // Fallback: rank lines by how many distinct query words they contain. This rescues
            // natural-language queries (e.g. "thumbs up feedback") that never appear verbatim
            // on a line but whose words do — making the search far less brittle on prototypes.
            var queryWords = ExtractSearchTokens(query);
            if (queryWords.Length == 0)
                return $"SEARCH_NOT_FOUND: No lines in '{filePath}' contain '{query}'. Try a different keyword.";

            var scored = new List<(int LineIndex, int Score)>();
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                var score = queryWords.Count(word => line.Contains(word, StringComparison.OrdinalIgnoreCase));
                if (score > 0)
                    scored.Add((lineIndex, score));
            }

            if (scored.Count == 0)
                return $"SEARCH_NOT_FOUND: No lines in '{filePath}' contain '{query}' or any of its words " +
                       $"({string.Join(", ", queryWords)}). Try a different keyword.";

            // Keep the strongest matches (highest word overlap, earliest on ties), then present
            // them in file order so the overlapping-region merge below behaves correctly.
            matchedIndices = scored
                .OrderByDescending(entry => entry.Score)
                .ThenBy(entry => entry.LineIndex)
                .Take(maxRegions)
                .Select(entry => entry.LineIndex)
                .OrderBy(lineIndex => lineIndex)
                .ToList();
            fuzzy = true;
        }

        var matchDescriptor = fuzzy
            ? $"{matchedIndices.Count} fuzzy match(es) — no exact phrase found, showing closest lines by word overlap"
            : $"{matchedIndices.Count} match(es)";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
            $"## search_in_artefact: '{query}' in {filePath} v{version} ({matchDescriptor})\n");
        sb.AppendLine("Copy a unique verbatim substring from below as old_str for edit_artefact.\n");

        var regionsShown = 0;
        var lastEnd = -1;

        foreach (var centreIndex in matchedIndices)
        {
            if (regionsShown >= maxRegions)
                break;

            var start = Math.Max(0, centreIndex - contextLines);
            var end = Math.Min(lines.Length - 1, centreIndex + contextLines);

            if (start <= lastEnd)
            {
                lastEnd = Math.Max(lastEnd, end);
                continue;
            }

            lastEnd = end;

            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                $"--- lines {start + 1}–{end + 1} ---");

            for (var lineIndex = start; lineIndex <= end; lineIndex++)
                sb.AppendLine(lines[lineIndex]);

            regionsShown++;

            if (sb.Length >= maxResultChars)
                break;
        }

        var result = sb.ToString();
        if (result.Length > maxResultChars)
            result = string.Concat(result.AsSpan(0, maxResultChars), "\n...(truncated — use a more specific query)");

        return result;
    }

    /// <summary>
    /// Splits a search query into distinct, meaningful tokens (≥3 characters, punctuation and
    /// markup stripped) used for the fuzzy fallback in <see cref="BuildSearchResult"/>. Returns
    /// at most 8 tokens, preserving first-seen order.
    /// </summary>
    internal static string[] ExtractSearchTokens(string query)
    {
        return query
            .Split(
                [' ', '\n', '\r', '\t', '<', '>', '"', '\'', '=', '{', '}', ';', ':', ',', '(', ')', '.', '/', '\\', '-', '_'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
    }

    /// <summary>
    /// Extracts up to 5 lines from <paramref name="fileContent"/> that contain words from
    /// <paramref name="attemptedAnchor"/>, with ±3 lines of surrounding context each.
    /// Returns a compact excerpt Claude can use to pick a real verbatim anchor.
    /// </summary>
    internal static string BuildAnchorContextHint(string fileContent, string attemptedAnchor)
    {
        const int contextLines = 3;
        const int maxSnippets = 3;
        const int maxHintChars = 3000;

        // Extract meaningful search words (≥4 chars, no HTML tags, no punctuation-only tokens)
        var searchWords = attemptedAnchor
            .Split([' ', '\n', '\r', '\t', '<', '>', '"', '\'', '=', '{', '}', ';', ':', ','], StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= 4 && !word.StartsWith("//", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();

        if (searchWords.Length == 0)
            return "(No search terms could be extracted — use get_artefact to read the full file)";

        var lines = fileContent.Split('\n');
        var matchedLineIndices = new HashSet<int>();

        foreach (var word in searchWords)
        {
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (lines[lineIndex].Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    matchedLineIndices.Add(lineIndex);
                    if (matchedLineIndices.Count >= maxSnippets * 5)
                        break;
                }
            }
        }

        if (matchedLineIndices.Count == 0)
            return $"(No lines found containing words from your anchor: {string.Join(", ", searchWords)})";

        var sb = new System.Text.StringBuilder();
        var snippetCount = 0;

        foreach (var centreIndex in matchedLineIndices.OrderBy(index => index))
        {
            if (snippetCount >= maxSnippets)
                break;

            var start = Math.Max(0, centreIndex - contextLines);
            var end = Math.Min(lines.Length - 1, centreIndex + contextLines);

            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"--- lines {start + 1}–{end + 1} ---");
            for (var lineIndex = start; lineIndex <= end; lineIndex++)
                sb.AppendLine(lines[lineIndex]);

            snippetCount++;

            if (sb.Length >= maxHintChars)
                break;
        }

        var hint = sb.ToString();
        if (hint.Length > maxHintChars)
            hint = hint[..maxHintChars] + "\n...(truncated)";

        return hint;
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

    private static string BuildContinuationCheckpointFilePath(Guid conversationId)
    {
        return $"checkpoints/conversation-{conversationId}.md";
    }

    private async Task SaveContinuationCheckpointAsync(
        Conversation conversation,
        Guid projectId,
        string latestUserMessage,
        string latestAssistantOutput,
        IReadOnlyList<Artefact> savedArtefacts,
        IReadOnlyList<ParkingLotItem> savedParkingLotItems,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var filePath = BuildContinuationCheckpointFilePath(conversation.Id);
        var content = BuildContinuationCheckpointContent(
            conversation,
            latestUserMessage,
            latestAssistantOutput,
            savedArtefacts,
            savedParkingLotItems);
        var contentType = "text/markdown";

        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
            projectId,
            filePath,
            cancellationToken);

        var storageKey = await _artefactStorageService.SaveContentAsync(
            projectId,
            filePath,
            nextVersion,
            content,
            contentType,
            cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            projectId,
            nextVersion,
            filePath,
            storageKey,
            contentType,
            Encoding.UTF8.GetByteCount(content),
            createdBy,
            _timeProvider,
            true);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        await _artefactRepository.DeletePreviousVersionsAsync(projectId, filePath, nextVersion, cancellationToken);
    }

    private static string BuildContinuationCheckpointContent(
        Conversation conversation,
        string latestUserMessage,
        string latestAssistantOutput,
        IReadOnlyList<Artefact> savedArtefacts,
        IReadOnlyList<ParkingLotItem> savedParkingLotItems)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Conversation Checkpoint");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Conversation ID: {conversation.Id}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Phase: {conversation.CurrentPhase} ({conversation.PhaseName})");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Questions asked: {conversation.QuestionsAsked}");
        if (!string.IsNullOrWhiteSpace(conversation.RequirementId))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Requirement window: {conversation.RequirementId}");
        }

        sb.AppendLine();
        sb.AppendLine("## What Was Just Completed");
        if (savedArtefacts.Count == 0)
        {
            sb.AppendLine("- No artefacts were saved in this interrupted turn.");
        }
        else
        {
            foreach (var artefact in savedArtefacts)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"- Saved {artefact.FilePath} v{artefact.Version}");
            }
        }

        if (savedParkingLotItems.Count > 0)
        {
            sb.AppendLine("- Parking lot updates in this interrupted turn:");
            foreach (var item in savedParkingLotItems)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  - [{item.Status}] {item.Content}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Last User Instruction");
        sb.AppendLine("```");
        sb.AppendLine(TrimForCheckpoint(latestUserMessage, 2000));
        sb.AppendLine("```");

        if (!string.IsNullOrWhiteSpace(latestAssistantOutput))
        {
            sb.AppendLine();
            sb.AppendLine("## Last Assistant Output (partial if interrupted)");
            sb.AppendLine("```");
            sb.AppendLine(TrimForCheckpoint(latestAssistantOutput, 3000));
            sb.AppendLine("```");
        }

        sb.AppendLine();
        sb.AppendLine("## Where To Continue");
        sb.AppendLine("- Continue from the current phase and requirement window.");
        sb.AppendLine("- Do not repeat completed writes listed above.");
        sb.AppendLine("- Start with the next unresolved requirement or unanswered question.");

        return sb.ToString();
    }

    private static string TrimForCheckpoint(string value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(empty)";
        }

        return value.Length <= maxChars
            ? value
            : value[..maxChars] + "...";
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

}
