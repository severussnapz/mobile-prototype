using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateProjectP00;

public sealed class UpdateProjectP00CommandHandler : IRequestHandler<UpdateProjectP00Command, Unit>
{
    private readonly IProjectRepository _projectRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateProjectP00CommandHandler(
        IProjectRepository projectRepository,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Unit> Handle(UpdateProjectP00Command request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException($"Project with ID '{request.ProjectId}' was not found.");

        var releaseType = request.ReleaseType is null
            ? project.ReleaseType
            : (string.IsNullOrWhiteSpace(request.ReleaseType) ? null : request.ReleaseType);
        var pilotDeploymentProcess = request.PilotDeploymentProcess is null
            ? project.PilotDeploymentProcess
            : (string.IsNullOrWhiteSpace(request.PilotDeploymentProcess) ? null : request.PilotDeploymentProcess);

        project.UpdateP00Configuration(
            releaseType,
            request.AssuranceRequired ?? project.AssuranceRequired,
            pilotDeploymentProcess,
            request.CsoRoleAssigned ?? project.CsoRoleAssigned,
            request.IgOwnerRoleAssigned ?? project.IgOwnerRoleAssigned,
            request.SecurityReviewerAssigned ?? project.SecurityReviewerAssigned,
            request.MedicalDeviceFlag ?? project.MedicalDeviceFlag,
            project.FigmaFileUrl,
            project.FigmaPatEncrypted,
            _timeProvider);

        await _projectRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
