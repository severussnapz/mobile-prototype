using MediatR;

namespace Genesis.AI.Domain.Commands.ApproveEmReview;

public sealed record ApproveEmReviewCommand(Guid ProjectId, string UserId, string? Notes)
    : IRequest<ApproveEmReviewResult>;
