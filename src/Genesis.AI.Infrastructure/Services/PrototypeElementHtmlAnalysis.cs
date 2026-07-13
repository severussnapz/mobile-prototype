using System.Text;
using System.Text.RegularExpressions;

namespace Genesis.AI.Infrastructure.Services;

internal static class PrototypeElementHtmlAnalysis
{
    private static readonly HashSet<string> DepthNeutralElementNames = new(
        [
            // HTML void elements
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr",
            // Common SVG elements frequently emitted as leaf nodes
            "path", "circle", "ellipse", "line", "polygon", "polyline", "rect", "stop", "use", "image"
        ],
        StringComparer.OrdinalIgnoreCase);

    public static int CountDirectChildren(string html)
    {
        var inner = ExtractInnerHtml(html);
        return Regex.Count(inner, @"<(?!/)[a-zA-Z]");
    }

    public static string ExtractChildElementsText(string html)
    {
        var inner = ExtractInnerHtml(html);
        if (string.IsNullOrWhiteSpace(inner))
        {
            return string.Empty;
        }

        return ExtractNestedText(inner);
    }

    private static string ExtractNestedText(string inner)
    {
        var builder = new StringBuilder(inner.Length);
        var depth = 0;

        for (var index = 0; index < inner.Length; index++)
        {
            if (inner[index] != '<')
            {
                if (depth >= 1)
                {
                    builder.Append(inner[index]);
                }

                continue;
            }

            var closeIndex = inner.IndexOf('>', index + 1);
            if (closeIndex < 0)
            {
                break;
            }

            UpdateDepth(inner[(index + 1)..closeIndex], ref depth);
            index = closeIndex;
        }

        // ponytail: comments and CDATA are treated as generic tags here; if they
        // become common in model output, upgrade to a parser-backed implementation.
        return builder.ToString().Trim();
    }

    private static void UpdateDepth(string rawTagSlice, ref int depth)
    {
        var rawTag = rawTagSlice.Trim();
        var isClosing = rawTag.Length > 0 && rawTag[0] == '/';
        var isSelfClosing = rawTag.Length > 0 && rawTag[^1] == '/';
        var tagName = ExtractTagName(rawTag);
        var isDepthNeutral = isSelfClosing || DepthNeutralElementNames.Contains(tagName);

        if (isClosing)
        {
            depth = Math.Max(0, depth - 1);
            return;
        }

        if (!isDepthNeutral)
        {
            depth++;
        }
    }

    private static string ExtractInnerHtml(string html)
    {
        var firstClose = html.IndexOf('>');
        if (firstClose < 0 || firstClose >= html.Length - 1)
        {
            return string.Empty;
        }

        var lastOpen = html.LastIndexOf('<');
        if (lastOpen <= firstClose)
        {
            return string.Empty;
        }

        return html[(firstClose + 1)..lastOpen];
    }

    private static string ExtractTagName(string rawTag)
    {
        if (string.IsNullOrWhiteSpace(rawTag))
        {
            return string.Empty;
        }

        var start = rawTag[0] == '/' ? 1 : 0;
        if (start >= rawTag.Length)
        {
            return string.Empty;
        }

        var end = start;
        while (end < rawTag.Length)
        {
            var value = rawTag[end];
            if (char.IsWhiteSpace(value) || value == '/' || value == '>')
            {
                break;
            }

            end++;
        }

        return rawTag[start..end];
    }
}
