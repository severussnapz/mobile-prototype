using System.Text.Json;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class GenerateFromContextStrategy : IApplyToScopeStrategy
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
    };
    private readonly IAiService _aiService;

    public GenerateFromContextStrategy(IAiService aiService)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
    }

    public async Task<IReadOnlyList<ApplyToScopeValueResult>> DeriveValuesAsync(
        IReadOnlyList<PrototypeDomSearchMatch> matches,
        string? literalValue,
        CancellationToken cancellationToken)
    {
        if (matches.Count == 0)
            return [];

        var snippetList = string.Join("\n", matches.Select((m, i) => $"{i + 1}. \"{m.TextSnippet}\""));

        var prompt = "You are generating accessible label values for UI elements.\n" +
            "For each element below, return a clean, descriptive value suitable for an aria-label or title attribute.\n" +
            "Strip emoji, arrows, and duplicate words. Be concise and specific.\n\n" +
            "Elements:\n" +
            snippetList +
            "\n\nRespond ONLY with a JSON array. No preamble, no markdown, no explanation.\n" +
            "Format: [{\"text_snippet\":\"<original>\",\"value\":\"<generated value>\"}]";

        var systemPrompt = AiSystemPrompt.FromFullPrompt(
            "You generate accessibility labels for UI elements. Respond only with valid JSON.");

        var messages = new List<AiMessage>
        {
            new(Genesis.AI.Domain.Enums.MessageRole.User, prompt)
        };

        AiResponse response;
        try
        {
            response = await _aiService.GenerateResponseAsync(systemPrompt, messages, cancellationToken);
        }
        catch (Exception)
        {
            return BuildEmptyResults(matches);
        }

        var generated = ParseResponse(response.Content);

        return matches.Select(match =>
        {
            var generated_value = generated.TryGetValue(
                match.TextSnippet.Trim(),
                out var val) ? val : string.Empty;

            return new ApplyToScopeValueResult(
                NodeKey: match.NodeKey,
                FragmentPath: match.FragmentPath,
                Value: generated_value);
        }).ToList();
    }

    private static Dictionary<string, string> ParseResponse(string content)
    {
        try
        {
            var cleaned = content.Trim();
            if (cleaned.StartsWith("```", StringComparison.Ordinal))
            {
                var start = cleaned.IndexOf('[', StringComparison.Ordinal);
                var end = cleaned.LastIndexOf(']');
                if (start >= 0 && end > start)
                    cleaned = cleaned[start..(end + 1)];
            }

            var items = JsonSerializer.Deserialize<List<GeneratedScopeValue>>(cleaned, JsonOptions);

            return items?.ToDictionary(
                item => item.TextSnippet.Trim(),
                item => item.Value,
                StringComparer.OrdinalIgnoreCase) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<ApplyToScopeValueResult> BuildEmptyResults(
        IReadOnlyList<PrototypeDomSearchMatch> matches)
    {
        return matches.Select(m => new ApplyToScopeValueResult(
            NodeKey: m.NodeKey,
            FragmentPath: m.FragmentPath,
            Value: string.Empty)).ToList();
    }
}

internal sealed record GeneratedScopeValue(string TextSnippet, string Value);
