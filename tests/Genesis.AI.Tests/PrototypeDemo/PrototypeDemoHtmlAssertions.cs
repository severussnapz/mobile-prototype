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

    // ---- Day 2 additions (Bedrock-backed generator) ----

    private const string EmisXUiKitResourceName =
        "Genesis.AI.Infrastructure.Resources.emis-x-ui-kit.md";

    // The exact banner wording pinned by the draft v0.1 generation prompt.
    public const string ExactPrototypeBanner =
        "PROTOTYPE ONLY — Requirements validation artefact. Not for production use.";

    public static void AssertContainsExactPrototypeBanner(string html)
    {
        Assert.Contains(ExactPrototypeBanner, html, StringComparison.Ordinal);
    }

    // Refines AssertNoFormatValidNhsNumbers: the prompt sanctions two obvious-fake
    // placeholders (000 000 0000 / 999 999 9999) which the raw 3-3-4 pattern would
    // otherwise trip. Every 3-3-4 number in the output must be one of those two;
    // anything else is a format-plausible number and a clinical-safety violation.
    public static void AssertNoPlausibleNhsNumbers(string html)
    {
        foreach (System.Text.RegularExpressions.Match match in Regex.Matches(html, NhsNumberPattern))
        {
            var digits = new string(match.Value.Where(char.IsDigit).ToArray());
            Assert.True(
                digits is "0000000000" or "9999999999",
                $"Output contains a format-plausible NHS number: '{match.Value}'.");
        }
    }

    // emis-x mode is fully self-contained — external stylesheet/script files
    // (CDN links) are only permitted in bootstrap/tailwind modes.
    public static void AssertNoExternalCdnReferences(string html)
    {
        Assert.DoesNotMatch(new Regex(@"<link[^>]+href\s*=\s*[""']https?:", RegexOptions.IgnoreCase), html);
        Assert.DoesNotMatch(new Regex(@"<script[^>]+src\s*=\s*[""']https?:", RegexOptions.IgnoreCase), html);
    }

    // The prompt's OUTPUT CONTRACT caps primary screens at 5 (extras listed in a
    // comment). Screens are counted via the data-screen marker so the bound is
    // machine-checkable.
    // NOTE: draft v0.1 of the prompt does not yet mandate the data-screen marker —
    // flagged for prompt review. The golden fixture uses it so this check is
    // deterministic until the prompt contract adopts it.
    public static void AssertScreenCountWithinBound(string html)
    {
        var screenCount = Regex.Count(html, @"data-screen\b", RegexOptions.IgnoreCase);
        Assert.True(screenCount <= 5, $"Expected at most 5 primary screens, found {screenCount}.");
    }

    // Asserts the assembled system prompt carries both the project requirements
    // and the EMIS-X UI kit — agnostic to the stable/mutable cache split, which
    // is a Day 3 implementation choice.
    public static void AssertSystemPromptContains(string systemPrompt, string requirementMarker)
    {
        Assert.Contains(requirementMarker, systemPrompt, StringComparison.Ordinal);
        Assert.Contains(LoadEmbeddedUiKitMarker(), systemPrompt, StringComparison.Ordinal);
    }

    private static string LoadEmbeddedUiKitMarker()
    {
        var assembly = typeof(StubPrototypeDemoGenerationService).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmisXUiKitResourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {EmisXUiKitResourceName}");
        using var reader = new StreamReader(stream);
        var uiKit = reader.ReadToEnd();
        return uiKit.Substring(50, 120);
    }
}
