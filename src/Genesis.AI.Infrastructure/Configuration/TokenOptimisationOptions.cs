namespace Genesis.AI.Infrastructure.Configuration;

/// <summary>
/// Feature flags controlling the token optimisation rollout for P3-P8.
/// All flags default to <c>false</c> in production until validated.
///
/// Rollout order:
/// 1. <see cref="FoundationPrefixEnabled"/> only (Change 1 + prompt contract from Change 2).
/// 2. <see cref="RequirementWindowingEnabled"/> for internal pilot users.
/// 3. <see cref="NonWindowedCrossCheckEnabled"/> for P6/P7/P8.
///
/// Rollback rule: if a correctness regression appears (duplicate IDs, missing artefacts,
/// unrecoverable phase stalls) disable the relevant flag. No schema rollback is required.
/// </summary>
public sealed class TokenOptimisationOptions
{
    public const string SectionName = "TokenOptimisation";

    /// <summary>
    /// When enabled, stable upstream artefacts (Category A) are injected into the system
    /// prompt before the Bedrock cache point, reducing per-turn token costs by ~90% for
    /// the cached portion.
    /// </summary>
    public bool FoundationPrefixEnabled { get; set; }

    /// <summary>
    /// When enabled, conversations are scoped per-requirement (one conversation per
    /// requirement_id). The frontend groups these as one continuous stage stepper.
    /// </summary>
    public bool RequirementWindowingEnabled { get; set; }

    /// <summary>
    /// When enabled, P6/P7/P8 include an explicit non-windowed cross-check phase at the
    /// end of the forward sweep. Mode switch must be triggered via explicit orchestration
    /// signal — never inferred from requirement counts or turn counts.
    /// </summary>
    public bool NonWindowedCrossCheckEnabled { get; set; }

    /// <summary>
    /// Optional per-stage overrides for the tool-turn hard limit.
    /// Keys are <see cref="StageType"/> string values (e.g. <c>"ClinicalSafety"</c>).
    /// When a key is present, its value replaces the default 40-turn limit for that stage.
    /// Useful for stages with high output volume (e.g. P08 Security writing many files).
    /// </summary>
    public Dictionary<string, int> StageToolTurnLimits { get; set; } = new();

    /// <summary>
    /// When enabled, the edit_artefact tool is registered and advertised to the AI.
    /// Allows surgical anchor-based edits to existing artefacts instead of full regeneration.
    /// Rollout: P02 pilot first, then extend to P03-P08 after telemetry is green.
    /// </summary>
    public bool EditArtefactEnabled { get; set; }

    /// <summary>
    /// When enabled, P02 uses fragment-based generation under prototype/fragments/.
    /// The platform assembles prototype/index.html deterministically after each fragment save.
    /// </summary>
    public bool PrototypeFragmentsEnabled { get; set; }

    /// <summary>
    /// When enabled, phase-aware skill content is appended to the stable (cached) part of
    /// the Bedrock system prompt, immediately before the cache breakpoint.
    /// Only active when <see cref="FoundationPrefixEnabled"/> is also true — requires the
    /// split-prompt path to insert skills before the cache point.
    /// Defaults to <c>false</c> until skill content has been validated against all stages.
    /// </summary>
    public bool ActiveSkillInjectionEnabled { get; set; }

    /// <summary>
    /// Phase 0 flag for Plan 4 DOM migration.
    /// When enabled, prototype search and mutation paths can use the new AngleSharp DOM
    /// services instead of the graph pipeline. Default is <c>false</c> until cutover.
    /// </summary>
    public bool PrototypeDomModeEnabled { get; set; }
    /// <summary>
    /// When enabled, the propose_requirement_change tool is registered and available
    /// in all pipeline stage conversations. Allows any pipeline to propose AC additions,
    /// clarifications, or contradiction flags back to requirement files.
    /// </summary>
    public bool RequirementFeedbackEnabled { get; set; }
}
