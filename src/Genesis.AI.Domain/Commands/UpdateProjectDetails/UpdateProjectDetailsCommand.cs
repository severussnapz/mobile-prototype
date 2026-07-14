using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateProjectDetails;

public sealed record UpdateProjectDetailsCommand(
    Guid ProjectId,
    string TriggeredBy,
    string? Name,
    string? Description,
    string? TimeSheetCode,
    string? ComplianceDomain
) : IRequest<Unit>;
