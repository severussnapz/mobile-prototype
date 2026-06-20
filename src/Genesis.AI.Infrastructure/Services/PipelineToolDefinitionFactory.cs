using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class PipelineToolDefinitionFactory
{
    internal static IReadOnlyList<AiToolDefinition> BuildBaseTools()
    {
        return new AiToolDefinition[]
        {
            ArtefactToolBuilder.BuildSaveArtefactTool(),
            PhaseToolBuilder.BuildAdvancePhaseTool(),
            ParkingLotToolBuilder.BuildAddParkingLotItemTool(),
            ParkingLotToolBuilder.BuildResolveParkingLotItemTool(),
            ProgressToolBuilder.BuildUpdateProgressTool(),
            ArtefactToolBuilder.BuildListArtefactsTool(),
            ArtefactToolBuilder.BuildGetArtefactTool(),
            SkillToolBuilder.BuildGetGuardrailDetailsTool(),
            ProgressToolBuilder.BuildSetOrchestrationModeTool(),
            PhaseToolBuilder.BuildAdvanceRequirementTool()
        };
    }

    internal static AiToolDefinition BuildEditArtefactTool()
        => ArtefactToolBuilder.BuildEditArtefactTool();

    internal static AiToolDefinition BuildEditArtefactByGraphNodeTool()
        => ArtefactToolBuilder.BuildEditArtefactByGraphNodeTool();

    internal static AiToolDefinition BuildSearchInArtefactTool()
        => ArtefactToolBuilder.BuildSearchInArtefactTool();

    internal static AiToolDefinition BuildSetNodeAttributeTool()
        => ArtefactToolBuilder.BuildSetNodeAttributeTool();

    internal static AiToolDefinition BuildSetNodeTextTool()
        => ArtefactToolBuilder.BuildSetNodeTextTool();

    internal static AiToolDefinition BuildAddNodeClassTool()
        => ArtefactToolBuilder.BuildAddNodeClassTool();

    internal static AiToolDefinition BuildRemoveNodeClassTool()
        => ArtefactToolBuilder.BuildRemoveNodeClassTool();

    internal static AiToolDefinition BuildInsertAdjacentHtmlTool()
        => ArtefactToolBuilder.BuildInsertAdjacentHtmlTool();

    internal static AiToolDefinition BuildRemoveElementTool()
        => ArtefactToolBuilder.BuildRemoveElementTool();

    internal static AiToolDefinition BuildApplyToScopeTool()
        => ArtefactToolBuilder.BuildApplyToScopeTool();
}
