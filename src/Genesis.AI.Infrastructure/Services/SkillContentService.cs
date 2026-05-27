using System.Collections.Frozen;
using System.Reflection;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Provides on-demand access to guardrail/steer skill content.
/// Skills are embedded as resources and served to the AI via the get_guardrail_details tool.
/// </summary>
public sealed class SkillContentService : ISkillContentService
{
    private readonly FrozenDictionary<string, string> _skillCache = LoadAllSkills();

    /// <summary>
    /// Returns the full content of a skill by its directory name (e.g. "emis-x-api-auth").
    /// Returns null if the skill is not found.
    /// </summary>
    public string? GetSkillContent(string skillName)
    {
        return _skillCache.TryGetValue(skillName, out var content) ? content : null;
    }

    /// <summary>
    /// Returns all available skill names.
    /// </summary>
    public IReadOnlyList<string> GetAvailableSkills()
    {
        return _skillCache.Keys.ToList();
    }

    private static FrozenDictionary<string, string> LoadAllSkills()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = "Genesis.AI.Infrastructure.Skills.";
        var suffix = ".md";

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(prefix, StringComparison.Ordinal) ||
                !resourceName.EndsWith(suffix, StringComparison.Ordinal))
                continue;

            // Extract skill name: "Genesis.AI.Infrastructure.Skills.emis-x-api-auth.md" → "emis-x-api-auth"
            var skillName = resourceName[prefix.Length..^suffix.Length];

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            // Strip YAML frontmatter if present
            if (content.StartsWith("---", StringComparison.Ordinal))
            {
                var endIndex = content.IndexOf("\n---", 3, StringComparison.Ordinal);
                if (endIndex > 0)
                {
                    content = content[(endIndex + 4)..].TrimStart();
                }
            }

            result[skillName] = content;
        }

        return result.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
