using System.Globalization;
using System.Text;
using Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Services;

public sealed class BedrockKnowledgeService : IKnowledgeService
{
    private const int MaxTopN = 20;
    private const int TargetWordCount = 400;
    private const int HardCapChars = 6000;

    private readonly GenesisAiDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BedrockKnowledgeService> _logger;

    public BedrockKnowledgeService(
        GenesisAiDbContext dbContext,
        IEmbeddingService embeddingService,
        TimeProvider timeProvider,
        ILogger<BedrockKnowledgeService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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

        // Embed all chunks BEFORE opening the transaction — never hold a write lock
        // across Bedrock network calls.
        var embeddedChunks = new List<(string Chunk, float[] Embedding)>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var embedding = await _embeddingService.EmbedAsync(chunk, cancellationToken);
            embeddedChunks.Add((chunk, embedding));
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _dbContext.KnowledgeDocuments
                .Where(k => k.Namespace == knowledgeNamespace
                    && k.SourcePath == sourcePath
                    && (projectId == null ? k.ProjectId == null : k.ProjectId == projectId))
                .ExecuteDeleteAsync(cancellationToken);

            for (var i = 0; i < embeddedChunks.Count; i++)
            {
                var (chunk, embedding) = embeddedChunks[i];
                var chunkMetadata = new Dictionary<string, string>(metadata) { ["chunkIndex"] = i.ToString(CultureInfo.InvariantCulture) };
                var doc = KnowledgeDocument.Create(
                    knowledgeNamespace,
                    projectId,
                    sourcePath,
                    i,
                    chunk,
                    new Vector(embedding),
                    chunkMetadata,
                    _timeProvider);

                _dbContext.KnowledgeDocuments.Add(doc);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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

        // EF.Functions.CosineDistance translates to the pgvector <=> operator.
        // OrderBy before Select ensures the HNSW index is used for the ORDER BY.
        var results = await _dbContext.KnowledgeDocuments
            .Where(k => k.Namespace == knowledgeNamespace
                && (projectId == null ? k.ProjectId == null : k.ProjectId == projectId))
            .Select(k => new
            {
                k.Content,
                k.SourcePath,
                k.Metadata,
                Distance = k.Embedding.CosineDistance(queryVector)
            })
            .OrderBy(x => x.Distance)
            .Take(effectiveTopN)
            .ToListAsync(cancellationToken);

        return results
            .Select(x => new KnowledgeChunk(x.Content, x.SourcePath, 1.0 - x.Distance, x.Metadata))
            .ToList();
    }

    public async Task DeleteBySourcePathAsync(
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        // ponytail: ExecuteDeleteAsync executes directly as DELETE SQL, bypassing the
        // change tracker. SaveChangesAsync after it would be a no-op — omitted intentionally.
        await _dbContext.KnowledgeDocuments
            .Where(k => k.Namespace == knowledgeNamespace
                && k.SourcePath == sourcePath
                && (projectId == null ? k.ProjectId == null : k.ProjectId == projectId))
            .ExecuteDeleteAsync(cancellationToken);
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
