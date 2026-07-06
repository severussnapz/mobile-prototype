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
    private const int TargetWordCount = 400;
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

        foreach (var line in content.Split('\n'))
        {
            var trimmedLine = line.TrimStart();

            if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
            }

            var isHeading = !inCodeBlock
                && (trimmedLine.StartsWith("## ", StringComparison.Ordinal)
                    || trimmedLine.StartsWith("### ", StringComparison.Ordinal));

            // Headings always start a new chunk, regardless of current word count.
            if (isHeading && currentChunk.Length > 0)
            {
                FlushChunk(chunks, currentChunk.ToString());
                currentChunk.Clear();
                currentWordCount = 0;
            }

            currentChunk.AppendLine(line);
            currentWordCount += CountWords(line);

            // Flush at paragraph boundaries when the target word count is reached.
            if (!inCodeBlock && currentWordCount >= TargetWordCount && string.IsNullOrWhiteSpace(line))
            {
                FlushChunk(chunks, currentChunk.ToString());
                currentChunk.Clear();
                currentWordCount = 0;
            }
        }

        if (currentChunk.Length > 0)
        {
            FlushChunk(chunks, currentChunk.ToString());
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
