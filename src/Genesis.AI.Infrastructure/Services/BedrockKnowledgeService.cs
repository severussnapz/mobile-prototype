using System.Globalization;
using System.Text;
using Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Genesis.AI.Infrastructure.Services;

public sealed class BedrockKnowledgeService : IKnowledgeService
{
    private const int MaxTopN = 20;
    private const int TargetWordCount = 150;
    private const int HardCapChars = 6000;

    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BedrockKnowledgeService> _logger;

    public BedrockKnowledgeService(
        IKnowledgeRepository knowledgeRepository,
        IEmbeddingService embeddingService,
        TimeProvider timeProvider,
        ILogger<BedrockKnowledgeService> logger)
    {
        _knowledgeRepository = knowledgeRepository ?? throw new ArgumentNullException(nameof(knowledgeRepository));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task IndexDocumentAsync(
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        string sourcePath,
        string content,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning(
                "IndexDocumentAsync called with empty content for {SourcePath}. Skipping.",
                sourcePath);
            return;
        }

        var chunks = ChunkMarkdown(content)
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk))
            .ToList();

        if (chunks.Count == 0)
        {
            _logger.LogWarning(
                "No indexable chunks produced for {SourcePath}. Skipping.",
                sourcePath);
            return;
        }

        // Embed all chunks BEFORE passing to repository — never hold a write lock
        // across Bedrock network calls.
        var documents = new List<KnowledgeDocument>(chunks.Count);
        foreach (var (chunk, index) in chunks.Select((chunkContent, chunkIndex) => (chunkContent, chunkIndex)))
        {
            var embedding = await _embeddingService.EmbedAsync(chunk, cancellationToken);
            var chunkMetadata = new Dictionary<string, string>(metadata)
            {
                ["chunkIndex"] = index.ToString(CultureInfo.InvariantCulture)
            };
            var doc = KnowledgeDocument.Create(
                knowledgeNamespace,
                projectId,
                sourcePath,
                index,
                chunk,
                new Vector(embedding),
                chunkMetadata,
                _timeProvider);

            documents.Add(doc);
        }

        await _knowledgeRepository.IndexAsync(
            documents, knowledgeNamespace, projectId, sourcePath, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> QueryAsync(
        string query,
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        int topN,
        CancellationToken cancellationToken)
    {
        var effectiveTopN = ClampTopN(topN);
        var queryEmbedding = await _embeddingService.EmbedAsync(query, cancellationToken);
        var queryVector = new Vector(queryEmbedding);

        return await _knowledgeRepository.QuerySimilarAsync(
            queryVector, knowledgeNamespace, projectId, effectiveTopN, cancellationToken);
    }

    public async Task DeleteBySourcePathAsync(
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        await _knowledgeRepository.DeleteBySourcePathAsync(
            knowledgeNamespace, projectId, sourcePath, cancellationToken);
    }

    // internal for direct unit testing via InternalsVisibleTo
    internal static int ClampTopN(int topN)
    {
        return Math.Min(topN, MaxTopN);
    }

    internal static IReadOnlyList<string> ChunkMarkdown(string content)
    {
        var chunkingState = new MarkdownChunkingState();

        foreach (var line in content.Split('\n'))
        {
            ProcessMarkdownLine(chunkingState, line);
        }

        if (chunkingState.CurrentChunk.Length > 0)
        {
            FlushWithBreadcrumb(chunkingState);
        }

        return chunkingState.Chunks
            .Where(chunkContent => !string.IsNullOrWhiteSpace(chunkContent))
            .Select(chunkContent => chunkContent.Trim())
            .ToList();
    }

    private static void FlushChunk(List<string> chunks, string chunk)
    {
        var trimmed = chunk.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        if (trimmed.Length <= HardCapChars)
        {
            chunks.Add(trimmed);
            return;
        }

        // Force-split on word boundaries when the chunk exceeds the hard cap.
        var remaining = trimmed;
        while (remaining.Length > HardCapChars)
        {
            var splitAt = HardCapChars;
            while (splitAt > 0 && !char.IsWhiteSpace(remaining[splitAt]))
            {
                splitAt--;
            }

            if (splitAt == 0)
            {
                splitAt = HardCapChars; // No whitespace boundary — hard cut.
            }

            chunks.Add(remaining[..splitAt].Trim());
            remaining = remaining[splitAt..].TrimStart();
        }

        if (!string.IsNullOrWhiteSpace(remaining))
        {
            chunks.Add(remaining.Trim());
        }
    }

    private static void ProcessMarkdownLine(MarkdownChunkingState chunkingState, string line)
    {
        var trimmedLine = line.TrimStart();

        if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
        {
            chunkingState.InCodeBlock = !chunkingState.InCodeBlock;
        }

        var headingLevel = chunkingState.InCodeBlock ? 0 : GetHeadingLevel(trimmedLine);
        if (headingLevel > 0)
        {
            StartHeadingSection(chunkingState, trimmedLine, headingLevel);
        }

        chunkingState.CurrentChunk.AppendLine(line);
        chunkingState.CurrentWordCount += CountWords(line);

        if (!chunkingState.InCodeBlock
            && chunkingState.CurrentWordCount >= TargetWordCount
            && string.IsNullOrWhiteSpace(line))
        {
            FlushWithBreadcrumb(chunkingState);
            ResetCurrentChunk(chunkingState);
        }
    }

    private static void StartHeadingSection(MarkdownChunkingState chunkingState, string trimmedLine, int headingLevel)
    {
        if (chunkingState.CurrentChunk.Length > 0)
        {
            FlushWithBreadcrumb(chunkingState);
            ResetCurrentChunk(chunkingState);
        }

        var headingLevelsToRemove = chunkingState.HeadingStack.Keys
            .Where(headingLevelKey => headingLevelKey >= headingLevel)
            .ToList();

        foreach (var headingLevelKey in headingLevelsToRemove)
        {
            chunkingState.HeadingStack.Remove(headingLevelKey);
        }

        chunkingState.HeadingStack[headingLevel] = StripMarkdownSyntax(trimmedLine);

        var overlap = GetOverlapPrefix(chunkingState);
        if (!string.IsNullOrEmpty(overlap))
        {
            chunkingState.CurrentChunk.Append(overlap);
            chunkingState.CurrentWordCount = CountWords(overlap);
            chunkingState.SuppressBreadcrumbForCurrentChunk = chunkingState.HeadingStack.Count == 1;
            return;
        }

        chunkingState.SuppressBreadcrumbForCurrentChunk = false;
    }

    private static void FlushWithBreadcrumb(MarkdownChunkingState chunkingState)
    {
        var raw = chunkingState.CurrentChunk.ToString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var breadcrumb = GetBreadcrumb(chunkingState.HeadingStack);
        var final = chunkingState.SuppressBreadcrumbForCurrentChunk || string.IsNullOrEmpty(breadcrumb)
            ? raw
            : $"{breadcrumb}\n\n{raw}";

        FlushChunk(chunkingState.Chunks, final);
        chunkingState.PreviousChunkLines = raw.Split('\n')
            .Where(chunkLine => !string.IsNullOrWhiteSpace(chunkLine))
            .ToList();
    }

    private static void ResetCurrentChunk(MarkdownChunkingState chunkingState)
    {
        chunkingState.CurrentChunk.Clear();
        chunkingState.CurrentWordCount = 0;
        chunkingState.SuppressBreadcrumbForCurrentChunk = false;
    }

    private static string GetBreadcrumb(Dictionary<int, string> headingStack)
    {
        if (headingStack.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" > ", headingStack
            .OrderBy(heading => heading.Key)
            .Select(heading => heading.Value));
    }

    private static string StripMarkdownSyntax(string line)
    {
        return System.Text.RegularExpressions.Regex
            .Replace(line.TrimStart(), @"^#+\s*", string.Empty)
            .Trim();
    }

    private static int GetHeadingLevel(string trimmedLine)
    {
        if (trimmedLine.StartsWith("### ", StringComparison.Ordinal)) return 3;
        if (trimmedLine.StartsWith("## ", StringComparison.Ordinal)) return 2;
        if (trimmedLine.StartsWith("# ", StringComparison.Ordinal)) return 1;
        return 0;
    }

    private static string GetOverlapPrefix(MarkdownChunkingState chunkingState)
    {
        const int overlapWordTarget = 30;

        if (chunkingState.PreviousChunkLines.Count == 0)
        {
            return string.Empty;
        }

        var overlapWords = string.Join("\n", chunkingState.PreviousChunkLines)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .TakeLast(overlapWordTarget)
            .ToArray();

        if (overlapWords.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(" ", overlapWords) + "\n\n";
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private sealed class MarkdownChunkingState
    {
        public List<string> Chunks { get; } = [];
        public StringBuilder CurrentChunk { get; } = new();
        public int CurrentWordCount { get; set; }
        public bool InCodeBlock { get; set; }
        public Dictionary<int, string> HeadingStack { get; } = [];
        public List<string> PreviousChunkLines { get; set; } = [];
        public bool SuppressBreadcrumbForCurrentChunk { get; set; }
    }
}
