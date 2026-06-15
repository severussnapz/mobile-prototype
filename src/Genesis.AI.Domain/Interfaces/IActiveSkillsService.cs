using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Resolves the ordered list of skill documents to inject into the Bedrock system
/// prompt for the current pipeline stage and phase.
/// </summary>
public interface IActiveSkillsService
{
    /// <summary>
    /// Returns the concatenated content of all skill documents applicable to the
    /// given stage and phase, ready for injection before the Bedrock cache breakpoint.
    /// Returns an empty string when no skills apply (e.g. P01/P02).
    /// </summary>
    /// <param name="stageType">The pipeline stage being executed.</param>
    /// <param name="currentPhase">The current phase number within the stage (0-indexed).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> BuildActiveSkillsAsync(StageType stageType, int currentPhase, CancellationToken cancellationToken);
}
