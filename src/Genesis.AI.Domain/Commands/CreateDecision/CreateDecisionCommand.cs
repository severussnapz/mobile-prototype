using MediatR;

namespace Genesis.AI.Domain.Commands.CreateDecision;

public record CreateDecisionCommand(
    Guid ProjectId,
    string Title,
    string Context,
    string Decision,
    string Consequences,
    string? AuthorErn,
    string? AuthorGivenName,
    string? AuthorFamilyName) : IRequest<CreateDecisionResult>;
