using System.Collections.Frozen;
using System.Reflection;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class EmbeddedPromptService : IPromptService
{
    private static readonly FrozenDictionary<StageType, string> PromptCache = LoadAllPrompts();

    private static readonly string PrototypeSingleFilePromptContent = LoadPrototypeSingleFilePrompt();

    private static readonly FrozenDictionary<StageType, string[]> PhaseDefinitions = new Dictionary<StageType, string[]>
    {
        [StageType.RequirementsDiscovery] = [
            "business_context",         // 0
            "classifier",               // 1
            "users_and_personas",       // 2
            "core_workflow",            // 3
            "non_functional",           // 4
            "compliance_anchoring",     // 5
            "finalisation"              // 6
        ],
        [StageType.Architecture] = [
            "context_loading",          // 0
            "technology_stack",         // 1
            "bdat_analysis",            // 2
            "platform_boundaries",      // 3
            "failure_modes",            // 4
            "integration_points",       // 5
            "aws_well_architected",     // 6
            "emis_principles",          // 7
            "operations_monitoring",    // 8
            "performance_cost",         // 9
            "security_architecture",    // 10
            "mermaid_diagrams",         // 11
            "verify_and_gap_fill",      // 12
            "feedback"                  // 13
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
            "hazard_severity",          // 2
            "hazard_likelihood",        // 3
            "risk_matrix",              // 4
            "control_elicitation",      // 5
            "residual_risk",            // 6
            "hazard_cards",             // 7
            "guardrail_mapping",        // 8
            "dcb0129_check",            // 9
            "cso_signoff",              // 10
            "write_and_gate",           // 11
            "cso_final_review"          // 12
        ],
        [StageType.InformationGovernance] = [
            "context_loading",          // 0
            "lawful_basis",             // 1
            "data_minimisation",        // 2
            "retention_and_deletion",   // 3
            "access_controls",          // 4
            "audit_and_governance",     // 5
            "write_to_file",            // 6
            "feedback"                  // 7
        ],
        [StageType.Security] = [
            "context_loading",          // 0
            "threat_modelling",         // 1
            "auth_and_access",          // 2
            "data_protection",          // 3
            "monitoring_and_alerting",  // 4
            "hardening_actions",        // 5
            "write_to_file",            // 6
            "feedback"                  // 7
        ],
        [StageType.Normalisation] = [
            "intake_and_plan",          // 0
            "per_requirement_gap_fill", // 1
            "gate_verification",        // 2
            "handoff"                   // 3
        ],
        [StageType.Planning] = [
            "intake",                   // 0
            "artefact_intake",          // 1
            "task_plan_generation",     // 2
            "em_review_gate",           // 3
            "confirmed_ready"           // 4
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
            StageType.InformationGovernance => 7,
            StageType.Security => 8,
            StageType.Normalisation => 9,
            StageType.Planning => 10,
            _ => 99
        };
    }

    public string GetSystemPrompt(StageType stageType)
    {
        return PromptCache.TryGetValue(stageType, out var prompt)
            ? prompt
            : throw new InvalidOperationException($"No prompt found for stage type: {stageType}");
    }

    public string GetPrototypeSingleFilePrompt()
    {
        return PrototypeSingleFilePromptContent;
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
            [StageType.InformationGovernance] = "Genesis.AI.Infrastructure.Prompts.Pipeline07InformationGovernance.md",
            [StageType.Security] = "Genesis.AI.Infrastructure.Prompts.Pipeline08Security.md",
            [StageType.Normalisation] = "Genesis.AI.Infrastructure.Prompts.Pipeline09Normalisation.md",
            [StageType.Planning] = "Genesis.AI.Infrastructure.Prompts.Pipeline10Planning.md",
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

    // Single-file prototype builder: PrototypeDemoGeneration.md + EMIS-X UI kit reference.
    // Assembled once and placed in the stable (cached) prompt part so the UI kit is not
    // re-billed on every conversation turn.
    private static string LoadPrototypeSingleFilePrompt()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prompt = LoadEmbeddedResource(assembly, "Genesis.AI.Infrastructure.Prompts.PrototypeDemoGeneration.md");
        var uiKit = LoadEmbeddedResource(assembly, "Genesis.AI.Infrastructure.Resources.emis-x-ui-kit.md");

        return $"""
            {prompt}

            ## EMIS-X Design System Reference

            {uiKit}
            """;
    }

    private static string LoadEmbeddedResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
