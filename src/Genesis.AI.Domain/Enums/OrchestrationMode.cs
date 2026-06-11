using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

/// <summary>
/// Controls how the AI orchestrator processes requirements during a stage run.
/// The mode must be switched explicitly via the <c>set_orchestration_mode</c> tool —
/// it is never inferred from turn counts, requirement counts, or queue state.
/// </summary>
public enum OrchestrationMode
{
    /// <summary>
    /// Default mode. The AI processes one requirement per windowed conversation.
    /// Each conversation has an independent, bounded message window.
    /// </summary>
    [PgName("forward_sweep")]
    ForwardSweep,

    /// <summary>
    /// Non-windowed cross-check mode for P6/P7/P8 only.
    /// Entered explicitly after the forward sweep completes, to perform a holistic
    /// cross-requirement consistency check (e.g. HAZ-ID monotonicity for P6).
    /// Only one cross-check conversation exists per stage run.
    /// </summary>
    [PgName("cross_check")]
    CrossCheck,
}
