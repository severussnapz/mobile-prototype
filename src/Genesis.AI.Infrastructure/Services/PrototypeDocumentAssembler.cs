using System.Reflection;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Inlines <c>emis-x-base.css</c> into the <c>&lt;head&gt;</c> of a raw HTML document
/// produced by the prototype-demo generation service.
///
/// This is the single authoritative assembly step shared by both the synchronous
/// prototype-demo endpoint (via <see cref="BedrockPrototypeDemoGenerationService"/>)
/// and the SSE streaming endpoint (via <c>PrototypeDemoStreamController</c>).
/// Neither path may duplicate this logic — both must call this class so the two
/// endpoints cannot drift.
///
/// Registered as a singleton: CSS is loaded once from the embedded resource at
/// construction time and reused across all requests.
/// </summary>
public sealed class PrototypeDocumentAssembler : IPrototypeDocumentAssembler
{
    private const string BaseCssResourceName =
        "Genesis.AI.Infrastructure.Resources.emis-x-base.css";

    private readonly string _css;

    public PrototypeDocumentAssembler()
    {
        _css = LoadEmbeddedText(BaseCssResourceName);
    }

    /// <inheritdoc />
    public string Assemble(string rawHtml)
    {
        var html = StripMarkdownFence(rawHtml);

        const string closingHead = "</head>";
        var index = html.IndexOf(closingHead, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            // ponytail: malformed model output — no </head> found; return as-is rather than
            // throwing so the caller can still surface whatever the model produced.
            return html;
        }

        return html.Insert(index, $"<style>\n{_css}\n</style>\n");
    }

    /// <summary>
    /// Strips an outer Markdown code fence (```html…``` or ```…```) that the model
    /// may wrap its HTML output in. Only the outermost fence is removed; backticks
    /// inside the document body (e.g. in a &lt;script&gt; template literal) are untouched.
    /// </summary>
    private static string StripMarkdownFence(string input)
    {
        var trimmed = input.Trim();

        // Must start with ``` (with optional language tag) and end with ```.
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return input;
        }

        // Find the end of the opening fence line (skip past the optional language tag).
        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline < 0)
        {
            return input;
        }

        // Verify the content ends with a closing ```.
        if (!trimmed.EndsWith("```", StringComparison.Ordinal))
        {
            return input;
        }

        // Remove leading fence line and trailing fence.
        var afterOpenFence = trimmed[(firstNewline + 1)..];
        var closingFenceStart = afterOpenFence.LastIndexOf("```", StringComparison.Ordinal);
        if (closingFenceStart < 0)
        {
            return input;
        }

        return afterOpenFence[..closingFenceStart].Trim();
    }

    private static string LoadEmbeddedText(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
