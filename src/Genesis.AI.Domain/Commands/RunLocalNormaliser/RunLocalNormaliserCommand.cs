using MediatR;

namespace Genesis.AI.Domain.Commands.RunLocalNormaliser;

public sealed record RunLocalNormaliserCommand(Guid ProjectId, string UserId)
    : IRequest<RunLocalNormaliserResult>;
