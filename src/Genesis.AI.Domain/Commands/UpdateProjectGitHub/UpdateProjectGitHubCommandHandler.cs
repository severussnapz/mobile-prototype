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

        if (!string.IsNullOrWhiteSpace(request.GitHubInstallationId)
            && !string.IsNullOrWhiteSpace(request.GitHubApiRepoUrl ?? project.GitHubApiRepoUrl))
        {
            var apiRepoUrl = request.GitHubApiRepoUrl ?? project.GitHubApiRepoUrl ?? "";
            var appRepoUrl = request.GitHubAppRepoUrl 
                ?? project.GitHubAppRepoUrl 
                ?? apiRepoUrl;
            var uri = new Uri(apiRepoUrl);
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            var owner = parts.Length > 0 ? parts[0] : "";
            var repoName = parts.Length > 1 ? parts[1] : "";
            project.SetGitHubConfig(
                apiRepoUrl, appRepoUrl, owner, repoName,
                request.GitHubInstallationId, _timeProvider);
        }
        else
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
