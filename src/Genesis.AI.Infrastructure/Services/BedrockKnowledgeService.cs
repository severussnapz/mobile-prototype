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
        foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
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
    internal static int ClampTopN(int topN) => Math.Min(topN, MaxTopN);

    internal static IReadOnlyList<string> ChunkMarkdown(string content)
    {
        var chunks = new List<string>();
        var currentChunk = new StringBuilder();
        var currentWordCount = 0;
        var inCodeBlock = false;

        // Breadcrumb tracking — maintain heading hierarchy
        var headingStack = new Dictionary<int, string>(); // level → heading text

        // Overlap tracking — last N words of previous chunk
        var previousChunkLines = new List<string>();
        const int OverlapWordTarget = 30; // ~20% of 150 word target
        var suppressBreadcrumbForCurrentChunk = false;

        string GetBreadcrumb()
        {
            if (headingStack.Count == 0) return string.Empty;
            return string.Join(" > ", headingStack
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value));
        }

        string StripMarkdownSyntax(string line)
        {
            // Strip leading # characters and whitespace
            return System.Text.RegularExpressions.Regex
                .Replace(line.TrimStart(), @"^#+\s*", string.Empty).Trim();
        }

        int GetHeadingLevel(string trimmedLine)
        {
            if (trimmedLine.StartsWith("### ", StringComparison.Ordinal)) return 3;
            if (trimmedLine.StartsWith("## ", StringComparison.Ordinal)) return 2;
            if (trimmedLine.StartsWith("# ", StringComparison.Ordinal)) return 1;
            return 0;
        }

        void FlushWithBreadcrumb()
        {
            var raw = currentChunk.ToString().Trim();
            if (string.IsNullOrWhiteSpace(raw)) return;

            var breadcrumb = GetBreadcrumb();
            var final = suppressBreadcrumbForCurrentChunk || string.IsNullOrEmpty(breadcrumb)
                ? raw
                : $"{breadcrumb}\n\n{raw}";

            FlushChunk(chunks, final);

            // Store lines for overlap into next chunk
            previousChunkLines = raw.Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
        }

        string GetOverlapPrefix()
        {
            if (previousChunkLines.Count == 0) return string.Empty;
            var overlapWords = string.Join("\n", previousChunkLines)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(OverlapWordTarget)
                .ToArray();

            return overlapWords.Length > 0
                ? string.Join(" ", overlapWords) + "\n\n"
                : string.Empty;
        }

        foreach (var line in content.Split('\n'))
        {
            var trimmedLine = line.TrimStart();

            if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
            }

            var headingLevel = !inCodeBlock ? GetHeadingLevel(trimmedLine) : 0;
            var isHeading = headingLevel > 0;

            if (isHeading)
            {
                // Flush current chunk before starting new section
                if (currentChunk.Length > 0)
                {
                    FlushWithBreadcrumb();
                    currentChunk.Clear();
                    currentWordCount = 0;
                }

                // Update heading stack — remove all headings at same or deeper level
                var keysToRemove = headingStack.Keys
                    .Where(k => k >= headingLevel)
                    .ToList();
                foreach (var key in keysToRemove)
                {
                    headingStack.Remove(key);
                }

                headingStack[headingLevel] = StripMarkdownSyntax(trimmedLine);

                // Start new chunk with overlap from previous chunk
                var overlap = GetOverlapPrefix();
                if (!string.IsNullOrEmpty(overlap))
                {
                    currentChunk.Append(overlap);
                    currentWordCount = CountWords(overlap);
                    suppressBreadcrumbForCurrentChunk = headingStack.Count == 1;
                }
                else
                {
                    suppressBreadcrumbForCurrentChunk = false;
                }
            }

            currentChunk.AppendLine(line);
            currentWordCount += CountWords(line);

            // Flush at paragraph boundaries when target word count reached
            if (!inCodeBlock && currentWordCount >= TargetWordCount
                && string.IsNullOrWhiteSpace(line))
            {
                FlushWithBreadcrumb();
                currentChunk.Clear();
                currentWordCount = 0;
                suppressBreadcrumbForCurrentChunk = false;
            }
        }

        if (currentChunk.Length > 0)
        {
            FlushWithBreadcrumb();
        }

        return chunks
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk))
            .Select(chunk => chunk.Trim())
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

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}
