using System.Text;
using AngleSharp.Dom;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class PrototypeDomMutationHelper
{
    internal static IElement? ResolveTargetElement(IDocument document, string nodeKey, string fragmentPath)
    {
        var stableLocator = ExtractStableLocator(nodeKey, fragmentPath);
        if (string.IsNullOrWhiteSpace(stableLocator))
        {
            return null;
        }

        if (stableLocator.StartsWith("css:", StringComparison.Ordinal))
        {
            stableLocator = stableLocator[4..];
        }

        var dataGenesisMatch = document.QuerySelector(
            $"[data-genesis-id=\"{EscapeCssString(stableLocator)}\"]");
        if (dataGenesisMatch is not null)
        {
            return dataGenesisMatch;
        }

        if (stableLocator.Length > 0 && !char.IsDigit(stableLocator[0]))
        {
            var idMatch = document.QuerySelector($"#{EscapeCssIdentifier(stableLocator)}");
            if (idMatch is not null)
            {
                return idMatch;
            }
        }

        try
        {
            return document.QuerySelector(stableLocator);
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static string? ApplyMutation(
        IElement element, PrototypeDomMutationOperation operation,
        string? attribute, string? value)
    {
        switch (operation)
        {
            case PrototypeDomMutationOperation.SetAttribute:
                if (string.IsNullOrWhiteSpace(attribute)) { return "attribute is required for SetAttribute"; }
                element.SetAttribute(attribute, value ?? string.Empty);
                return null;
            case PrototypeDomMutationOperation.SetText:
                element.TextContent = value ?? string.Empty;
                return null;
            case PrototypeDomMutationOperation.AddClass:
                if (string.IsNullOrWhiteSpace(value)) { return "value is required for AddClass"; }
                element.ClassList.Add(value);
                return null;
            case PrototypeDomMutationOperation.RemoveClass:
                if (string.IsNullOrWhiteSpace(value)) { return "value is required for RemoveClass"; }
                element.ClassList.Remove(value);
                return null;
            case PrototypeDomMutationOperation.InsertAdjacentHtml:
                return ApplyInsertAdjacentHtml(element, attribute, value);
            case PrototypeDomMutationOperation.RemoveElement:
                element.Remove();
                return null;
            case PrototypeDomMutationOperation.RemoveAttribute:
                if (string.IsNullOrWhiteSpace(attribute)) { return "attribute is required for RemoveAttribute"; }
                element.RemoveAttribute(attribute);
                return null;
            case PrototypeDomMutationOperation.SwapClass:
                if (string.IsNullOrWhiteSpace(value)) { return "value is required for SwapClass — format: old-class:new-class"; }
                var swapParts = value.Split(':', 2);
                if (swapParts.Length != 2) { return "SwapClass value must be in format old-class:new-class"; }
                element.ClassList.Remove(swapParts[0].Trim());
                element.ClassList.Add(swapParts[1].Trim());
                return null;
            default:
                return "unsupported operation";
        }
    }

    internal static string SerializeDocument(IDocument document, string originalHtml)
    {
        if (LooksLikeDocument(originalHtml))
        {
            var documentElement = document.DocumentElement?.OuterHtml ?? string.Empty;
            var doctype = document.Doctype is not null ? $"<!DOCTYPE {document.Doctype.Name}>" : null;
            if (string.IsNullOrWhiteSpace(doctype)) { return documentElement; }
            return string.Concat(doctype, documentElement);
        }

        return document.Body?.InnerHtml ?? string.Empty;
    }

    private static string? ApplyInsertAdjacentHtml(IElement element, string? attribute, string? value)
    {
        if (string.IsNullOrWhiteSpace(attribute)) { return "position is required for InsertAdjacentHtml"; }
        if (!TryParseInsertPosition(attribute, out var insertPosition)) { return "position must be one of: beforebegin, afterbegin, beforeend, afterend"; }
        if (string.IsNullOrWhiteSpace(value)) { return "value is required for InsertAdjacentHtml"; }
        element.Insert(insertPosition, value);
        return null;
    }

    private static bool TryParseInsertPosition(string position, out AdjacentPosition adjacentPosition)
    {
        if (position.Equals("beforebegin", StringComparison.OrdinalIgnoreCase)) { adjacentPosition = AdjacentPosition.BeforeBegin; return true; }
        if (position.Equals("afterbegin", StringComparison.OrdinalIgnoreCase)) { adjacentPosition = AdjacentPosition.AfterBegin; return true; }
        if (position.Equals("beforeend", StringComparison.OrdinalIgnoreCase)) { adjacentPosition = AdjacentPosition.BeforeEnd; return true; }
        if (position.Equals("afterend", StringComparison.OrdinalIgnoreCase)) { adjacentPosition = AdjacentPosition.AfterEnd; return true; }
        adjacentPosition = AdjacentPosition.BeforeEnd;
        return false;
    }

    private static string ExtractStableLocator(string nodeKey, string fragmentPath)
    {
        if (string.IsNullOrWhiteSpace(nodeKey)) { return string.Empty; }
        var separatorIndex = nodeKey.IndexOf('|', StringComparison.Ordinal);
        if (separatorIndex < 0) { return nodeKey.Trim(); }
        return nodeKey[(separatorIndex + 1)..].Trim();
    }

    private static bool LooksLikeDocument(string html)
    {
        var trimmed = html.TrimStart();
        return trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }

    private static string EscapeCssString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string EscapeCssIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_') { builder.Append(character); continue; }
            builder.Append('\\');
            builder.Append(character);
        }

        return builder.ToString();
    }
}
