using System.Text.Json;
using Genesis.AI.Api.Features.RequirementChanges;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;

namespace Genesis.AI.Tests.Api.RequirementChanges;

public sealed class RequirementChangeResponseAllFieldsMappingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void RequirementChangeResponse_FromDomain_MapsAndSerialisesAllFields()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var projectId = Guid.NewGuid();
        var raisingConversationId = Guid.NewGuid();

        var change = RequirementChange.Propose(
            projectId: projectId,
            reqId: "REQ-999",
            changeType: ChangeType.Gap,
            raisingPipeline: "pipeline_05_pxd",
            raisingPipelineConversationId: raisingConversationId,
            proposedAcText: "[ ] Proposed acceptance criteria",
            rationale: "Need clarification",
            createdBy: "creator-1",
            clinicalSafetyImpact: ImpactLevel.Definite,
            igImpact: ImpactLevel.Possible,
            securityImpact: ImpactLevel.Definite);

        change.Approve(
            approvedAcText: "[ ] Approved acceptance criteria with edits",
            clinicalSafetyImpact: ImpactLevel.Definite,
            igImpact: ImpactLevel.Possible,
            securityImpact: ImpactLevel.Definite,
            approvedBy: "approver-1",
            timeProvider: timeProvider);

        change.RecordClinicalSafetyReview("clinical-reviewer-1", timeProvider);

        change.Undo("undo-user-1", "Undo rationale", timeProvider);

        // Act
        var response = RequirementChangeResponse.FromDomain(change);
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert
        var root = JsonDocument.Parse(json).RootElement;

        Assert.True(root.TryGetProperty("id", out var idElement), "id field missing");
        Assert.Equal(change.Id, idElement.GetGuid());

        Assert.True(root.TryGetProperty("projectId", out var projectIdElement), "projectId field missing");
        Assert.Equal(change.ProjectId, projectIdElement.GetGuid());

        Assert.True(root.TryGetProperty("reqId", out var reqIdElement), "reqId field missing");
        Assert.Equal(change.ReqId, reqIdElement.GetString());

        Assert.True(root.TryGetProperty("changeType", out var changeTypeElement), "changeType field missing");
        Assert.Equal(change.ChangeType.ToString(), changeTypeElement.GetString());

        Assert.True(root.TryGetProperty("raisingPipeline", out var raisingPipelineElement), "raisingPipeline field missing");
        Assert.Equal(change.RaisingPipeline, raisingPipelineElement.GetString());

        Assert.True(root.TryGetProperty("raisingPipelineConversationId", out var raisingPipelineConversationIdElement), "raisingPipelineConversationId field missing");
        Assert.Equal(change.RaisingPipelineConversationId, raisingPipelineConversationIdElement.GetGuid());

        Assert.True(root.TryGetProperty("proposedAcText", out var proposedAcTextElement), "proposedAcText field missing");
        Assert.Equal(change.ProposedAcText, proposedAcTextElement.GetString());

        Assert.True(root.TryGetProperty("approvedAcText", out var approvedAcTextElement), "approvedAcText field missing");
        Assert.Equal(change.ApprovedAcText, approvedAcTextElement.GetString());

        Assert.True(root.TryGetProperty("humanEdited", out var humanEditedElement), "humanEdited field missing");
        Assert.Equal(change.HumanEdited, humanEditedElement.GetBoolean());

        Assert.True(root.TryGetProperty("rationale", out var rationaleElement), "rationale field missing");
        Assert.Equal(change.Rationale, rationaleElement.GetString());

        Assert.True(root.TryGetProperty("status", out var statusElement), "status field missing");
        Assert.Equal(change.Status.ToString(), statusElement.GetString());

        Assert.True(root.TryGetProperty("clinicalSafetyImpact", out var clinicalSafetyImpactElement), "clinicalSafetyImpact field missing");
        Assert.Equal(change.ClinicalSafetyImpact.ToString(), clinicalSafetyImpactElement.GetString());

        Assert.True(root.TryGetProperty("igImpact", out var igImpactElement), "igImpact field missing");
        Assert.Equal(change.IgImpact.ToString(), igImpactElement.GetString());

        Assert.True(root.TryGetProperty("securityImpact", out var securityImpactElement), "securityImpact field missing");
        Assert.Equal(change.SecurityImpact.ToString(), securityImpactElement.GetString());

        Assert.True(root.TryGetProperty("clinicalSafetyReviewed", out var clinicalSafetyReviewedElement), "clinicalSafetyReviewed field missing");
        Assert.Equal(change.ClinicalSafetyReviewed, clinicalSafetyReviewedElement.GetBoolean());

        Assert.True(root.TryGetProperty("igReviewed", out var igReviewedElement), "igReviewed field missing");
        Assert.Equal(change.IgReviewed, igReviewedElement.GetBoolean());

        Assert.True(root.TryGetProperty("securityReviewed", out var securityReviewedElement), "securityReviewed field missing");
        Assert.Equal(change.SecurityReviewed, securityReviewedElement.GetBoolean());

        Assert.True(root.TryGetProperty("hasOpenDefiniteReviews", out var hasOpenDefiniteReviewsElement), "hasOpenDefiniteReviews field missing");
        Assert.Equal(change.HasOpenDefiniteReviews(), hasOpenDefiniteReviewsElement.GetBoolean());

        Assert.True(root.TryGetProperty("approvedBy", out var approvedByElement), "approvedBy field missing");
        Assert.Equal(change.ApprovedBy, approvedByElement.GetString());

        Assert.True(root.TryGetProperty("approvedAt", out var approvedAtElement), "approvedAt field missing");
        Assert.Equal(change.ApprovedAt, approvedAtElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("undoneBy", out var undoneByElement), "undoneBy field missing");
        Assert.Equal(change.UndoneBy, undoneByElement.GetString());

        Assert.True(root.TryGetProperty("undoneAt", out var undoneAtElement), "undoneAt field missing");
        Assert.Equal(change.UndoneAt, undoneAtElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("createdAt", out var createdAtElement), "createdAt field missing");
        Assert.Equal(change.CreatedAt, createdAtElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("createdBy", out var createdByElement), "createdBy field missing");
        Assert.Equal(change.CreatedBy, createdByElement.GetString());
    }
}
