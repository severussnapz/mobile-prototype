using System.Globalization;
using System.Text.RegularExpressions;

namespace Genesis.AI.Infrastructure.Services;

internal readonly record struct StructuralValidationResult(bool IsValid, string? Reason)
{
    internal static StructuralValidationResult Ok()
    {
        return new StructuralValidationResult(true, null);
    }

    internal static StructuralValidationResult Fail(string reason)
    {
        return new StructuralValidationResult(false, reason);
    }
}

internal static class StructuralEditHtmlUtilities
{
    internal static string BuildScreenPath(int order, string slug)
    {
        return string.Create(CultureInfo.InvariantCulture, $"prototype/fragments/screen-{order:00}-{slug}.html");
    }

    internal static int ExtractScreenNumber(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (fileName.Length < 9)
        {
            return int.MaxValue;
        }

        var numberPart = fileName.Substring(7, 2);
        return int.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : int.MaxValue;
    }

    internal static string ExtractSlug(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (fileName.StartsWith("screen-", StringComparison.OrdinalIgnoreCase) && fileName.Length > 10)
        {
            return fileName.Substring(10);
        }

        return "screen";
    }

    internal static string ResolveContentType(string filePath)
    {
        if (filePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            return "text/html";
        }

        if (filePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
        {
            return "text/css";
        }

        if (filePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        {
            return "application/javascript";
        }

        return "text/plain";
    }

    internal static StructuralValidationResult ValidateDraftFragmentContent(string htmlContent, string filePath)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return StructuralValidationResult.Fail($"Draft fragment content is empty for {filePath}.");
        }

        var withoutScriptAndStyle = Regex.Replace(
            htmlContent,
            @"<script\b[^>]*>[\s\S]*?</script>",
            string.Empty,
            RegexOptions.IgnoreCase);

        withoutScriptAndStyle = Regex.Replace(
            withoutScriptAndStyle,
            @"<style\b[^>]*>[\s\S]*?</style>",
            string.Empty,
            RegexOptions.IgnoreCase);

        if (Regex.IsMatch(
                withoutScriptAndStyle,
                @"<[^>\n]*<[^>\n]*>",
                RegexOptions.IgnoreCase))
        {
            return StructuralValidationResult.Fail($"Draft fragment contains malformed tag boundaries for {filePath}.");
        }

        return StructuralValidationResult.Ok();
    }

    internal static string EnsureRootSectionId(string content, string screenId)
    {
        var rootSectionPattern = "<section\\b[^>]*>";
        var match = Regex.Match(content, rootSectionPattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return content;
        }

        var sectionTag = match.Value;
        var hasId = Regex.IsMatch(sectionTag, "\\sid=\\\"[^\\\"]*\\\"", RegexOptions.IgnoreCase);
        string updatedTag;
        if (hasId)
        {
            updatedTag = Regex.Replace(
                sectionTag,
                "\\sid=\\\"[^\\\"]*\\\"",
                $" id=\"{screenId}\"",
                RegexOptions.IgnoreCase);
        }
        else
        {
            updatedTag = sectionTag.Insert(sectionTag.Length - 1, $" id=\"{screenId}\"");
        }

        return content.Replace(sectionTag, updatedTag, StringComparison.Ordinal);
    }

    internal static string ToggleHiddenOnRootSection(string content, bool hidden)
    {
        var rootSectionPattern = "<section\\b[^>]*>";
        var match = Regex.Match(content, rootSectionPattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return content;
        }

        var sectionTag = match.Value;
        var hasHidden = Regex.IsMatch(sectionTag, "\\shidden(=\\\"hidden\\\")?", RegexOptions.IgnoreCase);

        var updatedTag = sectionTag;
        if (hidden && !hasHidden)
        {
            updatedTag = sectionTag.Insert(sectionTag.Length - 1, " hidden");
        }
        else if (!hidden && hasHidden)
        {
            updatedTag = Regex.Replace(sectionTag, "\\s+hidden(=\\\"hidden\\\")?", string.Empty, RegexOptions.IgnoreCase);
        }

        return content.Replace(sectionTag, updatedTag, StringComparison.Ordinal);
    }
}
