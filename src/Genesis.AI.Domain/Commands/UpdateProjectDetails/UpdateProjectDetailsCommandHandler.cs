using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateProjectDetails;

public sealed class UpdateProjectDetailsCommandHandler : IRequestHandler<UpdateProjectDetailsCommand, Unit>
{
    private readonly IProjectRepository _projectRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateProjectDetailsCommandHandler(
        IProjectRepository projectRepository,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Unit> Handle(UpdateProjectDetailsCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException($"Project with ID '{request.ProjectId}' was not found.");

        var complianceDomain = project.ComplianceDomain;
        if (!string.IsNullOrWhiteSpace(request.ComplianceDomain)
            && Enum.TryParse<ComplianceDomain>(request.ComplianceDomain, ignoreCase: true, out var parsedDomain))
        {
            complianceDomain = parsedDomain;
        }

        var name = request.Name is null
            ? project.Name
            : (string.IsNullOrWhiteSpace(request.Name) ? project.Name : request.Name);
        var description = request.Description is null
            ? project.Description
            : (string.IsNullOrWhiteSpace(request.Description) ? null : request.Description);
        var timeSheetCode = request.TimeSheetCode is null
            ? project.TimeSheetCode
            : (string.IsNullOrWhiteSpace(request.TimeSheetCode) ? project.TimeSheetCode : request.TimeSheetCode);

        project.UpdateDetails(
            name,
            description,
            timeSheetCode,
            complianceDomain,
            _timeProvider);

        await _projectRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
