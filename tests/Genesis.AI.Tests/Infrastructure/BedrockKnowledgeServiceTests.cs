using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure;
using Genesis.AI.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

public class BedrockKnowledgeServiceTests
{
    private static GenesisAiDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<GenesisAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options,
            Mock.Of<IMediator>());

    // -----------------------------------------------------------------------
    // Unit tests — no Postgres required
    // -----------------------------------------------------------------------

    [Fact]
    public void QueryAsync_ClampsTopNToMaximumOfTwenty()
    {
        // ClampTopN is internal static — verifies the clamping contract directly.
        // Full QueryAsync behaviour (HNSW ordering, score mapping) needs Postgres.
        Assert.Equal(20, BedrockKnowledgeService.ClampTopN(100));
        Assert.Equal(20, BedrockKnowledgeService.ClampTopN(20));
        Assert.Equal(5, BedrockKnowledgeService.ClampTopN(5));
        Assert.Equal(1, BedrockKnowledgeService.ClampTopN(1));
    }

    [Fact]
    public async Task IndexDocumentAsync_WhenContentIsWhitespace_LogsWarningAndReturns()
    {
        var embeddingMock = new Mock<IEmbeddingService>();
        await using var context = CreateInMemoryContext();
        var sut = new BedrockKnowledgeService(
            context, embeddingMock.Object, TimeProvider.System,
            NullLogger<BedrockKnowledgeService>.Instance);

        await sut.IndexDocumentAsync(
            KnowledgeNamespace.GenesisTool, null, "tools/guide.md",
            "   ", [], CancellationToken.None);

        embeddingMock.Verify(
            e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IndexDocumentAsync_WhenEmbeddingFails_PropagatesExceptionAndSkipsTransaction()
    {
        // Embedding runs BEFORE BeginTransactionAsync — a Bedrock failure never
        // reaches the DB write path; no delete is committed.
        var content = string.Join(" ", Enumerable.Repeat("word", 400));
        var embeddingMock = new Mock<IEmbeddingService>();
        embeddingMock
            .Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Bedrock unavailable"));

        await using var context = CreateInMemoryContext();
        var sut = new BedrockKnowledgeService(
            context, embeddingMock.Object, TimeProvider.System,
            NullLogger<BedrockKnowledgeService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.IndexDocumentAsync(
                KnowledgeNamespace.GenesisTool, null, "test/path.md",
                content, [], CancellationToken.None));
    }

    [Fact]
    public void ChunkMarkdown_WhenContentHasParagraphBreaks_ProducesMultipleChunks()
    {
        // Paragraph 1 exceeds the 400-word target; the blank line between paragraphs
        // triggers the flush, producing at least two chunks.
        var paragraph1 = string.Join(" ", Enumerable.Repeat("word", 420));
        var content = $"{paragraph1}\n\nSecond paragraph here.";

        var chunks = BedrockKnowledgeService.ChunkMarkdown(content);

        Assert.True(chunks.Count >= 2, $"Expected >= 2 chunks but got {chunks.Count}");
    }

    [Fact]
    public void ChunkMarkdown_WhenChunkExceedsHardCap_ForceSplitsOnWordBoundary()
    {
        // ~7500 chars with no paragraph breaks; FlushChunk must split at word boundaries.
        var content = string.Join(" ", Enumerable.Repeat("word", 1500));

        var chunks = BedrockKnowledgeService.ChunkMarkdown(content);

        Assert.True(chunks.Count >= 2, $"Expected >= 2 chunks but got {chunks.Count}");
        Assert.All(chunks, chunk => Assert.True(chunk.Length <= 6000,
            $"Chunk exceeds 6000 chars: length={chunk.Length}"));
    }

    // -----------------------------------------------------------------------
    // Integration tests — require a real Postgres + pgvector instance.
    // Implement with Testcontainers.PostgreSql (already in Directory.Packages.props).
    // -----------------------------------------------------------------------

    [Fact(Skip = "Requires Postgres — implement with Testcontainers.PostgreSql")]
    public async Task IndexDocumentAsync_ChunksContent_AndEmbedsEachChunk()
    {
        // Verify chunk count and that EmbedAsync was called once per produced chunk.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires Postgres — implement with Testcontainers.PostgreSql")]
    public async Task IndexDocumentAsync_DeletesExistingChunks_BeforeInsertingNew()
    {
        // Index, then re-index the same sourcePath — old chunks must be replaced atomically.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires Postgres — implement with Testcontainers.PostgreSql")]
    public async Task QueryAsync_ReturnsChunksOrderedBySimilarity()
    {
        // Verify score = 1 - cosineDistance and results are ordered descending by similarity.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires Postgres — implement with Testcontainers.PostgreSql")]
    public async Task DeleteBySourcePathAsync_RemovesAllChunksForSourcePath()
    {
        // Insert chunks, delete by source path, verify the table is empty for that path.
        await Task.CompletedTask;
    }
}
