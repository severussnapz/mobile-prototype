using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public interface ISessionCloseSkillBuilder
{
    string Build(StageType stageType, string conversationSummary);
}