namespace Genesis.AI.Domain.Commands.RecordDomainReview;

public sealed record RecordDomainReviewCommand(
    Guid ChangeId,
    ReviewDomain Domain,
    string Reviewer);
