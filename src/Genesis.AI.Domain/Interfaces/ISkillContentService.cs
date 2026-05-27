namespace Genesis.AI.Domain.Interfaces;

public interface ISkillContentService
{
    string? GetSkillContent(string skillName);

    IReadOnlyList<string> GetAvailableSkills();
}
