using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public interface IPromptService
{
    string GetSystemPrompt(StageType stageType);
    int GetTotalPhases(StageType stageType);
    string[] GetPhaseNames(StageType stageType);

    /// <summary>
    /// Returns the single-file prototype builder system prompt (PrototypeDemoGeneration.md)
    /// with the EMIS-X UI kit reference appended. Intended for the stable (cached) part of
    /// the Bedrock system prompt when <c>PrototypeSingleFileEnabled</c> is active.
    /// </summary>
    string GetPrototypeSingleFilePrompt();
}
