namespace Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;

public sealed class RequirementChange
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string ReqId { get; private set; } = string.Empty;
    public ChangeType ChangeType { get; private set; }
    public string RaisingPipeline { get; private set; } = string.Empty;
    public Guid? RaisingPipelineConversationId { get; private set; }
    public string? ProposedAcText { get; private set; }
    public string? ApprovedAcText { get; private set; }
    public bool HumanEdited { get; private set; }
    public string Rationale { get; private set; } = string.Empty;
    public ChangeStatus Status { get; private set; }
    public ImpactLevel ClinicalSafetyImpact { get; private set; }
    public ImpactLevel IgImpact { get; private set; }
    public ImpactLevel SecurityImpact { get; private set; }
    public bool ClinicalSafetyReviewed { get; private set; }
    public string? ClinicalSafetyReviewer { get; private set; }
    public DateTimeOffset? ClinicalSafetyReviewedAt { get; private set; }
    public bool IgReviewed { get; private set; }
    public string? IgReviewer { get; private set; }
    public DateTimeOffset? IgReviewedAt { get; private set; }
    public bool SecurityReviewed { get; private set; }
    public string? SecurityReviewer { get; private set; }
    public DateTimeOffset? SecurityReviewedAt { get; private set; }
    public IReadOnlyList<string>? PrototypeFragmentsAffected { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? UndoneBy { get; private set; }
    public DateTimeOffset? UndoneAt { get; private set; }
    public string? UndoRationale { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    private RequirementChange() { }

    public static RequirementChange Propose(
        Guid projectId,
        string reqId,
        ChangeType changeType,
        string raisingPipeline,
        Guid? raisingPipelineConversationId,
        string? proposedAcText,
        string rationale,
        string createdBy)
    {
        if (changeType != ChangeType.Contradiction && string.IsNullOrWhiteSpace(proposedAcText))
        {
            throw new ArgumentException(
                "proposedAcText is required for Gap and Clarification change types.",
                nameof(proposedAcText));
        }

        return new RequirementChange
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ReqId = reqId,
            ChangeType = changeType,
            RaisingPipeline = raisingPipeline,
            RaisingPipelineConversationId = raisingPipelineConversationId,
            ProposedAcText = proposedAcText,
            Rationale = rationale,
            Status = ChangeStatus.Pending,
            ClinicalSafetyImpact = ImpactLevel.None,
            IgImpact = ImpactLevel.None,
            SecurityImpact = ImpactLevel.None,
            ClinicalSafetyReviewed = false,
            IgReviewed = false,
            SecurityReviewed = false,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Approve(
        string? approvedAcText,
        ImpactLevel clinicalSafetyImpact,
        ImpactLevel igImpact,
        ImpactLevel securityImpact,
        string approvedBy,
        TimeProvider timeProvider)
    {
        if (Status != ChangeStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot approve a requirement change with status '{Status}'.");
        }

        var effectiveAcText = approvedAcText ?? ProposedAcText;
        HumanEdited = !string.Equals(effectiveAcText, ProposedAcText, StringComparison.Ordinal);
        ApprovedAcText = effectiveAcText;
        ClinicalSafetyImpact = clinicalSafetyImpact;
        IgImpact = igImpact;
        SecurityImpact = securityImpact;
        ApprovedBy = approvedBy;
        ApprovedAt = timeProvider.GetUtcNow();
        Status = ChangeStatus.Approved;
    }

    public void Undo(string undoneBy, string? rationale, TimeProvider timeProvider)
    {
        if (Status != ChangeStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot undo a requirement change with status '{Status}'.");
        }

        UndoneBy = undoneBy;
        UndoneAt = timeProvider.GetUtcNow();
        UndoRationale = rationale;
        Status = ChangeStatus.Undone;
    }

    public void Reject(string rejectedBy, TimeProvider timeProvider)
    {
        if (Status != ChangeStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot reject a requirement change with status '{Status}'.");
        }

        ApprovedBy = rejectedBy;
        ApprovedAt = timeProvider.GetUtcNow();
        Status = ChangeStatus.Rejected;
    }

    public void RecordClinicalSafetyReview(string reviewer, TimeProvider timeProvider)
    {
        ClinicalSafetyReviewed = true;
        ClinicalSafetyReviewer = reviewer;
        ClinicalSafetyReviewedAt = timeProvider.GetUtcNow();
    }

    public void RecordIgReview(string reviewer, TimeProvider timeProvider)
    {
        IgReviewed = true;
        IgReviewer = reviewer;
        IgReviewedAt = timeProvider.GetUtcNow();
    }

    public void RecordSecurityReview(string reviewer, TimeProvider timeProvider)
    {
        SecurityReviewed = true;
        SecurityReviewer = reviewer;
        SecurityReviewedAt = timeProvider.GetUtcNow();
    }

    public bool HasOpenDefiniteReviews()
    {
        return (ClinicalSafetyImpact == ImpactLevel.Definite && !ClinicalSafetyReviewed)
            || (IgImpact == ImpactLevel.Definite && !IgReviewed)
            || (SecurityImpact == ImpactLevel.Definite && !SecurityReviewed);
    }
}
