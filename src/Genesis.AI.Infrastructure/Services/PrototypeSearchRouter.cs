namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Decides whether a search_in_artefact call should be served by the structured DOM
/// search (which returns elements with their real ClassList) instead of the raw-text
/// line search. HTML prototype artefacts route to DOM search so the agent receives an
/// authoritative selector rather than mining class names out of raw HTML; non-HTML
/// prototype files (_styles.css, _app.js, data.js) and all other artefacts keep the
/// text search.
/// </summary>
public static class PrototypeSearchRouter
{
    private const string AssembledPrototypePath = "prototype/index.html";
    private const string FragmentsPrefix = "prototype/fragments/";
    private const string HtmlExtension = ".html";

    public static bool ShouldRouteToDomSearch(string filePath, bool domModeEnabled)
    {
        if (!domModeEnabled)
        {
            return false;
        }

        if (filePath.Equals(AssembledPrototypePath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return filePath.StartsWith(FragmentsPrefix, StringComparison.OrdinalIgnoreCase)
            && filePath.EndsWith(HtmlExtension, StringComparison.OrdinalIgnoreCase);
    }
}
