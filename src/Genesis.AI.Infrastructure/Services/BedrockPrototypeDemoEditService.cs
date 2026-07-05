using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Bedrock-backed implementation of <see cref="IPrototypeDemoEditService"/>.
/// Sends the selected element's <c>outerHTML</c> and the user's instruction to
/// <see cref="IAiService.StreamResponseAsync"/>, then runs deterministic post-
/// generation checks (failure modes 1–6) before returning a four-valued result.
///
/// Prompt cache split (mirrors Decision A from BedrockPrototypeDemoGenerationService):
///   Stable  = base edit prompt + emis-x-ui-kit.md (shared across every emis-x edit — cached ~10× cheaper)
///   Mutable = selected element outerHTML + instruction + active UI kit (per-request, always fresh)
///
/// After a successful edit the updated full document is persisted best-effort to
/// S3 (versioned) and the prototype/index.html artefact row is updated, so version
/// history and cross-stage reads see surgical edits. A persistence failure is logged
/// but never fails the edit — the client already has the returned document.
/// </summary>
public sealed class BedrockPrototypeDemoEditService : IPrototypeDemoEditService
{
    private const string PromptResourceName =
        "Genesis.AI.Infrastructure.Prompts.PrototypeElementEdit.md";

    private const string UiKitResourceName =
        "Genesis.AI.Infrastructure.Resources.emis-x-ui-kit.md";

    // Surgical edits only ever apply to the single-file prototype artefact.
    private const string PrototypeHtmlFilePath = "prototype/index.html";
    private const string PrototypeHtmlContentType = "text/html";

    private readonly IAiService _aiService;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BedrockPrototypeDemoEditService> _logger;
    private readonly IConversationRepository _conversationRepository;

    public BedrockPrototypeDemoEditService(
        IAiService aiService,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        TimeProvider timeProvider,
        ILogger<BedrockPrototypeDemoEditService> logger,
        IConversationRepository conversationRepository)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    public async Task<PrototypeElementEditResult> EditElementAsync(
        Guid projectId,
        PrototypeElementEditRequest request,
        CancellationToken cancellationToken)
    {
        var prompt = LoadEmbeddedText(PromptResourceName);
        var uiKit = LoadEmbeddedText(UiKitResourceName);

        // Cache split A: stable = edit prompt + emis-x-ui-kit.md (shared/cached);
        // mutable = selected element + instruction (per-request, always fresh).
        var systemPrompt = new AiSystemPrompt(
            StablePart: BuildStablePart(prompt, uiKit),
            MutablePart: BuildMutablePart(request));

        var userMessage = new AiMessage(
            MessageRole.User,
            $"Edit the selected element as instructed. Return only the updated outerHTML.");

        var aiResponse = await _aiService.GenerateResponseAsync(systemPrompt, [userMessage], cancellationToken);
        var result = PrototypeElementValidator.Validate(aiResponse.Content.Trim(), request.SelectedOuterHtml);
        if (result.Status != PrototypeElementEditStatus.Applied)
        {
            return result;
        }

        // Applied: perform the element replacement server-side against the full document so
        // the client renders the returned document directly. Locate the element by a
        // serialisation-independent fingerprint (self-closing SVG tags etc. defeat a raw match).
        var updatedFullHtml = await PrototypeElementReplacer.ReplaceElementAsync(
            request.CurrentHtml, request.SelectedOuterHtml, result.UpdatedOuterHtml, cancellationToken);
        if (updatedFullHtml is null)
        {
            return PrototypeElementEditResult.Rejected(
                "Selected element could not be located in the current prototype document.");
        }

        // Persistence is best-effort: the edit already succeeded and the client renders
        // updatedFullHtml directly. A save failure must not fail the edit — log and carry on.
        await PersistUpdatedPrototypeAsync(projectId, updatedFullHtml, cancellationToken);

        if (request.ConversationId.HasValue)
        {
            await RecordSurgicalEditTokenUsageAsync(
                request.ConversationId.Value,
                aiResponse.InputTokens,
                aiResponse.OutputTokens,
                aiResponse.CacheReadInputTokens,
                aiResponse.CacheWriteInputTokens,
                cancellationToken);
        }

        return result with { UpdatedFullHtml = updatedFullHtml };
    }

    // Persist the updated prototype to S3 and update the DB row (or create it if missing).
    // createdBy is "system" — the edit service has no user context, consistent with S3
    // fallback versions.
    private async Task PersistUpdatedPrototypeAsync(
        Guid projectId,
        string updatedFullHtml,
        CancellationToken cancellationToken)
    {
        try
        {
            var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
                projectId, PrototypeHtmlFilePath, cancellationToken);

            var newStorageKey = await _artefactStorageService.SaveContentAsync(
                projectId, PrototypeHtmlFilePath, nextVersion, updatedFullHtml,
                PrototypeHtmlContentType, cancellationToken);

            var sizeBytes = Encoding.UTF8.GetByteCount(updatedFullHtml);

            var existingArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
                projectId, PrototypeHtmlFilePath, cancellationToken);

            if (existingArtefact is not null)
            {
                existingArtefact.ReplaceContent(
                    nextVersion, newStorageKey, PrototypeHtmlContentType,
                    sizeBytes, "system", _timeProvider);
                await _artefactRepository.UpdateAsync(existingArtefact, cancellationToken);
            }
            else
            {
                var newArtefact = Artefact.CreateS3Artefact(
                    projectId, nextVersion, PrototypeHtmlFilePath, newStorageKey,
                    PrototypeHtmlContentType, sizeBytes, "system", _timeProvider, true);
                await _artefactRepository.AddAsync(newArtefact, cancellationToken);
            }

            await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist surgical prototype edit for project {ProjectId}; the edit was returned to the client but not saved.",
                projectId);
        }
    }

    // --- System prompt builders ---

    private static string BuildStablePart(string prompt, string uiKit)
    {
        return $"""
            {prompt}

            ## EMIS-X Design System Reference

            {uiKit}
            """;
    }

    private static string BuildMutablePart(PrototypeElementEditRequest request)
    {
        return $"""
            ## Edit Request

            SELECTED ELEMENT:
            {request.SelectedOuterHtml}

            INSTRUCTION:
            {request.Instruction}

            ACTIVE UI KIT: {request.ActiveUiKit}
            """;
    }

    private static string LoadEmbeddedText(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private async Task RecordSurgicalEditTokenUsageAsync(
        Guid conversationId,
        int inputTokens,
        int outputTokens,
        int cacheReadInputTokens,
        int cacheWriteInputTokens,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
            if (conversation is null)
            {
                _logger.LogWarning(
                    "Surgical edit token usage: conversation {ConversationId} not found — skipping.",
                    conversationId);
                return;
            }
            conversation.RecordTokenUsage(
                inputTokens, outputTokens, cacheReadInputTokens, cacheWriteInputTokens, _timeProvider);
            await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Surgical edit token usage: failed to record for conversation {ConversationId} — edit already succeeded.",
                conversationId);
        }
    }
}
