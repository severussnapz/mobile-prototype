using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Commands.AddParkingLotItem;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class AddParkingLotItemCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly AddParkingLotItemCommandHandler _handler;

    public AddParkingLotItemCommandHandlerTests()
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

        _handler = new AddParkingLotItemCommandHandler(
            _conversationRepositoryMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFound()
    {
        var conversationId = Guid.NewGuid();
        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var command = new AddParkingLotItemCommand(conversationId, "Content", "high");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_InvalidPriority_ReturnsValidationError()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new AddParkingLotItemCommand(conversation.Id, "Content", "invalid_priority");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.NotNull(result.ValidationError);
        Assert.Contains("Invalid priority", result.ValidationError);
    }

    [Theory]
    [InlineData("critical")]
    [InlineData("high")]
    [InlineData("medium")]
    [InlineData("Critical")]
    [InlineData("HIGH")]
    public async Task Handle_ValidPriority_AddsItem(string priority)
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new AddParkingLotItemCommand(conversation.Id, "Investigate auth flow", priority);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.Null(result.ValidationError);
        Assert.NotNull(result.Item);
        Assert.Equal("Investigate auth flow", result.Item.Content);
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesChanges()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new AddParkingLotItemCommand(conversation.Id, "Content", "high");

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ItemAddedToConversation()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new AddParkingLotItemCommand(conversation.Id, "Review security model", "critical");

        await _handler.Handle(command, CancellationToken.None);

        Assert.Single(conversation.ParkingLotItems);
        Assert.Equal("Review security model", conversation.ParkingLotItems.First().Content);
        Assert.Equal(ParkingLotPriority.Critical, conversation.ParkingLotItems.First().Priority);
    }
}
