using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Commands.AdvancePhase;
using Genesis.AI.Domain.Commands.SetPhase;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class AdvancePhaseCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IPromptService> _promptServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly AdvancePhaseCommandHandler _handler;

    public AdvancePhaseCommandHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _promptServiceMock = new Mock<IPromptService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _conversationRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _promptServiceMock
            .Setup(service => service.GetPhaseNames(It.IsAny<StageType>()))
            .Returns(["intro", "discovery", "review"]);

        _handler = new AdvancePhaseCommandHandler(_conversationRepositoryMock.Object, _promptServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFound()
    {
        var command = new AdvancePhaseCommand(Guid.NewGuid());
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_AlreadyAtFinalPhase_ReturnsValidationError()
    {
        var conversation = new Conversation(Guid.NewGuid(), 0, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new AdvancePhaseCommand(conversation.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.NotNull(result.ValidationError);
        Assert.Contains("final phase", result.ValidationError);
    }

    [Fact]
    public async Task Handle_ValidAdvance_IncrementsPhaseAndSaves()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _conversationRepositoryMock
            .Setup(repository => repository.GetStageTypeByStageIdAsync(conversation.StageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageType.RequirementsDiscovery);

        var command = new AdvancePhaseCommand(conversation.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.Null(result.ValidationError);
        Assert.Equal(1, result.Phase);
        Assert.Equal("discovery", result.PhaseName);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class SetPhaseCommandHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IPromptService> _promptServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly SetPhaseCommandHandler _handler;

    public SetPhaseCommandHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _promptServiceMock = new Mock<IPromptService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _conversationRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _promptServiceMock
            .Setup(service => service.GetPhaseNames(It.IsAny<StageType>()))
            .Returns(["intro", "discovery", "review"]);

        _handler = new SetPhaseCommandHandler(_conversationRepositoryMock.Object, _promptServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFound()
    {
        var command = new SetPhaseCommand(Guid.NewGuid(), 1);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_PhaseOutOfRange_ReturnsValidationError()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var command = new SetPhaseCommand(conversation.Id, 99);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.NotNull(result.ValidationError);
        Assert.Contains("out of range", result.ValidationError);
    }

    [Fact]
    public async Task Handle_ValidPhase_SetsPhaseAndSaves()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _conversationRepositoryMock
            .Setup(repository => repository.GetStageTypeByStageIdAsync(conversation.StageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageType.RequirementsDiscovery);

        var command = new SetPhaseCommand(conversation.Id, 2);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.Null(result.ValidationError);
        Assert.Equal(2, result.Phase);
        Assert.Equal("review", result.PhaseName);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
