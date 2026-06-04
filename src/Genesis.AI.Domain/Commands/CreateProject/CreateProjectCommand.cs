using Genesis.AI.Domain.Enums;
using MediatR;

namespace Genesis.AI.Domain.Commands.CreateProject;

public record CreateProjectCommand(
    string Code,
    string Name,
    string? Description,
    string TimeSheetCode,
    ComplianceDomain ComplianceDomain,
    string CreatedBy) : IRequest<Guid>;
