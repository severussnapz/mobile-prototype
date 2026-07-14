using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Domain.Commands.UpdateProjectGitHub;

public sealed class UpdateProjectGitHubCommandHandler : IRequestHandler<UpdateProjectGitHubCommand, UpdateProjectGitHubResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly TimeProvider _timeProvider;
    private readonly IGenesisStructureScaffolder? _scaffolder;
    private readonly ILogger<UpdateProjectGitHubCommandHandler> _logger;

    public UpdateProjectGitHubCommandHandler(
        IProjectRepository projectRepository,
        ISecretEncryptionService secretEncryptionService,
        TimeProvider timeProvider,
        IGenesisStructureScaffolder? scaffolder = null,
        ILogger<UpdateProjectGitHubCommandHandler>? logger = null)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _secretEncryptionService = secretEncryptionService ?? throw new ArgumentNullException(nameof(secretEncryptionService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _scaffolder = scaffolder;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateProjectGitHubCommandHandler>.Instance;
    }

    public async Task<UpdateProjectGitHubResult> Handle(UpdateProjectGitHubCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException($"Project with ID '{request.ProjectId}' was not found.");

        var apiRepoUrl = request.GitHubApiRepoUrl is null
            ? project.GitHubApiRepoUrl
            : string.IsNullOrWhiteSpace(request.GitHubApiRepoUrl) ? null : request.GitHubApiRepoUrl;
        var appRepoUrl = request.GitHubAppRepoUrl is null
            ? project.GitHubAppRepoUrl
            : string.IsNullOrWhiteSpace(request.GitHubAppRepoUrl) ? null : request.GitHubAppRepoUrl;
        var figmaFileUrl = request.FigmaFileUrl is null
            ? project.FigmaFileUrl
            : string.IsNullOrWhiteSpace(request.FigmaFileUrl) ? null : request.FigmaFileUrl;

        if (!string.IsNullOrWhiteSpace(request.GitHubInstallationId)
            && !string.IsNullOrWhiteSpace(apiRepoUrl))
        {
            var uri = new Uri(apiRepoUrl);
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            var owner = parts.Length > 0 ? parts[0] : "";
            var repoName = parts.Length > 1 ? parts[1] : "";
            project.SetGitHubConfig(
                apiRepoUrl, appRepoUrl ?? apiRepoUrl, owner, repoName,
                request.GitHubInstallationId, _timeProvider);
        }
        else
        {
            project.UpdateGitHubUrls(apiRepoUrl, appRepoUrl, figmaFileUrl, _timeProvider);
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

        if (_scaffolder is not null && project.HasGitHubConfig)
        {
            try { await _scaffolder.ScaffoldAsync(request.ProjectId, request.TriggeredBy, cancellationToken); }
            catch (Exception ex) { _logger?.LogWarning(ex, "Scaffold failed {ProjectId}", request.ProjectId); }
        }

        return new UpdateProjectGitHubResult(
            request.FigmaPat is not null ? request.FigmaPat : null);
    }
}
