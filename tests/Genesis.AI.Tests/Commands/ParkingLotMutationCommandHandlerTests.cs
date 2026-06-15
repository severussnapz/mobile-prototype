using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Commands.DeferParkingLotItem;
using Genesis.AI.Domain.Commands.DeleteParkingLotItem;
using Genesis.AI.Domain.Commands.ResolveParkingLotItem;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class ResolveParkingLotItemCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly ResolveParkingLotItemCommandHandler _handler;

    public ResolveParkingLotItemCommandHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _conversationRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ResolveParkingLotItemCommandHandler(_conversationRepositoryMock.Object, _timeProvider);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFound()
    {
        var command = new ResolveParkingLotItemCommand(Guid.NewGuid(), Guid.NewGuid(), "Done by manual review");
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsNotFound()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new ResolveParkingLotItemCommand(conversation.Id, Guid.NewGuid(), "Nothing to resolve");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_ValidItem_ResolvesAndSaves()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        var item = conversation.AddParkingLotItem("Investigate auth", ParkingLotPriority.High, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new ResolveParkingLotItemCommand(conversation.Id, item.Id, "Reviewed and implemented");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.NotNull(result.Item);
        Assert.Equal(ParkingLotStatus.Resolved, result.Item.Status);
        Assert.Equal("Reviewed and implemented", result.Item.ClosureDecision);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DeferParkingLotItemCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly DeferParkingLotItemCommandHandler _handler;

    public DeferParkingLotItemCommandHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _conversationRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeferParkingLotItemCommandHandler(_conversationRepositoryMock.Object, _timeProvider);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFound()
    {
        var command = new DeferParkingLotItemCommand(Guid.NewGuid(), Guid.NewGuid(), "Waiting for dependency");
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsNotFound()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new DeferParkingLotItemCommand(conversation.Id, Guid.NewGuid(), "Deferred due to priority");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_ValidItem_DefersAndSaves()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        var item = conversation.AddParkingLotItem("Review later", ParkingLotPriority.Medium, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new DeferParkingLotItemCommand(conversation.Id, item.Id, "Deferred pending SME confirmation");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.NotNull(result.Item);
        Assert.Equal(ParkingLotStatus.Deferred, result.Item.Status);
        Assert.Equal("Deferred pending SME confirmation", result.Item.ClosureDecision);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DeleteParkingLotItemCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly TimeProvider _timeProvider;
    private readonly DeleteParkingLotItemCommandHandler _handler;

    public DeleteParkingLotItemCommandHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _timeProvider = TimeProvider.System;

        _handler = new DeleteParkingLotItemCommandHandler(_conversationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsFalse()
    {
        var command = new DeleteParkingLotItemCommand(Guid.NewGuid(), Guid.NewGuid());
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFalse()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new DeleteParkingLotItemCommand(conversation.Id, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_ValidItem_RemovesAndReturnsTrue()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        var item = conversation.AddParkingLotItem("Remove me", ParkingLotPriority.Critical, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdWithParkingLotAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new DeleteParkingLotItemCommand(conversation.Id, item.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        _conversationRepositoryMock.Verify(
            repository => repository.RemoveParkingLotItemAsync(item, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
