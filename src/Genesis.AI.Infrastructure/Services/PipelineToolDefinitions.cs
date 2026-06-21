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
    public const string EditArtefactByGraphNode = "edit_artefact_by_graph_node";
    public const string SearchInArtefact = "search_in_artefact";
    public const string SetNodeAttribute = "set_node_attribute";
    public const string SetNodeText = "set_node_text";
    public const string AddNodeClass = "add_node_class";
    public const string RemoveNodeClass = "remove_node_class";
    public const string InsertAdjacentHtml = "insert_adjacent_html";
    public const string RemoveElement = "remove_element";
    public const string ApplyToScope = "apply_to_scope";
    public const string ProposeRequirementChange = "propose_requirement_change";

    /// <summary>
    /// Returns the tool list conditioned on <paramref name="options"/> and <paramref name="stageType"/>.
    /// <c>edit_artefact</c> is registered for all non-Prototype stages when <see cref="TokenOptimisationOptions.EditArtefactEnabled"/> is true.
    /// Prototype stage uses node-targeted tools; <c>edit_artefact_by_graph_node</c> is offered only when DOM mode is disabled.
    /// Excludes <c>get_guardrail_details</c> when the stage already has skills injected via active skill injection.
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

        if (options.RequirementFeedbackEnabled)
        {
            base_.Add(FeedbackToolBuilder.BuildProposeRequirementChangeTool());
        }

        if (!options.EditArtefactEnabled)
            return base_.AsReadOnly();

        // Prototype stage uses node-targeted editing tools.
        // Keep graph-node replacement only when DOM mode is disabled.
        if (stageType == Domain.Enums.StageType.Prototype)
        {
            if (!options.PrototypeDomModeEnabled)
            {
                base_.Add(PipelineToolDefinitionFactory.BuildEditArtefactByGraphNodeTool());
            }

            base_.Add(PipelineToolDefinitionFactory.BuildSetNodeAttributeTool());
            base_.Add(PipelineToolDefinitionFactory.BuildSetNodeTextTool());
            base_.Add(PipelineToolDefinitionFactory.BuildAddNodeClassTool());
            base_.Add(PipelineToolDefinitionFactory.BuildRemoveNodeClassTool());

            if (options.PrototypeDomModeEnabled)
            {
                base_.Add(PipelineToolDefinitionFactory.BuildInsertAdjacentHtmlTool());
                base_.Add(PipelineToolDefinitionFactory.BuildRemoveElementTool());
                base_.Add(PipelineToolDefinitionFactory.BuildApplyToScopeTool());
            }
        }
        else
        {
            base_.Add(PipelineToolDefinitionFactory.BuildEditArtefactTool());
        }

        base_.Add(PipelineToolDefinitionFactory.BuildSearchInArtefactTool());

        return base_.AsReadOnly();
    }
}
