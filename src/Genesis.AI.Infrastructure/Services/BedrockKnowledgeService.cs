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
        foreach (var (chunk, index) in chunks.Select((chunkText, chunkIndex) => (chunkText, chunkIndex)))
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
        var chunkBuilder = new MarkdownChunkBuilder(content);
        return chunkBuilder.Build();
    }

    private sealed class MarkdownChunkBuilder
    {
        private const int OverlapWordTarget = 30;

        private readonly string _content;
        private readonly List<string> _chunks = [];
        private readonly StringBuilder _currentChunk = new();
        private readonly Dictionary<int, string> _headingStack = [];

        private List<string> _previousChunkLines = [];
        private int _currentWordCount;
        private bool _inCodeBlock;
        private bool _suppressBreadcrumbForCurrentChunk;

        public MarkdownChunkBuilder(string content)
        {
            _content = content;
        }

        public List<string> Build()
        {
            foreach (var line in _content.Split('\n'))
            {
                ProcessLine(line);
            }

            if (_currentChunk.Length > 0)
            {
                FlushWithBreadcrumb();
            }

            return _chunks
                .Where(chunk => !string.IsNullOrWhiteSpace(chunk))
                .Select(chunk => chunk.Trim())
                .ToList();
        }

        private void ProcessLine(string line)
        {
            var trimmedLine = line.TrimStart();
            ToggleCodeBlockIfNeeded(trimmedLine);

            var headingLevel = !_inCodeBlock ? GetHeadingLevel(trimmedLine) : 0;
            if (headingLevel > 0)
            {
                HandleHeading(trimmedLine, headingLevel);
            }

            _currentChunk.AppendLine(line);
            _currentWordCount += CountWords(line);

            if (!_inCodeBlock && _currentWordCount >= TargetWordCount && string.IsNullOrWhiteSpace(line))
            {
                FlushWithBreadcrumb();
                ResetCurrentChunk();
            }
        }

        private void ToggleCodeBlockIfNeeded(string trimmedLine)
        {
            if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
            {
                _inCodeBlock = !_inCodeBlock;
            }
        }

        private void HandleHeading(string trimmedLine, int headingLevel)
        {
            if (_currentChunk.Length > 0)
            {
                FlushWithBreadcrumb();
                ResetCurrentChunk();
            }

            var keysToRemove = _headingStack.Keys
                .Where(headingLevelKey => headingLevelKey >= headingLevel)
                .ToList();
            foreach (var key in keysToRemove)
            {
                _headingStack.Remove(key);
            }

            _headingStack[headingLevel] = StripMarkdownSyntax(trimmedLine);
            ApplyOverlapPrefix();
        }

        private void ApplyOverlapPrefix()
        {
            var overlap = GetOverlapPrefix();
            if (string.IsNullOrEmpty(overlap))
            {
                _suppressBreadcrumbForCurrentChunk = false;
                return;
            }

            _currentChunk.Append(overlap);
            _currentWordCount = CountWords(overlap);
            _suppressBreadcrumbForCurrentChunk = _headingStack.Count == 1;
        }

        private string GetOverlapPrefix()
        {
            if (_previousChunkLines.Count == 0)
            {
                return string.Empty;
            }

            var overlapWords = string.Join("\n", _previousChunkLines)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(OverlapWordTarget)
                .ToArray();

            return overlapWords.Length > 0
                ? string.Join(" ", overlapWords) + "\n\n"
                : string.Empty;
        }

        private void FlushWithBreadcrumb()
        {
            var raw = _currentChunk.ToString().Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            var breadcrumb = GetBreadcrumb();
            var final = _suppressBreadcrumbForCurrentChunk || string.IsNullOrEmpty(breadcrumb)
                ? raw
                : $"{breadcrumb}\n\n{raw}";

            FlushChunk(_chunks, final);

            _previousChunkLines = raw.Split('\n')
                .Where(lineValue => !string.IsNullOrWhiteSpace(lineValue))
                .ToList();
        }

        private string GetBreadcrumb()
        {
            if (_headingStack.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" > ", _headingStack
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

        private void ResetCurrentChunk()
        {
            _currentChunk.Clear();
            _currentWordCount = 0;
            _suppressBreadcrumbForCurrentChunk = false;
        }
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

    private static int CountWords(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
