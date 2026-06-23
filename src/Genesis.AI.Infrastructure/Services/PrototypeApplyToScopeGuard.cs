using System.Text.RegularExpressions;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Structural guard on the apply_to_scope mutation path. Rejects selectors and inserted
/// HTML that reference a class absent from the loaded fragment scope (invented classes are
/// physically unwritable), and rejects inserted HTML that authors CSS.
/// </summary>
public static class PrototypeApplyToScopeGuard
{
    private const string InsertAdjacentHtml = "insert_adjacent_html";

    private static readonly Regex SelectorClassRegex =
        new(@"\.(-?[_a-zA-Z][_a-zA-Z0-9-]*)", RegexOptions.Compiled);

    private static readonly Regex HtmlClassAttributeRegex =
        new(@"class\s*=\s*[""']([^""']*)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex StyleTagRegex =
        new(@"<\s*style", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex StyleAttributeRegex =
        new(@"style\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CssRuleRegex =
        new(@"\{[^{}]*[A-Za-z-]+\s*:\s*[^;{}]+;", RegexOptions.Compiled);

    public static string? Validate(
        string scope,
        string? selector,
        string? operation,
        string? value,
        IReadOnlyCollection<string> existingClasses)
    {
        var isInsertHtml = string.Equals(operation, InsertAdjacentHtml, StringComparison.OrdinalIgnoreCase);

        if (isInsertHtml && !string.IsNullOrWhiteSpace(value) && ContainsCss(value))
        {
            return "NOTHING WAS WRITTEN. insert HTML only, reuse an existing class, do not author CSS.";
        }

        if (existingClasses.Count == 0)
        {
            return null;
        }

        var existingSet = new HashSet<string>(existingClasses, StringComparer.Ordinal);

        var referencedClasses = ExtractSelectorClasses(selector).ToList();
        if (isInsertHtml && !string.IsNullOrWhiteSpace(value))
        {
            referencedClasses.AddRange(ExtractHtmlClasses(value));
        }

        var inventedClass = referencedClasses.FirstOrDefault(className => !existingSet.Contains(className));
        if (inventedClass is not null)
        {
            var existingList = string.Join(", ", existingClasses.Select(className => $".{className}"));
            return $"NOTHING WAS WRITTEN. class '.{inventedClass}' does not exist in scope '{scope}'. " +
                   $"Classes that exist here: {existingList}. " +
                   "Reuse one of these exact classes, or ask the user to paste the exact HTML element.";
        }

        return null;
    }

    private static bool ContainsCss(string value)
    {
        return StyleTagRegex.IsMatch(value) ||
               StyleAttributeRegex.IsMatch(value) ||
               CssRuleRegex.IsMatch(value);
    }

    private static IEnumerable<string> ExtractSelectorClasses(string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return [];
        }

        return SelectorClassRegex.Matches(selector).Select(match => match.Groups[1].Value);
    }

    private static IEnumerable<string> ExtractHtmlClasses(string value)
    {
        return HtmlClassAttributeRegex
            .Matches(value)
            .SelectMany(match => match.Groups[1].Value.Split(
                [' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
    }
}
