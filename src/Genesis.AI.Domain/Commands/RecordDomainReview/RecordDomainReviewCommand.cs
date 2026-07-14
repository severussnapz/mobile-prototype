namespace Genesis.AI.Domain.Commands.RecordDomainReview;

public sealed record RecordDomainReviewCommand(
    Guid ProjectId,
    Guid ChangeId,
    ReviewDomain Domain,
    string Reviewer);
