using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

public class BedrockKnowledgeServiceTests
{
    // -----------------------------------------------------------------------
    // Unit tests — no repository or database required
    // -----------------------------------------------------------------------

    [Fact]
    public void QueryAsync_ClampsTopNToMaximumOfTwenty()
    {
        // ClampTopN is internal static — verifies the clamping contract directly.
        // Full QueryAsync behaviour (HNSW ordering, score mapping) needs integration tests.
        Assert.Equal(20, BedrockKnowledgeService.ClampTopN(100));
        Assert.Equal(20, BedrockKnowledgeService.ClampTopN(20));
        Assert.Equal(5, BedrockKnowledgeService.ClampTopN(5));
        Assert.Equal(1, BedrockKnowledgeService.ClampTopN(1));
    }

    [Fact]
    public async Task IndexDocumentAsync_WhenContentIsWhitespace_LogsWarningAndReturns()
    {
        var repositoryMock = new Mock<IKnowledgeRepository>();
        var embeddingMock = new Mock<IEmbeddingService>();
        
        var sut = new BedrockKnowledgeService(
            repositoryMock.Object,
            embeddingMock.Object,
            TimeProvider.System,
            NullLogger<BedrockKnowledgeService>.Instance);

        await sut.IndexDocumentAsync(
            KnowledgeNamespace.GenesisTool, null, "tools/guide.md",
            "   ", [], CancellationToken.None);

        embeddingMock.Verify(
            e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repositoryMock.Verify(
            r => r.IndexAsync(It.IsAny<IReadOnlyList<Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate.KnowledgeDocument>>(),
                It.IsAny<KnowledgeNamespace>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IndexDocumentAsync_WhenEmbeddingFails_PropagatesExceptionAndSkipsRepository()
    {
        // Embedding runs BEFORE calling repository — a Bedrock failure never
        // reaches the repository call.
        var content = string.Join(" ", Enumerable.Repeat("word", 400));
        var repositoryMock = new Mock<IKnowledgeRepository>();
        var embeddingMock = new Mock<IEmbeddingService>();
        embeddingMock
            .Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Bedrock unavailable"));

        var sut = new BedrockKnowledgeService(
            repositoryMock.Object,
            embeddingMock.Object,
            TimeProvider.System,
            NullLogger<BedrockKnowledgeService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.IndexDocumentAsync(
                KnowledgeNamespace.GenesisTool, null, "test/path.md",
                content, [], CancellationToken.None));

        repositoryMock.Verify(
            r => r.IndexAsync(It.IsAny<IReadOnlyList<Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate.KnowledgeDocument>>(),
                It.IsAny<KnowledgeNamespace>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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
}
