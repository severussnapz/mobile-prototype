using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.ProposeRequirementChange;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Domain;

public class ProposeRequirementChangeCommandTests
{
    [Fact]
    public async Task Handle_WhenValidGapProposal_CreatesAndSavesPendingChange()
    {
        var projectId = Guid.NewGuid();
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();

        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        RequirementChange? savedChange = null;
        repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<RequirementChange>(), It.IsAny<CancellationToken>()))
            .Callback<RequirementChange, CancellationToken>((change, _) => savedChange = change);

        var handler = new ProposeRequirementChangeCommandHandler(repositoryMock.Object);
        var command = new ProposeRequirementChangeCommand(
            ProjectId: projectId,
            ReqId: "REQ-001",
            ChangeType: ChangeType.Gap,
            RaisingPipeline: "pipeline_05_pxd",
            RaisingPipelineConversationId: null,
            ProposedAcText: "[ ] Step indicator shows all steps.",
            Rationale: "Missing behaviour for conditional steps",
            CreatedBy: "idris.issa");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(savedChange);
        Assert.Equal(ChangeStatus.Pending, savedChange!.Status);
        Assert.Equal("REQ-001", savedChange.ReqId);
        Assert.Equal(ChangeType.Gap, savedChange.ChangeType);
        Assert.Equal(ImpactLevel.None, savedChange.ClinicalSafetyImpact);
        Assert.NotEqual(Guid.Empty, result.ChangeId);
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<RequirementChange>(),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenContradictionWithNullAcText_CreatesPendingChange()
    {
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new ProposeRequirementChangeCommandHandler(repositoryMock.Object);
        var command = new ProposeRequirementChangeCommand(
            ProjectId: Guid.NewGuid(),
            ReqId: "REQ-001",
            ChangeType: ChangeType.Contradiction,
            RaisingPipeline: "pipeline_05_pxd",
            RaisingPipelineConversationId: null,
            ProposedAcText: null,
            Rationale: "Conflicts with REQ-007",
            CreatedBy: "idris.issa");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.ChangeId);
    }

    [Fact]
    public async Task Handle_WhenGapWithNullAcText_ThrowsArgumentException()
    {
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var handler = new ProposeRequirementChangeCommandHandler(repositoryMock.Object);
        var command = new ProposeRequirementChangeCommand(
            ProjectId: Guid.NewGuid(),
            ReqId: "REQ-001",
            ChangeType: ChangeType.Gap,
            RaisingPipeline: "pipeline_05_pxd",
            RaisingPipelineConversationId: null,
            ProposedAcText: null,
            Rationale: "Missing behaviour",
            CreatedBy: "idris.issa");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
