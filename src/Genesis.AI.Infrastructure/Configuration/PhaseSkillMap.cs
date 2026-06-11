using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Infrastructure.Configuration;

/// <summary>
/// Static configuration mapping pipeline stages and phases to the skill files
/// that should be injected into the Bedrock system prompt for that turn.
///
/// Three tiers are combined at call time by <see cref="GetSkillsForPhase"/>:
///   1. <see cref="UniversalSkills"/>  — injected on every phase of every supported stage.
///   2. Stage skills                   — additional skills applied on every phase of a specific stage.
///   3. Phase overrides                — additional skills applied only at a specific phase number.
///
/// Supported stages: Architecture (P03) through Planning (P10).
/// RequirementsDiscovery (P01) and Prototype (P02) are explicitly excluded —
/// those stages do not use the active skill injection path.
/// </summary>
public static class PhaseSkillMap
{
    /// <summary>
    /// Skills injected on every phase of every supported stage (P03–P10).
    /// </summary>
    public static readonly IReadOnlyList<string> UniversalSkills =
    [
        "interview-discipline",
        "parking-lot",
        "phase-transition-protocol",
        "bounded-clarification-budget",
        "carry-forward-contract",
        "tool-failure-policy",
    ];

    /// <summary>
    /// Additional skills applied on every phase of specific stages.
    /// P03 Architecture: EMIS microservice design and observability guardrails.
    /// P04 Design: EMIS API standards, auth, C# patterns, DDD, and data access guardrails.
    /// P05 PxD: EMIS webapp design system and accessibility guardrails.
    /// P06, P07, P08 require human-in-the-loop and pre-fill confidence protocols.
    /// P08 Security additionally injects the EMIS security guardrail skill.
    /// </summary>
    private static readonly Dictionary<StageType, string[]> StageSkills = new()
    {
        [StageType.Architecture] =
        [
            "emis-x-api-microservice-design",
            "emis-x-api-observability",
            "emis-x-api-postgres",
        ],
        [StageType.Design] =
        [
            "emis-x-api-standards",
            "emis-x-api-auth",
            "emis-x-api-csharp-standards",
            "emis-x-api-domain-driven-design",
            "emis-x-api-data-access",
        ],
        [StageType.Pxd] =
        [
            "emis-x-webapp-design-system",
            "emis-x-webapp-coding-standards",
            "emis-x-webapp-accessibility",
            "emis-x-webapp-clinical-safety",
        ],
        [StageType.ClinicalSafety] =
        [
            "human-in-the-loop-protocol",
            "pre-fill-confidence-markers",
            "emis-x-api-clinical-safety",
        ],
        [StageType.InformationGovernance] =
        [
            "human-in-the-loop-protocol",
            "pre-fill-confidence-markers",
        ],
        [StageType.Security] =
        [
            "human-in-the-loop-protocol",
            "pre-fill-confidence-markers",
            "emis-x-api-security",
        ],
    };

    /// <summary>
    /// Additional skills applied only at specific phase numbers within a stage.
    /// Phase 0 is always the orientation/routing phase.
    /// </summary>
    private static readonly Dictionary<StageType, Dictionary<int, string[]>> PhaseOverrides = new()
    {
        [StageType.Architecture] = new()
        {
            [0] = ["run-mode-routing-p03", "context-loading-p03", "review-list-p03"],
            [1] = ["technology-stack-p03", "adr-register-protocol", "mandatory-adr-index-strategy", "mandatory-adr-idempotency"],
            [2] = ["bdat-analysis-method", "ig003-gate-p03", "service-classification-rules", "immediate-write-protocol"],
            [3] = ["platform-boundaries-method"],
            [4] = ["failure-modes-method"],
            [5] = ["emis-landscape-integration"],
            [6] = ["aws-well-architected"],
            [7] = ["emis-principles-validation"],
            [8] = ["operations-monitoring"],
            [9] = ["performance-cost"],
            [10] = ["security-framing-p03"],
            [11] = ["mermaid-diagrams"],
            [12] = ["gap-fill-verification", "emis-x-stack-reference"],
            [13] = ["iteration-report", "feedback-collection-p03"],
        },
        [StageType.Design] = new()
        {
            [0] = ["context-loading-p04", "service-scope-verification"],
            [1] = ["api-contract-design", "cross-requirement-chain"],
            [2] = ["database-schema-design"],
            [3] = ["component-interface-design"],
            [4] = ["state-machine-design"],
            [5] = ["data-validation-rules"],
            [6] = ["error-handling-strategy"],
            [7] = ["integration-contract-design"],
            [8] = ["data-migration-strategy"],
            [9] = ["testing-strategy"],
            [10] = ["performance-optimisation"],
            [11] = ["api-documentation"],
            [12] = ["output-write-protocol", "no-placeholder-enforcement"],
            [13] = ["iteration-report", "feedback-collection-p04"],
        },
        [StageType.Pxd] = new()
        {
            [0] = ["context-loading-p05", "emis-ui-kit-baseline"],
            [1] = ["user-flow-mapping"],
            [2] = ["wireframe-design"],
            [3] = ["component-specifications"],
            [4] = ["interaction-patterns"],
            [5] = ["accessibility-requirements"],
            [6] = ["responsive-design"],
            [7] = ["visual-design"],
            [8] = ["micro-interactions"],
            [9] = ["error-states"],
            [10] = ["empty-states"],
            [11] = ["design-system-integration"],
            [12] = ["output-write-protocol", "no-placeholder-enforcement"],
            [13] = ["iteration-report", "feedback-collection-p05"],
        },
    };

    /// <summary>
    /// Returns the complete ordered list of skill names to inject for the given
    /// stage type and phase number. Skills are deduplicated; order is universal →
    /// stage → phase.
    /// </summary>
    /// <param name="stageType">The pipeline stage being executed.</param>
    /// <param name="phase">The current phase number within the stage (0-indexed).</param>
    /// <returns>
    /// Ordered skill names corresponding to embedded <c>.md</c> files under
    /// <c>Genesis.AI.Infrastructure.Skills/</c>. Returns an empty list for
    /// unsupported stages (RequirementsDiscovery, Prototype).
    /// </returns>
    public static IReadOnlyList<string> GetSkillsForPhase(StageType stageType, int phase)
    {
        if (stageType is StageType.RequirementsDiscovery or StageType.Prototype)
        {
            return [];
        }

        var skills = new List<string>(UniversalSkills);

        if (StageSkills.TryGetValue(stageType, out var stageLevel))
        {
            skills.AddRange(stageLevel);
        }

        if (PhaseOverrides.TryGetValue(stageType, out var phaseMap) &&
            phaseMap.TryGetValue(phase, out var phaseLevel))
        {
            skills.AddRange(phaseLevel);
        }

        return skills.AsReadOnly();
    }

    /// <summary>
    /// Returns every distinct skill name referenced anywhere in the map.
    /// Used by tests to verify all referenced skills resolve to embedded resources.
    /// </summary>
    public static IReadOnlySet<string> AllReferencedSkills()
    {
        var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in UniversalSkills)
        {
            all.Add(skill);
        }

        foreach (var stageSkillList in StageSkills.Values)
        {
            foreach (var skill in stageSkillList)
            {
                all.Add(skill);
            }
        }

        foreach (var phaseMap in PhaseOverrides.Values)
        {
            foreach (var phaseSkillList in phaseMap.Values)
            {
                foreach (var skill in phaseSkillList)
                {
                    all.Add(skill);
                }
            }
        }

        return all;
    }
}
