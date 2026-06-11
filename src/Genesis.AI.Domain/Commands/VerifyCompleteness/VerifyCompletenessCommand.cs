using MediatR;

namespace Genesis.AI.Domain.Commands.VerifyCompleteness;

public sealed record VerifyCompletenessCommand(Guid ProjectId)
    : IRequest<VerifyCompletenessResult>;
