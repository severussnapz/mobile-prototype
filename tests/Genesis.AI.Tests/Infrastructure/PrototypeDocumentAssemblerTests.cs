using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="PrototypeDocumentAssembler"/>.
/// The assembler must strip the Markdown code fence the model wraps its output in
/// before injecting the EMIS-X base CSS into the HTML document.
/// </summary>
public class PrototypeDocumentAssemblerTests
{
    private static PrototypeDocumentAssembler CreateSut() => new();

    private const string MinimalHtml =
        "<!DOCTYPE html><html><head></head><body>demo</body></html>";

    // ── fence stripping ──────────────────────────────────────────────────────

    [Fact]
    public void Assemble_WithBacktickHtmlFence_StripsLeadingFenceBeforeDoctype()
    {
        var input = $"```html\n{MinimalHtml}\n```";

        var result = CreateSut().Assemble(input);

        Assert.False(result.TrimStart().StartsWith("```", StringComparison.Ordinal),
            "Output must not start with a Markdown fence.");
        Assert.True(result.Contains("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase),
            "DOCTYPE must survive fence stripping.");
    }

    [Fact]
    public void Assemble_WithBacktickHtmlFence_StripsTrailingFence()
    {
        var input = $"```html\n{MinimalHtml}\n```";

        var result = CreateSut().Assemble(input);

        Assert.False(result.TrimEnd().EndsWith("```", StringComparison.Ordinal),
            "Output must not end with a Markdown fence.");
        Assert.True(result.TrimEnd().EndsWith("</html>", StringComparison.OrdinalIgnoreCase),
            "Output must end with </html> after fence removal.");
    }

    [Fact]
    public void Assemble_WithBacktickFence_StillInjectsEmisXCssIntoHead()
    {
        var input = $"```html\n{MinimalHtml}\n```";

        var result = CreateSut().Assemble(input);

        // CSS injection proof: the assembler inserts a <style> block before </head>.
        Assert.Contains("<style>", result, StringComparison.OrdinalIgnoreCase);
        var styleIndex = result.IndexOf("<style>", StringComparison.OrdinalIgnoreCase);
        var closingHeadIndex = result.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        Assert.True(styleIndex < closingHeadIndex,
            "<style> must appear before </head>.");
    }

    [Fact]
    public void Assemble_WithPlainBacktickFence_NoLanguageTag_StripsCorrectly()
    {
        // Model may emit ``` without a language tag.
        var input = $"```\n{MinimalHtml}\n```";

        var result = CreateSut().Assemble(input);

        Assert.False(result.TrimStart().StartsWith("```", StringComparison.Ordinal));
        Assert.True(result.Contains("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase));
    }

    // ── negative: inner backticks must not be touched ────────────────────────

    [Fact]
    public void Assemble_WithBackticksInsideScriptTag_DoesNotAlterInnerContent()
    {
        // A legitimate document that contains backticks inside a script — not a fence.
        const string htmlWithInnerBackticks =
            "<!DOCTYPE html><html><head></head><body>" +
            "<script>const x = `hello`;</script>" +
            "</body></html>";

        var result = CreateSut().Assemble(htmlWithInnerBackticks);

        Assert.Contains("`hello`", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Assemble_FencedInputWithInnerTripleBackticks_StripsOuterFenceAndPreservesInnerBackticks()
    {
        // The outer fence is stripped; the triple-backticks inside the <script>
        // template literal must survive intact. LastIndexOf must select the trailing
        // fence, not the inner one.
        const string innerBackticks = "```";
        var html =
            "<!DOCTYPE html><html><head></head><body>" +
            $"<script>const tag = {innerBackticks}hello{innerBackticks};</script>" +
            "</body></html>";
        var fenced = $"```html\n{html}\n```";

        var result = CreateSut().Assemble(fenced);

        // Outer fence stripped.
        Assert.False(result.TrimStart().StartsWith("```", StringComparison.Ordinal),
            "Output must not start with the outer Markdown fence.");
        Assert.False(result.TrimEnd().EndsWith("```", StringComparison.Ordinal),
            "Output must not end with the outer Markdown fence.");

        // Document boundaries preserved.
        Assert.True(result.TrimStart().StartsWith("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase),
            "Output must start with <!DOCTYPE html>.");
        Assert.True(result.TrimEnd().EndsWith("</html>", StringComparison.OrdinalIgnoreCase),
            "Output must end with </html>.");

        // Inner triple-backticks untouched.
        Assert.Contains($"{innerBackticks}hello{innerBackticks}", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Assemble_WithNoFence_StillInjectsCss()
    {
        // No fence at all — existing CSS-injection path must be unaffected.
        var result = CreateSut().Assemble(MinimalHtml);

        Assert.Contains("<style>", result, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.TrimStart().StartsWith("```", StringComparison.Ordinal));
    }
}
