using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public interface IPromptService
{
    string GetSystemPrompt(StageType stageType);
    int GetTotalPhases(StageType stageType);
    string[] GetPhaseNames(StageType stageType);
}
