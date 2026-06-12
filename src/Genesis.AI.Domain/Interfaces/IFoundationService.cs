using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Builds the Category A (stable foundation) content for a given stage.
/// Foundation content consists of read-only upstream artefacts injected into the
/// system prompt before the Bedrock cache point so they are cached across turns.
/// </summary>
public interface IFoundationService
{
    /// <summary>
    /// Builds the foundation content block for the specified project and stage.
    /// Returns an empty string when no foundation artefacts are available or the stage
    /// does not have a foundation map entry (P1, P2, P9, P10).
    /// Safe logging only — logs artefact counts and character lengths, never content.
    /// </summary>
    Task<string> BuildFoundationContentAsync(
        Guid projectId,
        StageType stageType,
        CancellationToken cancellationToken);
}
