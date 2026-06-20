using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Xunit;

namespace Genesis.AI.Tests.Domain;

public class RequirementChangeTests
{
    [Fact]
    public void Propose_WhenValidRequest_CreatesPendingChange()
    {
        var change = RequirementChange.Propose(
            projectId: Guid.NewGuid(),
            reqId: "REQ-001",
            changeType: ChangeType.Gap,
            raisingPipeline: "pipeline_05_pxd",
            raisingPipelineConversationId: null,
            proposedAcText: "[ ] Step indicator shows all steps.",
            rationale: "Missing behaviour for conditional steps",
            createdBy: "idris.issa");

        Assert.Equal(ChangeStatus.Pending, change.Status);
        Assert.Equal("REQ-001", change.ReqId);
        Assert.Equal(ChangeType.Gap, change.ChangeType);
        Assert.Equal(ImpactLevel.None, change.ClinicalSafetyImpact);
        Assert.Equal(ImpactLevel.None, change.IgImpact);
        Assert.Equal(ImpactLevel.None, change.SecurityImpact);
        Assert.False(change.HumanEdited);
        Assert.Null(change.ApprovedAcText);
    }

    [Fact]
    public void Propose_WhenContradictionType_ProposedAcTextCanBeNull()
    {
        var change = RequirementChange.Propose(
            projectId: Guid.NewGuid(),
            reqId: "REQ-001",
            changeType: ChangeType.Contradiction,
            raisingPipeline: "pipeline_05_pxd",
            raisingPipelineConversationId: null,
            proposedAcText: null,
            rationale: "Conflicts with REQ-007",
            createdBy: "idris.issa");

        Assert.Equal(ChangeType.Contradiction, change.ChangeType);
        Assert.Null(change.ProposedAcText);
    }

    [Fact]
    public void Approve_WhenPending_SetsApprovedStatus()
    {
        var change = BuildPendingChange();

        change.Approve(
            approvedAcText: "[ ] Step indicator shows all steps.",
            clinicalSafetyImpact: ImpactLevel.None,
            igImpact: ImpactLevel.Possible,
            securityImpact: ImpactLevel.None,
            approvedBy: "idris.issa",
            timeProvider: TimeProvider.System);

        Assert.Equal(ChangeStatus.Approved, change.Status);
        Assert.Equal(ImpactLevel.Possible, change.IgImpact);
        Assert.Equal("idris.issa", change.ApprovedBy);
        Assert.NotNull(change.ApprovedAt);
        Assert.False(change.HumanEdited);
    }

    [Fact]
    public void Approve_WhenAcTextDiffersFromProposed_SetsHumanEdited()
    {
        var change = BuildPendingChange();

        change.Approve(
            approvedAcText: "[ ] Step indicator shows all 5 steps.",
            clinicalSafetyImpact: ImpactLevel.None,
            igImpact: ImpactLevel.None,
            securityImpact: ImpactLevel.None,
            approvedBy: "idris.issa",
            timeProvider: TimeProvider.System);

        Assert.True(change.HumanEdited);
        Assert.Equal("[ ] Step indicator shows all 5 steps.", change.ApprovedAcText);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ThrowsInvalidOperationException()
    {
        var change = BuildPendingChange();
        change.Approve("[ ] Step indicator shows all steps.", ImpactLevel.None,
            ImpactLevel.None, ImpactLevel.None, "idris.issa", TimeProvider.System);

        Assert.Throws<InvalidOperationException>(() =>
            change.Approve("[ ] Step indicator shows all steps.", ImpactLevel.None,
                ImpactLevel.None, ImpactLevel.None, "idris.issa", TimeProvider.System));
    }

    [Fact]
    public void Undo_WhenApproved_SetsPendingStatus()
    {
        var change = BuildPendingChange();
        change.Approve("[ ] Step.", ImpactLevel.None, ImpactLevel.None,
            ImpactLevel.None, "idris.issa", TimeProvider.System);

        change.Undo(undoneBy: "idris.issa", rationale: "Wrong wording",
            timeProvider: TimeProvider.System);

        Assert.Equal(ChangeStatus.Undone, change.Status);
        Assert.Equal("idris.issa", change.UndoneBy);
        Assert.Equal("Wrong wording", change.UndoRationale);
        Assert.NotNull(change.UndoneAt);
    }

    [Fact]
    public void Undo_WhenPending_ThrowsInvalidOperationException()
    {
        var change = BuildPendingChange();

        Assert.Throws<InvalidOperationException>(() =>
            change.Undo("idris.issa", "reason", TimeProvider.System));
    }

    [Fact]
    public void Reject_WhenPending_SetsRejectedStatus()
    {
        var change = BuildPendingChange();

        change.Reject(rejectedBy: "idris.issa", timeProvider: TimeProvider.System);

        Assert.Equal(ChangeStatus.Rejected, change.Status);
    }

    [Fact]
    public void RecordClinicalSafetyReview_WhenDefiniteImpact_SetsReviewed()
    {
        var change = BuildPendingChange();
        change.Approve("[ ] Step.", ImpactLevel.Definite, ImpactLevel.None,
            ImpactLevel.None, "idris.issa", TimeProvider.System);

        change.RecordClinicalSafetyReview(
            reviewer: "cso@emis.com",
            timeProvider: TimeProvider.System);

        Assert.True(change.ClinicalSafetyReviewed);
        Assert.Equal("cso@emis.com", change.ClinicalSafetyReviewer);
        Assert.NotNull(change.ClinicalSafetyReviewedAt);
    }

    [Fact]
    public void HasOpenDefiniteReviews_WhenDefiniteAndUnreviewed_ReturnsTrue()
    {
        var change = BuildPendingChange();
        change.Approve("[ ] Step.", ImpactLevel.None, ImpactLevel.Definite,
            ImpactLevel.None, "idris.issa", TimeProvider.System);

        Assert.True(change.HasOpenDefiniteReviews());
    }

    [Fact]
    public void HasOpenDefiniteReviews_WhenAllNone_ReturnsFalse()
    {
        var change = BuildPendingChange();
        change.Approve("[ ] Step.", ImpactLevel.None, ImpactLevel.None,
            ImpactLevel.None, "idris.issa", TimeProvider.System);

        Assert.False(change.HasOpenDefiniteReviews());
    }

    [Fact]
    public void HasOpenDefiniteReviews_WhenDefiniteAndReviewed_ReturnsFalse()
    {
        var change = BuildPendingChange();
        change.Approve("[ ] Step.", ImpactLevel.Definite, ImpactLevel.None,
            ImpactLevel.None, "idris.issa", TimeProvider.System);
        change.RecordClinicalSafetyReview("cso@emis.com", TimeProvider.System);

        Assert.False(change.HasOpenDefiniteReviews());
    }

    private static RequirementChange BuildPendingChange()
    {
        return RequirementChange.Propose(
            projectId: Guid.NewGuid(),
            reqId: "REQ-001",
            changeType: ChangeType.Gap,
            raisingPipeline: "pipeline_05_pxd",
            raisingPipelineConversationId: null,
            proposedAcText: "[ ] Step indicator shows all steps.",
            rationale: "Missing behaviour",
            createdBy: "idris.issa");
    }
}
