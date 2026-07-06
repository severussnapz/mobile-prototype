namespace Genesis.AI.Domain.Interfaces;

public sealed record PrototypeDomSearchMatch(
    string NodeKey,
    string FragmentPath,
    string TagName,
    string TextSnippet,
    string CssSelector,
    IReadOnlyList<string> ClassList,
    string ParentContext,
    string SiblingContext);
