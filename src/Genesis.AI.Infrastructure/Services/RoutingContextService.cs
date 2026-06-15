using Genesis.AI.Domain;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Resolves a <see cref="RoutingContext"/> by loading the conversation, stage
/// type, and project artefacts in parallel.
///
/// Artefact-derived signals (swagger_present, prototype_present, etc.) are
/// computed from the artefact manifest and injected into the routing context
/// so that phase-specific skills can select fast-track paths without
/// performing redundant artefact lookups.
///
/// Throws <see cref="InvalidOperationException"/> if the conversation or stage
/// type is missing — callers should treat this as a 404-equivalent.
/// </summary>
public sealed class RoutingContextService : IRoutingContextService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IArtefactRepository _artefactRepository;

    public RoutingContextService(
        IConversationRepository conversationRepository,
        IArtefactRepository artefactRepository)
    {
        _conversationRepository = conversationRepository;
        _artefactRepository = artefactRepository;
    }
    public async Task<RoutingContext> BuildRoutingContextAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversationTask = _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        var stageTypeTask = _conversationRepository.GetStageTypeByConversationIdAsync(conversationId, cancellationToken);

        await Task.WhenAll(conversationTask, stageTypeTask);

        var conversation = await conversationTask
            ?? throw new InvalidOperationException($"Conversation '{conversationId}' not found.");

        var stageType = await stageTypeTask
            ?? throw new InvalidOperationException($"Stage type not found for conversation '{conversationId}'.");

        var projectContext = await _conversationRepository.GetProjectContextByStageIdAsync(conversation.StageId, cancellationToken);
        var projectId = projectContext?.ProjectId ?? Guid.Empty;

        var artefacts = await LoadProjectArtefactsAsync(projectId, cancellationToken);

        var isFirstMessage = conversation.QuestionsAsked <= 1;

        return RoutingContext.Create(
            stageType: stageType,
            currentPhase: conversation.CurrentPhase,
            isFirstMessage: isFirstMessage,
            swaggerPresent: HasArtefactWithPrefix(artefacts, "swagger/"),
            prototypePresent: HasArtefactWithPath(artefacts, "prototype/index.html"),
            hazardRegistryExisting: HasArtefactWithPrefix(artefacts, "clinical-safety/"),
            hazIdWatermark: GetHazIdWatermark(artefacts),
            securityFramingPresent: HasArtefactContentMarker(artefacts, "security-framing"),
            dpiaReferenceExisting: HasArtefactWithPrefix(artefacts, "ig/dpia"),
            nhsRetentionApplicable: HasNhsSpecialCategoryData(artefacts),
            lawfulBasisConfirmed: HasArtefactWithPrefix(artefacts, "ig/lawful-basis"));
    }

    private async Task<IReadOnlyList<Artefact>> LoadProjectArtefactsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty)
        {
            return [];
        }

        return await _artefactRepository.GetByProjectIdAsync(projectId, cancellationToken);
    }

    private static bool HasArtefactWithPrefix(IReadOnlyList<Artefact> artefacts, string prefix)
    {
        return artefacts.Any(artefact => artefact.FilePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasArtefactWithPath(IReadOnlyList<Artefact> artefacts, string path)
    {
        return artefacts.Any(artefact => string.Equals(artefact.FilePath, path, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasArtefactContentMarker(IReadOnlyList<Artefact> artefacts, string marker)
    {
        return artefacts.Any(artefact => artefact.FilePath.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static int GetHazIdWatermark(IReadOnlyList<Artefact> artefacts)
    {
        var watermarkArtefact = artefacts
            .FirstOrDefault(artefact => artefact.FilePath.Equals(
                "feedback/HAZ_ID_WATERMARK.md", StringComparison.OrdinalIgnoreCase));

        if (watermarkArtefact is null)
        {
            return 0;
        }

        // Return the artefact version as a proxy for the watermark value;
        // the actual watermark value is parsed by the AI from file content.
        return watermarkArtefact.Version;
    }

    private static bool HasNhsSpecialCategoryData(IReadOnlyList<Artefact> artefacts)
    {
        return artefacts.Any(artefact =>
            artefact.FilePath.Contains("clinical", StringComparison.OrdinalIgnoreCase) ||
            artefact.FilePath.Contains("clinical-safety", StringComparison.OrdinalIgnoreCase));
    }
}
