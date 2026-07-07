using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateProjectGitHub;

public sealed class UpdateProjectGitHubCommandHandler : IRequestHandler<UpdateProjectGitHubCommand, UpdateProjectGitHubResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly TimeProvider _timeProvider;

    public UpdateProjectGitHubCommandHandler(
        IProjectRepository projectRepository,
        ISecretEncryptionService secretEncryptionService,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _secretEncryptionService = secretEncryptionService ?? throw new ArgumentNullException(nameof(secretEncryptionService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<UpdateProjectGitHubResult> Handle(UpdateProjectGitHubCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException($"Project with ID '{request.ProjectId}' was not found.");

        if (request.GitHubApiRepoUrl is not null
            || request.GitHubAppRepoUrl is not null
            || request.FigmaFileUrl is not null)
        {
            project.UpdateGitHubUrls(
                request.GitHubApiRepoUrl,
                request.GitHubAppRepoUrl,
                request.FigmaFileUrl,
                _timeProvider);
        }

        if (request.FigmaPat is not null)
        {
            project.UpdateP00Configuration(
                project.ReleaseType,
                project.AssuranceRequired,
                project.PilotDeploymentProcess,
                project.CsoRoleAssigned,
                project.IgOwnerRoleAssigned,
                project.SecurityReviewerAssigned,
                project.MedicalDeviceFlag,
                project.FigmaFileUrl,
                _secretEncryptionService.Encrypt(request.FigmaPat),
                _timeProvider);
        }

        await _projectRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateProjectGitHubResult(
            request.FigmaPat is not null ? request.FigmaPat : null);
    }
}
