using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.ReopenStageForAmendment;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Domain;

public class ReopenStageForAmendmentCommandTests
{
    [Fact]
    public async Task Handle_WhenStageComplete_ReopensStageAndCreatesConversation()
    {
        var project = new Project("TEST", "Test Project", null, "TS001",
            ComplianceDomain.Generic, "idris.issa", TimeProvider.System);

        var stage = project.PipelineStages.First(s => s.StageType == StageType.Pxd);
        var stageId = stage.Id;

        var projectRepositoryMock = new Mock<IProjectRepository>();
        var conversationRepositoryMock = new Mock<IConversationRepository>();
        var promptServiceMock = new Mock<IPromptService>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();

        projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        conversationRepositoryMock
            .Setup(r => r.GetByStageAndRequirementIdAsync(
                stageId, "REQ-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);
        conversationRepositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        projectRepositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        promptServiceMock.Setup(s => s.GetTotalPhases(StageType.Pxd)).Returns(3);

        Conversation? savedConversation = null;
        conversationRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()))
            .Callback<Conversation, CancellationToken>((c, _) => savedConversation = c);

        var handler = new ReopenStageForAmendmentCommandHandler(
            projectRepositoryMock.Object,
            conversationRepositoryMock.Object,
            promptServiceMock.Object,
            TimeProvider.System);

        var result = await handler.Handle(
            new ReopenStageForAmendmentCommand(stageId, "REQ-001"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedConversation);
        Assert.Equal("REQ-001", savedConversation!.RequirementId);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ReturnsFailure()
    {
        var stageId = Guid.NewGuid();
        var projectRepositoryMock = new Mock<IProjectRepository>();
        projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var handler = new ReopenStageForAmendmentCommandHandler(
            projectRepositoryMock.Object,
            new Mock<IConversationRepository>().Object,
            new Mock<IPromptService>().Object,
            TimeProvider.System);

        var result = await handler.Handle(
            new ReopenStageForAmendmentCommand(stageId, "REQ-001"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WhenConversationAlreadyExists_DoesNotCreateDuplicate()
    {
        var project = new Project("TEST", "Test Project", null, "TS001",
            ComplianceDomain.Generic, "idris.issa", TimeProvider.System);

        var stage = project.PipelineStages.First(s => s.StageType == StageType.Pxd);
        var stageId = stage.Id;

        var existingConversation = new Conversation(stageId, 3, TimeProvider.System, "REQ-001");

        var projectRepositoryMock = new Mock<IProjectRepository>();
        var conversationRepositoryMock = new Mock<IConversationRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();

        projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        conversationRepositoryMock
            .Setup(r => r.GetByStageAndRequirementIdAsync(
                stageId, "REQ-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConversation);
        conversationRepositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        projectRepositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new ReopenStageForAmendmentCommandHandler(
            projectRepositoryMock.Object,
            conversationRepositoryMock.Object,
            new Mock<IPromptService>().Object,
            TimeProvider.System);

        var result = await handler.Handle(
            new ReopenStageForAmendmentCommand(stageId, "REQ-001"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        conversationRepositoryMock.Verify(r => r.AddAsync(
            It.IsAny<Conversation>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
