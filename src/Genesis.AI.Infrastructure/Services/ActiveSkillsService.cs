using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Builds the active skill content block for injection into the Bedrock system prompt.
///
/// Uses <see cref="PhaseSkillMap"/> to determine which skill names apply for the
/// given stage and phase, then loads each skill's content from
/// <see cref="ISkillContentService"/>. Skills are concatenated in map order
/// (universal → stage → phase override) with a markdown heading separator.
/// </summary>
public sealed class ActiveSkillsService : IActiveSkillsService
{
    private readonly ISkillContentService _skillContentService;
    private readonly ILogger<ActiveSkillsService> _logger;

    public ActiveSkillsService(
        ISkillContentService skillContentService,
        ILogger<ActiveSkillsService> logger)
    {
        _skillContentService = skillContentService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> BuildActiveSkillsAsync(
        StageType stageType,
        int currentPhase,
        CancellationToken cancellationToken)
    {
        var skillNames = PhaseSkillMap.GetSkillsForPhase(stageType, currentPhase);

        if (skillNames.Count == 0)
        {
            return Task.FromResult(string.Empty);
        }

        var parts = new List<string>(skillNames.Count);

        foreach (var skillName in skillNames)
        {
            var content = _skillContentService.GetSkillContent(skillName);

            if (content is null)
            {
                _logger.LogWarning(
                    "Active skill '{SkillName}' referenced by PhaseSkillMap for stage {StageType} phase {Phase} was not found in embedded resources.",
                    skillName,
                    stageType,
                    currentPhase);

                continue;
            }

            parts.Add(content);
        }

        if (parts.Count == 0)
        {
            return Task.FromResult(string.Empty);
        }

        var result = string.Join("\n\n---\n\n", parts);
        return Task.FromResult(result);
    }
}
