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
        return PrototypeDomToolBuilder.BuildSetNodeAttributeTool();
    }

    internal static AiToolDefinition BuildSetNodeTextTool()
    {
        return PrototypeDomToolBuilder.BuildSetNodeTextTool();
    }

    internal static AiToolDefinition BuildAddNodeClassTool()
    {
        return PrototypeDomToolBuilder.BuildAddNodeClassTool();
    }

    internal static AiToolDefinition BuildRemoveNodeClassTool()
    {
        return PrototypeDomToolBuilder.BuildRemoveNodeClassTool();
    }

    internal static AiToolDefinition BuildInsertAdjacentHtmlTool()
    {
        return PrototypeDomToolBuilder.BuildInsertAdjacentHtmlTool();
    }

    internal static AiToolDefinition BuildRemoveElementTool()
    {
        return PrototypeDomToolBuilder.BuildRemoveElementTool();
    }

    internal static AiToolDefinition BuildApplyToScopeTool()
    {
        return PrototypeDomToolBuilder.BuildApplyToScopeTool();
    }
}
