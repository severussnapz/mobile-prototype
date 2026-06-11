using Genesis.AI.Domain;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

public sealed class RoutingContextServiceTests
{
    private readonly Mock<IConversationRepository> _repositoryMock = new();
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public RoutingContextServiceTests()
    {
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private RoutingContextService CreateSut() =>
        new(_repositoryMock.Object, _artefactRepositoryMock.Object);

    private Conversation CreateConversation(Guid stageId, int totalPhases = 13, int userMessageCount = 0)
    {
        var conversation = new Conversation(stageId, totalPhases, _timeProvider);
        for (var index = 0; index < userMessageCount; index++)
        {
            conversation.AddMessage(MessageRole.User, "Hello", null, _timeProvider);
        }
        return conversation;
    }

    // ── Conversation not found ────────────────────────────────────────────────

    [Fact]
    public async Task BuildRoutingContextAsync_ConversationNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        _repositoryMock.Setup(repository => repository.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);
        _repositoryMock.Setup(repository => repository.GetStageTypeByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageType.Architecture);

        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BuildRoutingContextAsync(conversationId, CancellationToken.None));
    }

    // ── Stage type not found ──────────────────────────────────────────────────

    [Fact]
    public async Task BuildRoutingContextAsync_StageTypeNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var conversation = CreateConversation(Guid.NewGuid());
        _repositoryMock.Setup(repository => repository.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _repositoryMock.Setup(repository => repository.GetStageTypeByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageType?)null);

        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BuildRoutingContextAsync(conversationId, CancellationToken.None));
    }

    // ── StageType propagated ──────────────────────────────────────────────────

    [Theory]
    [InlineData(StageType.Architecture)]
    [InlineData(StageType.Design)]
    [InlineData(StageType.ClinicalSafety)]
    public async Task BuildRoutingContextAsync_ReturnsCorrectStageType(StageType stageType)
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var conversation = CreateConversation(Guid.NewGuid());
        _repositoryMock.Setup(repository => repository.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _repositoryMock.Setup(repository => repository.GetStageTypeByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stageType);

        var sut = CreateSut();

        // Act
        var result = await sut.BuildRoutingContextAsync(conversationId, CancellationToken.None);

        // Assert
        Assert.Equal(stageType, result.StageType);
    }

    // ── CurrentPhase propagated ───────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(12)]
    public async Task BuildRoutingContextAsync_ReturnsCurrentPhaseFromConversation(int expectedPhase)
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var conversation = CreateConversation(Guid.NewGuid());
        for (var index = 0; index < expectedPhase; index++)
        {
            conversation.AdvancePhase($"phase_{index}");
        }
        _repositoryMock.Setup(repository => repository.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _repositoryMock.Setup(repository => repository.GetStageTypeByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageType.Architecture);

        var sut = CreateSut();

        // Act
        var result = await sut.BuildRoutingContextAsync(conversationId, CancellationToken.None);

        // Assert
        Assert.Equal(expectedPhase, result.CurrentPhase);
    }

    // ── IsFirstMessage ────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildRoutingContextAsync_NoMessages_IsFirstMessageTrue()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var conversation = CreateConversation(Guid.NewGuid(), userMessageCount: 0);
        _repositoryMock.Setup(repository => repository.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _repositoryMock.Setup(repository => repository.GetStageTypeByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageType.Architecture);

        var sut = CreateSut();

        // Act
        var result = await sut.BuildRoutingContextAsync(conversationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFirstMessage);
    }

    [Fact]
    public async Task BuildRoutingContextAsync_OneUserMessage_IsFirstMessageTrue()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var conversation = CreateConversation(Guid.NewGuid(), userMessageCount: 1);
        _repositoryMock.Setup(repository => repository.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _repositoryMock.Setup(repository => repository.GetStageTypeByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageType.Architecture);

        var sut = CreateSut();

        // Act
        var result = await sut.BuildRoutingContextAsync(conversationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFirstMessage);
    }

    [Fact]
    public async Task BuildRoutingContextAsync_TwoUserMessages_IsFirstMessageFalse()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var conversation = CreateConversation(Guid.NewGuid(), userMessageCount: 2);
        _repositoryMock.Setup(repository => repository.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _repositoryMock.Setup(repository => repository.GetStageTypeByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageType.Architecture);

        var sut = CreateSut();

        // Act
        var result = await sut.BuildRoutingContextAsync(conversationId, CancellationToken.None);

        // Assert
        Assert.False(result.IsFirstMessage);
    }

    // ── Both queries run ──────────────────────────────────────────────────────

    [Fact]
    public async Task BuildRoutingContextAsync_ValidInputs_CallsBothRepositoryMethods()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var conversation = CreateConversation(Guid.NewGuid());
        _repositoryMock.Setup(repository => repository.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _repositoryMock.Setup(repository => repository.GetStageTypeByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageType.Architecture);

        var sut = CreateSut();

        // Act
        await sut.BuildRoutingContextAsync(conversationId, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(repository => repository.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(repository => repository.GetStageTypeByConversationIdAsync(conversationId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
