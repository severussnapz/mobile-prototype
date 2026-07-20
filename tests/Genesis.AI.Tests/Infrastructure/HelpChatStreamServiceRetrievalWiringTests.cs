using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

// RED: HelpChatStreamService.StreamAsync currently passes the raw current message
// straight to IKnowledgeService.QueryAsync for both namespaces. These tests pin the
// desired behaviour — the retrieval query must be "{priorTurnSummary}: {currentMessage}",
// built via HelpChatStreamService.BuildRetrievalQuery (already implemented and unit
// tested in isolation, but never wired into StreamAsync).
public sealed class HelpChatStreamServiceRetrievalWiringTests
{
    [Fact]
    public async Task StreamAsync_GenesisToolQuery_UsesPriorUserMessagePrefixedQuery()
    {
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var timeProvider = new FakeTimeProvider();
        var conversation = HelpConversation.Create(Guid.NewGuid(), "user-1", TimeProvider.System);
        conversation.AddMessage("user", "Artefact Scope Restructure — Solution Design", timeProvider);
        conversation.AddMessage("assistant", "That is correct.", timeProvider);

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
                           "why are we doing it",
                           Guid.NewGuid(),
                           Guid.NewGuid(),
                           "user-1",
                           CancellationToken.None))
        {
        }

        knowledgeServiceMock.Verify(service => service.QueryAsync(
                "Artefact Scope Restructure — Solution Design: why are we doing it",
                KnowledgeNamespace.GenesisTool,
                null,
                3,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamAsync_ProjectArtefactQuery_UsesPriorUserMessagePrefixedQuery()
    {
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var timeProvider = new FakeTimeProvider();
        var projectId = Guid.NewGuid();
        var conversation = HelpConversation.Create(projectId, "user-1", TimeProvider.System);
        conversation.AddMessage("user", "first question", timeProvider);
        conversation.AddMessage("assistant", "a1", timeProvider);
        conversation.AddMessage("user", "second question", timeProvider);
        conversation.AddMessage("assistant", "a2", timeProvider);

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
                           "and why",
                           projectId,
                           Guid.NewGuid(),
                           "user-1",
                           CancellationToken.None))
        {
        }

        knowledgeServiceMock.Verify(service => service.QueryAsync(
                "second question: and why",
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamAsync_BothNamespaces_ReceiveIdenticalRetrievalQuery()
    {
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var timeProvider = new FakeTimeProvider();
        var projectId = Guid.NewGuid();
        var conversation = HelpConversation.Create(projectId, "user-1", TimeProvider.System);
        conversation.AddMessage("user", "prior turn", timeProvider);

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
                           "current turn",
                           projectId,
                           Guid.NewGuid(),
                           "user-1",
                           CancellationToken.None))
        {
        }

        knowledgeServiceMock.Verify(service => service.QueryAsync(
                "prior turn: current turn",
                KnowledgeNamespace.GenesisTool,
                null,
                3,
                It.IsAny<CancellationToken>()),
            Times.Once);

        knowledgeServiceMock.Verify(service => service.QueryAsync(
                "prior turn: current turn",
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamAsync_NoPriorMessages_RetrievalQueryIsCurrentMessageAlone()
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
                           "how does P06 work",
                           Guid.NewGuid(),
                           Guid.NewGuid(),
                           "user-1",
                           CancellationToken.None))
        {
        }

        knowledgeServiceMock.Verify(service => service.QueryAsync(
                "how does P06 work",
                KnowledgeNamespace.GenesisTool,
                null,
                3,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamAsync_LogsRetrievalQuery_WithConstructedQueryString()
    {
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var timeProvider = new FakeTimeProvider();
        var conversation = HelpConversation.Create(Guid.NewGuid(), "user-1", TimeProvider.System);
        conversation.AddMessage("user", "prior turn", timeProvider);

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

        var loggerMock = new Mock<ILogger<HelpChatStreamService>>();

        var sut = new HelpChatStreamService(
            helpConversationRepositoryMock.Object,
            knowledgeServiceMock.Object,
            aiServiceMock.Object,
            TimeProvider.System,
            loggerMock.Object);

        await foreach (var _ in sut.StreamAsync(
                           "current turn",
                           Guid.NewGuid(),
                           Guid.NewGuid(),
                           "user-1",
                           CancellationToken.None))
        {
        }

        loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("prior turn: current turn", StringComparison.Ordinal)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
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
