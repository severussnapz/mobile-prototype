using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Configuration;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Defines the tools available to the AI during pipeline conversations.
/// These are Claude tool-use definitions that produce structured JSON calls
/// instead of relying on prompt-based markers.
/// </summary>
public static class PipelineToolDefinitions
{
    private static readonly Lazy<IReadOnlyList<AiToolDefinition>> _tools =
        new(PipelineToolDefinitionFactory.BuildBaseTools);

    public static IReadOnlyList<AiToolDefinition> All => _tools.Value;

    public const string SaveArtefact = "save_artefact";
    public const string AdvancePhase = "advance_phase";
    public const string AddParkingLotItem = "add_parking_lot_item";
    public const string ResolveParkingLotItem = "resolve_parking_lot_item";
    public const string UpdateProgress = "update_progress";
    public const string GetGuardrailDetails = "get_guardrail_details";
    public const string ListArtefacts = "list_artefacts";
    public const string GetArtefact = "get_artefact";
    public const string SetOrchestrationMode = "set_orchestration_mode";
    public const string AdvanceRequirement = "advance_requirement";
    public const string EditArtefact = "edit_artefact";
    public const string SearchInArtefact = "search_in_artefact";

    /// <summary>
    /// Returns the tool list conditioned on <paramref name="options"/> and <paramref name="stageType"/>.
    /// Includes <c>edit_artefact</c> when <see cref="TokenOptimisationOptions.EditArtefactEnabled"/> is true.
    /// Excludes <c>get_guardrail_details</c> when the stage already has skills injected via active skill injection
    /// — Claude has everything it needs in the system prompt and should not burn tool turns fetching skills.
    /// </summary>
    public static IReadOnlyList<AiToolDefinition> GetTools(TokenOptimisationOptions options, Domain.Enums.StageType? stageType = null)
    {
        var base_ = new List<AiToolDefinition>(All);

        // Remove get_guardrail_details for stages where active skills are pre-injected.
        // Stages that have entries in PhaseSkillMap.StageSkills have their guardrails in the
        // cached system prompt already — offering the fetch tool wastes tool turns.
        if (options.ActiveSkillInjectionEnabled && stageType.HasValue
            && Configuration.PhaseSkillMap.HasStageSkills(stageType.Value))
        {
            base_.RemoveAll(tool => tool.Name == GetGuardrailDetails);
        }

        if (!options.EditArtefactEnabled)
            return base_.AsReadOnly();

        base_.Add(PipelineToolDefinitionFactory.BuildEditArtefactTool());
        base_.Add(PipelineToolDefinitionFactory.BuildSearchInArtefactTool());
        return base_.AsReadOnly();
    }
}
