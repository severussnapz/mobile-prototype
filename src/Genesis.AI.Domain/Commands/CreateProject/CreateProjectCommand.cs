using Genesis.AI.Domain.Enums;
using MediatR;

namespace Genesis.AI.Domain.Commands.CreateProject;

public record CreateProjectCommand(
    string Code,
    string Name,
    string? Description,
    ComplianceDomain ComplianceDomain,
    string CreatedBy) : IRequest<Guid>;
