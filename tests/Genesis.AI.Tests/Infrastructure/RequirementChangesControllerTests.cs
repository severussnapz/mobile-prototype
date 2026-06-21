using Genesis.AI.Api.Features.RequirementChanges;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.ApproveRequirementChange;
using Genesis.AI.Domain.Commands.ProposeRequirementChange;
using Genesis.AI.Domain.Commands.RecordDomainReview;
using Genesis.AI.Domain.Commands.RejectRequirementChange;
using Genesis.AI.Domain.Commands.UndoApproveRequirementChange;
using Genesis.AI.Domain.Interfaces;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class RequirementChangesControllerTests
{
    [Fact]
    public void RequirementChangesController_HasCorrectRoute()
    {
        var routeAttr = typeof(RequirementChangesController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), false)
            .FirstOrDefault() as Microsoft.AspNetCore.Mvc.RouteAttribute;

        Assert.NotNull(routeAttr);
        Assert.Contains("requirement-changes", routeAttr!.Template);
    }

    [Fact]
    public void ProposeRequest_HasRequiredProperties()
    {
        var request = new ProposeRequirementChangeRequest
        {
            ReqId = "REQ-001",
            ChangeType = "gap",
            Rationale = "Missing behaviour",
            ProposedAcText = "[ ] New AC."
        };

        Assert.Equal("REQ-001", request.ReqId);
        Assert.Equal("gap", request.ChangeType);
    }

    [Fact]
    public void ApproveRequest_HasImpactFields()
    {
        var request = new ApproveRequirementChangeRequest
        {
            ApprovedAcText = "[ ] Edited AC.",
            ClinicalSafetyImpact = "none",
            IgImpact = "possible",
            SecurityImpact = "none"
        };

        Assert.Equal("possible", request.IgImpact);
        Assert.Null(request.ApprovedAcText is null ? null : request.ApprovedAcText == "[ ] Edited AC." ? null : "wrong");
    }

    [Fact]
    public void RequirementChangeResponse_MapsFromDomainObject()
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

        var response = RequirementChangeResponse.FromDomain(change);

        Assert.Equal("REQ-001", response.ReqId);
        Assert.Equal("Gap", response.ChangeType);
        Assert.Equal("Pending", response.Status);
        Assert.Equal("pipeline_05_pxd", response.RaisingPipeline);
        Assert.Equal("None", response.ClinicalSafetyImpact);
        Assert.Equal("None", response.IgImpact);
        Assert.Equal("None", response.SecurityImpact);
        Assert.False(response.HasOpenDefiniteReviews);
    }
}
