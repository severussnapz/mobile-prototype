namespace Genesis.AI.Domain.Commands.ApproveEmReview;

public sealed record ApproveEmReviewResult(
    ApproveEmReviewStatus Status,
    string? ErrorDetail);
