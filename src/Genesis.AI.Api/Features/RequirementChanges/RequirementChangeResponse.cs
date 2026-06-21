using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;

namespace Genesis.AI.Api.Features.RequirementChanges;

public sealed class RequirementChangeResponse
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string ReqId { get; init; } = string.Empty;
    public string ChangeType { get; init; } = string.Empty;
    public string RaisingPipeline { get; init; } = string.Empty;
    public Guid? RaisingPipelineConversationId { get; init; }
    public string? ProposedAcText { get; init; }
    public string? ApprovedAcText { get; init; }
    public bool HumanEdited { get; init; }
    public string Rationale { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string ClinicalSafetyImpact { get; init; } = string.Empty;
    public string IgImpact { get; init; } = string.Empty;
    public string SecurityImpact { get; init; } = string.Empty;
    public bool ClinicalSafetyReviewed { get; init; }
    public bool IgReviewed { get; init; }
    public bool SecurityReviewed { get; init; }
    public bool HasOpenDefiniteReviews { get; init; }
    public string? ApprovedBy { get; init; }
    public DateTimeOffset? ApprovedAt { get; init; }
    public string? UndoneBy { get; init; }
    public DateTimeOffset? UndoneAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;

    public static RequirementChangeResponse FromDomain(RequirementChange change)
    {
        return new RequirementChangeResponse
        {
            Id = change.Id,
            ProjectId = change.ProjectId,
            ReqId = change.ReqId,
            ChangeType = change.ChangeType.ToString(),
            RaisingPipeline = change.RaisingPipeline,
            RaisingPipelineConversationId = change.RaisingPipelineConversationId,
            ProposedAcText = change.ProposedAcText,
            ApprovedAcText = change.ApprovedAcText,
            HumanEdited = change.HumanEdited,
            Rationale = change.Rationale,
            Status = change.Status.ToString(),
            ClinicalSafetyImpact = change.ClinicalSafetyImpact.ToString(),
            IgImpact = change.IgImpact.ToString(),
            SecurityImpact = change.SecurityImpact.ToString(),
            ClinicalSafetyReviewed = change.ClinicalSafetyReviewed,
            IgReviewed = change.IgReviewed,
            SecurityReviewed = change.SecurityReviewed,
            HasOpenDefiniteReviews = change.HasOpenDefiniteReviews(),
            ApprovedBy = change.ApprovedBy,
            ApprovedAt = change.ApprovedAt,
            UndoneBy = change.UndoneBy,
            UndoneAt = change.UndoneAt,
            CreatedAt = change.CreatedAt,
            CreatedBy = change.CreatedBy
        };
    }
}
