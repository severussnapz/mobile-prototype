using System.Text.Json;
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
    {
        return ArtefactToolBuilder.BuildEditArtefactTool();
    }

    internal static AiToolDefinition BuildEditArtefactByGraphNodeTool()
    {
        return ArtefactToolBuilder.BuildEditArtefactByGraphNodeTool();
    }

    internal static AiToolDefinition BuildSearchInArtefactTool()
    {
        return ArtefactToolBuilder.BuildSearchInArtefactTool();
    }

    internal static AiToolDefinition BuildSetNodeAttributeTool()
    {
        return ArtefactToolBuilder.BuildSetNodeAttributeTool();
    }

    internal static AiToolDefinition BuildSetNodeTextTool()
    {
        return ArtefactToolBuilder.BuildSetNodeTextTool();
    }

    internal static AiToolDefinition BuildAddNodeClassTool()
    {
        return ArtefactToolBuilder.BuildAddNodeClassTool();
    }

    internal static AiToolDefinition BuildRemoveNodeClassTool()
    {
        return ArtefactToolBuilder.BuildRemoveNodeClassTool();
    }

    internal static AiToolDefinition BuildInsertAdjacentHtmlTool()
    {
        return ArtefactToolBuilder.BuildInsertAdjacentHtmlTool();
    }

    internal static AiToolDefinition BuildRemoveElementTool()
    {
        return ArtefactToolBuilder.BuildRemoveElementTool();
    }

    internal static AiToolDefinition BuildListElementsTool()
    {
        return ArtefactToolBuilder.BuildListElementsTool();
    }

    internal static AiToolDefinition BuildApplyBulkAttributesTool()
    {
        return ArtefactToolBuilder.BuildApplyBulkAttributesTool();
    }
}
