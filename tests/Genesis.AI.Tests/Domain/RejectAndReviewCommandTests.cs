using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.RejectRequirementChange;
using Genesis.AI.Domain.Commands.RecordDomainReview;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Domain;

public class RejectRequirementChangeCommandTests
{
    [Fact]
    public async Task Handle_WhenPendingChange_SetsRejectedStatus()
    {
        var changeId = Guid.NewGuid();
        var change = BuildPendingChange(changeId);

        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repositoryMock.Setup(r => r.GetByIdAsync(changeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(change);
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RejectRequirementChangeCommandHandler(
            repositoryMock.Object, TimeProvider.System);

        await handler.Handle(
            new RejectRequirementChangeCommand(changeId, "idris.issa"),
            CancellationToken.None);

        Assert.Equal(ChangeStatus.Rejected, change.Status);
    }

    [Fact]
    public async Task Handle_WhenChangeNotFound_ThrowsInvalidOperationException()
    {
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequirementChange?)null);

        var handler = new RejectRequirementChangeCommandHandler(
            repositoryMock.Object, TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new RejectRequirementChangeCommand(Guid.NewGuid(), "idris.issa"),
                CancellationToken.None));
    }

    private static RequirementChange BuildPendingChange(Guid changeId)
    {
        var change = RequirementChange.Propose(
            projectId: Guid.NewGuid(),
            reqId: "REQ-001",
            changeType: ChangeType.Gap,
            raisingPipeline: "pipeline_05_pxd",
            raisingPipelineConversationId: null,
            proposedAcText: "[ ] Step indicator shows all steps.",
            rationale: "Missing behaviour",
            createdBy: "idris.issa");

        typeof(RequirementChange).GetProperty("Id")!.SetValue(change, changeId);
        return change;
    }
}

public class RecordDomainReviewCommandTests
{
    [Fact]
    public async Task Handle_WhenClinicalSafetyReview_SetsClinicalSafetyReviewed()
    {
        var changeId = Guid.NewGuid();
        var change = BuildApprovedChange(changeId, ImpactLevel.Definite,
            ImpactLevel.None, ImpactLevel.None);

        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repositoryMock.Setup(r => r.GetByIdAsync(changeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(change);
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RecordDomainReviewCommandHandler(
            repositoryMock.Object, TimeProvider.System);

        await handler.Handle(
            new RecordDomainReviewCommand(changeId, ReviewDomain.ClinicalSafety, "cso@emis.com"),
            CancellationToken.None);

        Assert.True(change.ClinicalSafetyReviewed);
        Assert.Equal("cso@emis.com", change.ClinicalSafetyReviewer);
        Assert.False(change.HasOpenDefiniteReviews());
    }

    [Fact]
    public async Task Handle_WhenIgReview_SetsIgReviewed()
    {
        var changeId = Guid.NewGuid();
        var change = BuildApprovedChange(changeId, ImpactLevel.None,
            ImpactLevel.Definite, ImpactLevel.None);

        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repositoryMock.Setup(r => r.GetByIdAsync(changeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(change);
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RecordDomainReviewCommandHandler(
            repositoryMock.Object, TimeProvider.System);

        await handler.Handle(
            new RecordDomainReviewCommand(changeId, ReviewDomain.InformationGovernance,
                "dpo@emis.com"),
            CancellationToken.None);

        Assert.True(change.IgReviewed);
        Assert.Equal("dpo@emis.com", change.IgReviewer);
        Assert.False(change.HasOpenDefiniteReviews());
    }

    [Fact]
    public async Task Handle_WhenSecurityReview_SetsSecurityReviewed()
    {
        var changeId = Guid.NewGuid();
        var change = BuildApprovedChange(changeId, ImpactLevel.None,
            ImpactLevel.None, ImpactLevel.Definite);

        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var unitOfWorkMock = new Mock<Genesis.AI.Core.Data.IUnitOfWork>();
        repositoryMock.Setup(r => r.GetByIdAsync(changeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(change);
        repositoryMock.Setup(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RecordDomainReviewCommandHandler(
            repositoryMock.Object, TimeProvider.System);

        await handler.Handle(
            new RecordDomainReviewCommand(changeId, ReviewDomain.Security,
                "security@emis.com"),
            CancellationToken.None);

        Assert.True(change.SecurityReviewed);
        Assert.Equal("security@emis.com", change.SecurityReviewer);
        Assert.False(change.HasOpenDefiniteReviews());
    }

    private static RequirementChange BuildApprovedChange(
        Guid changeId,
        ImpactLevel clinicalSafety,
        ImpactLevel ig,
        ImpactLevel security)
    {
        var change = RequirementChange.Propose(
            projectId: Guid.NewGuid(),
            reqId: "REQ-001",
            changeType: ChangeType.Gap,
            raisingPipeline: "pipeline_05_pxd",
            raisingPipelineConversationId: null,
            proposedAcText: "[ ] Step indicator shows all steps.",
            rationale: "Missing behaviour",
            createdBy: "idris.issa");

        typeof(RequirementChange).GetProperty("Id")!.SetValue(change, changeId);

        change.Approve("[ ] Step indicator shows all steps.",
            clinicalSafety, ig, security, "idris.issa", TimeProvider.System);

        return change;
    }
}
