using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

public class BedrockKnowledgeServiceTests
{
    private static readonly char[] WordSeparators = [' ', '\n', '\r', '\t'];

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

    [Fact]
    public void ChunkMarkdown_PreservesH1TitleInEveryChunk()
    {
        var content = "# Module 1: Requirements\n\n## Exercise 1\n\nContent here";

        var chunks = BedrockKnowledgeService.ChunkMarkdown(content);
        var exerciseChunk = chunks.Single(chunk => chunk.Contains("Exercise 1", StringComparison.Ordinal));

        Assert.StartsWith("Module 1: Requirements > Exercise 1", exerciseChunk, StringComparison.Ordinal);
    }

    [Fact]
    public void ChunkMarkdown_PreservesParentHeadingBreadcrumbInChildChunk()
    {
        var content = "## Parent Section\n\nParent content\n\n### Child Section\n\nChild content";

        var chunks = BedrockKnowledgeService.ChunkMarkdown(content);
        var childChunk = chunks.Single(chunk => chunk.Contains("Child Section", StringComparison.Ordinal));

        Assert.StartsWith("Parent Section > Child Section", childChunk, StringComparison.Ordinal);
    }

    [Fact]
    public void ChunkMarkdown_PreservesFullBreadcrumbPath_WhenThreeLevelsDeep()
    {
        var content = "# H1\n\n## H2\n\n### H3\n\nContent";

        var chunks = BedrockKnowledgeService.ChunkMarkdown(content);
        var h3Chunk = chunks.Single(chunk => chunk.Contains("H3", StringComparison.Ordinal));

        Assert.StartsWith("H1 > H2 > H3", h3Chunk, StringComparison.Ordinal);
    }

    [Fact]
    public void ChunkMarkdown_BreadcrumbStripsMarkdownSyntax()
    {
        var content = "## Exercise 2: Handle a GAP Response\n\nContent";

        var chunks = BedrockKnowledgeService.ChunkMarkdown(content);
        var chunk = chunks.Single();

        Assert.StartsWith("Exercise 2: Handle a GAP Response", chunk, StringComparison.Ordinal);
    }

    [Fact]
    public void ChunkMarkdown_ExistingBehaviourUnchanged_WhenNoHeadings()
    {
        var content = "This is plain paragraph content without headings.\n\nStill plain text in paragraph two.";

        var chunks = BedrockKnowledgeService.ChunkMarkdown(content);

        Assert.All(chunks, chunk => Assert.DoesNotContain(" > ", chunk, StringComparison.Ordinal));
        Assert.Single(chunks);
    }

    [Fact]
    public void ChunkMarkdown_IncludesOverlapFromPreviousChunk()
    {
        var sectionOne = string.Join(" ", Enumerable.Repeat("alpha", 420));
        var sectionTwo = string.Join(" ", Enumerable.Repeat("beta", 420));
        var content = $"## Section One\n\n{sectionOne}\n\n## Section Two\n\n{sectionTwo}";

        var chunks = BedrockKnowledgeService.ChunkMarkdown(content);

        Assert.True(chunks.Count >= 2, $"Expected at least 2 chunks, got {chunks.Count}");

        var firstChunkTailWords = chunks[0]
            .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries)
            .TakeLast(20)
            .ToArray();

        var secondChunkPrefix = string.Join(" ", chunks[1]
            .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Take(20));

        Assert.Contains(string.Join(" ", firstChunkTailWords), secondChunkPrefix, StringComparison.Ordinal);
    }

    [Fact]
    public void ChunkMarkdown_TargetWordCount_IsReducedTo150()
    {
        var section = string.Join(" ", Enumerable.Repeat("word", 160));
        var content = $"## Section\n\n{section}\n\nnext paragraph";

        var chunks = BedrockKnowledgeService.ChunkMarkdown(content);

        Assert.Equal(2, chunks.Count);
    }

    [Fact]
    public async Task HelpChatStreamService_QueriesGenesisTool_WithTopN3()
    {
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var conversation = HelpConversation.Create(Guid.NewGuid(), "user-1", TimeProvider.System);
        var helpConversationRepositoryMock = new Mock<IHelpConversationRepository>();
        helpConversationRepositoryMock.SetupGet(repository => repository.UnitOfWork)
            .Returns(unitOfWorkMock.Object);
        helpConversationRepositoryMock
            .Setup(repository => repository.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var knowledgeServiceMock = new Mock<IKnowledgeService>();
        knowledgeServiceMock
            .Setup(service => service.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var aiServiceMock = new Mock<IAiService>();
        aiServiceMock
            .Setup(service => service.StreamResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsAsyncEnumerable("ok"));

        var sut = new HelpChatStreamService(
            helpConversationRepositoryMock.Object,
            knowledgeServiceMock.Object,
            aiServiceMock.Object,
            TimeProvider.System,
            NullLogger<HelpChatStreamService>.Instance);

        await foreach (var _ in sut.StreamAsync(
                           "how do tools work",
                           Guid.NewGuid(),
                           Guid.NewGuid(),
                           "user-1",
                           CancellationToken.None))
        {
        }

        knowledgeServiceMock.Verify(service => service.QueryAsync(
                It.IsAny<string>(),
                KnowledgeNamespace.GenesisTool,
                null,
                3,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HelpChatStreamService_QueriesProjectArtefact_WithTopN5()
    {
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var projectId = Guid.NewGuid();
        var conversation = HelpConversation.Create(projectId, "user-1", TimeProvider.System);
        var helpConversationRepositoryMock = new Mock<IHelpConversationRepository>();
        helpConversationRepositoryMock.SetupGet(repository => repository.UnitOfWork)
            .Returns(unitOfWorkMock.Object);
        helpConversationRepositoryMock
            .Setup(repository => repository.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var knowledgeServiceMock = new Mock<IKnowledgeService>();
        knowledgeServiceMock
            .Setup(service => service.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var aiServiceMock = new Mock<IAiService>();
        aiServiceMock
            .Setup(service => service.StreamResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsAsyncEnumerable("ok"));

        var sut = new HelpChatStreamService(
            helpConversationRepositoryMock.Object,
            knowledgeServiceMock.Object,
            aiServiceMock.Object,
            TimeProvider.System,
            NullLogger<HelpChatStreamService>.Instance);

        await foreach (var _ in sut.StreamAsync(
                           "what changed in this project",
                           projectId,
                           Guid.NewGuid(),
                           "user-1",
                           CancellationToken.None))
        {
        }

        knowledgeServiceMock.Verify(service => service.QueryAsync(
                It.IsAny<string>(),
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static async IAsyncEnumerable<string> AsAsyncEnumerable(params string[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }
}
