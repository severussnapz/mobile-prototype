using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Commands.SendMessage;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class SendMessageCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _conversationRepositoryMock
            .Setup(r => r.UnitOfWork)
            .Returns(_unitOfWorkMock.Object);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new SendMessageCommandHandler(
            _conversationRepositoryMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsMessageId()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(r => r.GetByIdWithMessagesAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new SendMessageCommand(conversation.Id, "Hello AI", "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsMessageToConversation()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(r => r.GetByIdWithMessagesAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new SendMessageCommand(conversation.Id, "Hello AI", "user-1");

        await _handler.Handle(command, CancellationToken.None);

        Assert.Single(conversation.Messages);
        Assert.Equal("Hello AI", conversation.Messages.First().Content);
        Assert.Equal(MessageRole.User, conversation.Messages.First().Role);
    }

    [Fact]
    public async Task Handle_ValidCommand_IncrementsMessageCount()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(r => r.GetByIdWithMessagesAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new SendMessageCommand(conversation.Id, "Hello AI", "user-1");

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, conversation.MessageCount);
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesChanges()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(r => r.GetByIdWithMessagesAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new SendMessageCommand(conversation.Id, "Hello AI", "user-1");

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ThrowsInvalidOperationException()
    {
        var conversationId = Guid.NewGuid();
        _conversationRepositoryMock
            .Setup(r => r.GetByIdWithMessagesAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var command = new SendMessageCommand(conversationId, "Hello", "user-1");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserMessage_IncrementsQuestionsAsked()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(r => r.GetByIdWithMessagesAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new SendMessageCommand(conversation.Id, "What about security?", "user-1");

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, conversation.QuestionsAsked);
    }
}
