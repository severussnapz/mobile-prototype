using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Commands.GenerateSessionClose;

public static class SessionCloseStageMap
{
    public static string GetFilePath(StageType stageType) => stageType switch
    {
        StageType.RequirementsDiscovery => "session-close/SESSION-CLOSE-P01.md",
        StageType.Prototype => "session-close/SESSION-CLOSE-P02.md",
        StageType.Architecture => "session-close/SESSION-CLOSE-P03.md",
        StageType.Design => "session-close/SESSION-CLOSE-P04.md",
        StageType.Pxd => "session-close/SESSION-CLOSE-P05.md",
        StageType.ClinicalSafety => "session-close/SESSION-CLOSE-P06.md",
        StageType.InformationGovernance => "session-close/SESSION-CLOSE-P07.md",
        StageType.Security => "session-close/SESSION-CLOSE-P08.md",
        _ => throw new NotSupportedException($"Stage type {stageType} does not support session close.")
    };
}