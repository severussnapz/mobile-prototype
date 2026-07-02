using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Genesis.AI.Infrastructure.Services;
using Xunit;

namespace Genesis.AI.Tests.PrototypeDemo;

// Shared Day 0 harness assertions for the Plan-4 prototype-demo HTML output.
// The same four content checks are exercised at BOTH the service level and the
// handler level (per the Day 1 test plan). Fails to compile until
// StubPrototypeDemoGenerationService exists — that is the intended red.
internal static class PrototypeDemoHtmlAssertions
{
    private const string EmisXBaseCssResourceName =
        "Genesis.AI.Infrastructure.Resources.emis-x-base.css";

    // Format-valid NHS number shape (3-3-4 with optional single spaces). The
    // prototype must never emit anything matching this — demo data only.
    private const string NhsNumberPattern = @"\d{3}\s?\d{3}\s?\d{4}";

    // Materialises the streaming generation output into a single HTML string.
    // Day 1 the controller does this synchronously; Day 2 it streams the same
    // chunks over SSE — the service contract is unchanged either way.
    public static async Task<string> CollectAsync(IAsyncEnumerable<string> stream)
    {
        var builder = new StringBuilder();
        await foreach (var chunk in stream)
        {
            builder.Append(chunk);
        }

        return builder.ToString();
    }

    public static async IAsyncEnumerable<string> AsAsyncStream(params string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.CompletedTask;
            yield return chunk;
        }
    }

    public static void AssertContainsPrototypeOnlyBanner(string html)
    {
        Assert.Contains("PROTOTYPE ONLY", html, StringComparison.Ordinal);
    }

    public static void AssertEmisXBaseCssInlinedIntoHead(string html)
    {
        var head = ExtractHead(html);
        Assert.Contains(LoadEmbeddedCssMarker(), head, StringComparison.Ordinal);
    }

    public static void AssertCompleteHtmlDocument(string html)
    {
        var trimmed = html.Trim();
        Assert.StartsWith("<!DOCTYPE html>", trimmed, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("</html>", trimmed, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertNoFormatValidNhsNumbers(string html)
    {
        Assert.DoesNotMatch(new Regex(NhsNumberPattern), html);
    }

    private static string ExtractHead(string html)
    {
        var start = html.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
        var end = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        Assert.True(start >= 0 && end > start, "Output must contain a <head>...</head> section.");
        return html.Substring(start, end - start);
    }

    // Reads a stable slice of the actual embedded CSS so the assertion survives
    // future EMIS-X design-token updates without being rewritten. Skips the
    // leading @import line (offset 50) which could legitimately be stripped.
    private static string LoadEmbeddedCssMarker()
    {
        var assembly = typeof(StubPrototypeDemoGenerationService).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmisXBaseCssResourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {EmisXBaseCssResourceName}");
        using var reader = new StreamReader(stream);
        var css = reader.ReadToEnd();
        return css.Substring(50, 120);
    }
}
