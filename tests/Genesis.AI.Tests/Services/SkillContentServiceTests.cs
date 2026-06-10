using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public class SkillContentServiceTests
{
    private readonly SkillContentService _service = new();

    [Fact]
    public void GetAvailableSkills_WhenSkillsEmbedded_ReturnsNonEmptyList()
    {
        var skills = _service.GetAvailableSkills();

        Assert.NotEmpty(skills);
    }

    [Fact]
    public void GetSkillContent_ForKnownSkill_ReturnsContent()
    {
        var knownSkill = _service.GetAvailableSkills()[0];

        var content = _service.GetSkillContent(knownSkill);

        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public void GetSkillContent_ForKnownSkillWithDifferentCasing_ReturnsContent()
    {
        var knownSkill = _service.GetAvailableSkills()[0];

        var content = _service.GetSkillContent(knownSkill.ToUpperInvariant());

        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public void GetSkillContent_ForUnknownSkill_ReturnsNull()
    {
        var content = _service.GetSkillContent("this-skill-does-not-exist");

        Assert.Null(content);
    }

    [Fact]
    public void GetSkillContent_ForKnownSkill_StripsYamlFrontmatter()
    {
        var knownSkill = _service.GetAvailableSkills()[0];

        var content = _service.GetSkillContent(knownSkill);

        Assert.NotNull(content);
        Assert.False(content!.StartsWith("---", StringComparison.Ordinal));
    }
}
