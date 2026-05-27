using System.Collections.Frozen;
using System.Reflection;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class EmbeddedPromptService : IPromptService
{
    private static readonly FrozenDictionary<StageType, string> PromptCache = LoadAllPrompts();

    private static readonly FrozenDictionary<StageType, string[]> PhaseDefinitions = new Dictionary<StageType, string[]>
    {
        [StageType.RequirementsDiscovery] = [
            "mode_selection",           // 0
            "strategic_context",        // 1
            "product_context",          // 2
            "personas_users",           // 3
            "core_workflow",            // 4
            "requirements_elicitation", // 5
            "non_functional",           // 6
            "integration_points",       // 7
            "assumptions_risks",        // 8
            "constraints",              // 9
            "success_metrics",          // 10
            "finalise_and_polish",      // 11
            "feedback"                  // 12
        ],
        [StageType.Architecture] = [
            "context_loading",          // 0
            "cross_requirement_questions", // 1
            "bdat_analysis",            // 2
            "adr_creation",             // 3
            "failure_modes",            // 4
            "integration_points",       // 5
            "service_classification",   // 6
            "evaluation_specs",         // 7
            "verify_and_complete",      // 8
            "feedback"                  // 9
        ],
        [StageType.Design] = [
            "context_loading",          // 0
            "cross_requirement_questions", // 1
            "api_contract",             // 2
            "database_schema",          // 3
            "component_interfaces",     // 4
            "state_machine",            // 5
            "data_validation",          // 6
            "error_handling",           // 7
            "integration_contracts",    // 8
            "data_migration",           // 9
            "testing_strategy",         // 10
            "performance",              // 11
            "write_to_file",            // 12
            "feedback"                  // 13
        ],
        [StageType.Pxd] = [
            "context_loading",          // 0
            "cross_requirement_questions", // 1
            "user_flow_mapping",        // 2
            "wireframe_design",         // 3
            "component_specifications", // 4
            "interaction_patterns",     // 5
            "accessibility",            // 6
            "responsive_design",        // 7
            "visual_design",            // 8
            "micro_interactions",       // 9
            "error_states",             // 10
            "empty_states",             // 11
            "write_to_file",            // 12
            "feedback"                  // 13
        ],
        [StageType.ClinicalSafety] = [
            "context_loading",          // 0
            "hazard_identification",    // 1
            "severity_assessment",      // 2
            "likelihood_assessment",    // 3
            "risk_evaluation",          // 4
            "mitigation_design",        // 5
            "residual_risk",            // 6
            "guardrail_mapping",        // 7
            "cso_review",              // 8
            "write_to_file",            // 9
            "feedback"                  // 10
        ],
        [StageType.Normalisation] = [
            "context_loading",          // 0
            "incremental_check",        // 1
            "extraction",               // 2
            "cross_cutting",            // 3
            "validation",               // 4
            "output_generation",        // 5
            "feedback"                  // 6
        ],
        [StageType.Planning] = [
            "context_loading",          // 0
            "dependency_analysis",      // 1
            "task_generation",          // 2
            "layer_ordering",           // 3
            "gate_insertion",           // 4
            "self_review",              // 5
            "output_generation",        // 6
            "feedback"                  // 7
        ],
        [StageType.Prototype] = [
            "context_loading",          // 0
            "flow_prioritisation",      // 1
            "visual_direction",         // 2
            "build_prototype",          // 3
            "iterate_and_refine",       // 4
            "validation_notes"          // 5
        ]
    }.ToFrozenDictionary();

    /// <summary>
    /// Returns the stage ordering for pipeline progression.
    /// </summary>
    public static int GetStageOrder(StageType stageType)
    {
        return stageType switch
        {
            StageType.RequirementsDiscovery => 1,
            StageType.Prototype => 2,
            StageType.Architecture => 3,
            StageType.Design => 4,
            StageType.Pxd => 5,
            StageType.ClinicalSafety => 6,
            StageType.Normalisation => 7,
            StageType.Planning => 8,
            _ => 99
        };
    }

    public string GetSystemPrompt(StageType stageType)
    {
        return PromptCache.TryGetValue(stageType, out var prompt)
            ? prompt
            : throw new InvalidOperationException($"No prompt found for stage type: {stageType}");
    }

    public int GetTotalPhases(StageType stageType)
    {
        return PhaseDefinitions.TryGetValue(stageType, out var phases)
            ? phases.Length - 1  // 0-indexed, so total = length - 1
            : 12;
    }

    public string[] GetPhaseNames(StageType stageType)
    {
        return PhaseDefinitions.TryGetValue(stageType, out var phases)
            ? phases
            : ["unknown"];
    }

    private static FrozenDictionary<StageType, string> LoadAllPrompts()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var mapping = new Dictionary<StageType, string>
        {
            [StageType.RequirementsDiscovery] = "Genesis.AI.Infrastructure.Prompts.Pipeline01RequirementsDiscovery.md",
            [StageType.Prototype] = "Genesis.AI.Infrastructure.Prompts.Pipeline02Prototype.md",
            [StageType.Architecture] = "Genesis.AI.Infrastructure.Prompts.Pipeline03Architecture.md",
            [StageType.Design] = "Genesis.AI.Infrastructure.Prompts.Pipeline04Design.md",
            [StageType.Pxd] = "Genesis.AI.Infrastructure.Prompts.Pipeline05Pxd.md",
            [StageType.ClinicalSafety] = "Genesis.AI.Infrastructure.Prompts.Pipeline06ClinicalSafety.md",
            [StageType.Normalisation] = "Genesis.AI.Infrastructure.Prompts.Pipeline07Normalisation.md",
            [StageType.Planning] = "Genesis.AI.Infrastructure.Prompts.Pipeline08Planning.md",
        };

        var result = new Dictionary<StageType, string>();

        foreach (var (stageType, resourceName) in mapping)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
            using var reader = new StreamReader(stream);
            result[stageType] = reader.ReadToEnd();
        }

        return result.ToFrozenDictionary();
    }
}
