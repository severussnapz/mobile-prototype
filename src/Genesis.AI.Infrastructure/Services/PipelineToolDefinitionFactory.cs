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
}
