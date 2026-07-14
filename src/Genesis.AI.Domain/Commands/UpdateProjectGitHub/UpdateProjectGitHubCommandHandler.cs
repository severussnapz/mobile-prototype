using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
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
    private readonly IGitHubTokenService? _tokenService;
    private readonly IGitHubContentsService? _contentsService;
    private readonly ILogger<UpdateProjectGitHubCommandHandler> _logger;

    public UpdateProjectGitHubCommandHandler(
        IProjectRepository projectRepository,
        ISecretEncryptionService secretEncryptionService,
        TimeProvider timeProvider,
        IGenesisStructureScaffolder? scaffolder = null,
        IGitHubTokenService? tokenService = null,
        IGitHubContentsService? contentsService = null,
        ILogger<UpdateProjectGitHubCommandHandler>? logger = null)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _secretEncryptionService = secretEncryptionService ?? throw new ArgumentNullException(nameof(secretEncryptionService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _scaffolder = scaffolder;
        _tokenService = tokenService;
        _contentsService = contentsService;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateProjectGitHubCommandHandler>.Instance;
    }

    public async Task<UpdateProjectGitHubResult> Handle(UpdateProjectGitHubCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException($"Project with ID '{request.ProjectId}' was not found.");

        var apiRepoUrl = ResolveUrl(request.GitHubApiRepoUrl, project.GitHubApiRepoUrl);
        var appRepoUrl = ResolveUrl(request.GitHubAppRepoUrl, project.GitHubAppRepoUrl);
        var figmaFileUrl = ResolveUrl(request.FigmaFileUrl, project.FigmaFileUrl);

        ApplyGitHubConfiguration(project, request, apiRepoUrl, appRepoUrl, figmaFileUrl);
        ApplyFigmaPat(project, request);

        await _projectRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        await ScaffoldIfConfiguredAsync(project, request, cancellationToken);

        var (apiRepoVerified, apiRepoError, appRepoVerified, appRepoError) =
            await VerifyRepositoriesAsync(project, apiRepoUrl, appRepoUrl, cancellationToken);

        return new UpdateProjectGitHubResult(
            request.FigmaPat is not null ? request.FigmaPat : null,
            apiRepoVerified,
            apiRepoError,
            appRepoVerified,
            appRepoError);
    }

    private static string? ResolveUrl(string? requested, string? existing)
    {
        if (requested is null)
        {
            return existing;
        }

        return string.IsNullOrWhiteSpace(requested) ? null : requested;
    }

    private static (string Owner, string Repo) ParseOwnerRepo(string repoUrl)
    {
        var uri = new Uri(repoUrl);
        var parts = uri.AbsolutePath.Trim('/').Split('/');
        var owner = parts.Length > 0 ? parts[0] : "";
        var repo = parts.Length > 1 ? parts[1] : "";
        return (owner, repo);
    }

    private void ApplyGitHubConfiguration(
        Project project,
        UpdateProjectGitHubCommand request,
        string? apiRepoUrl,
        string? appRepoUrl,
        string? figmaFileUrl)
    {
        if (!string.IsNullOrWhiteSpace(request.GitHubInstallationId))
        {
            var (owner, repoName) = string.IsNullOrWhiteSpace(apiRepoUrl)
                ? ("", "")
                : ParseOwnerRepo(apiRepoUrl);

            project.SetGitHubConfig(
                apiRepoUrl, appRepoUrl, owner, repoName,
                request.GitHubInstallationId, _timeProvider);
        }
        else
        {
            project.UpdateGitHubUrls(apiRepoUrl, appRepoUrl, figmaFileUrl, _timeProvider);
        }
    }

    private void ApplyFigmaPat(Project project, UpdateProjectGitHubCommand request)
    {
        if (request.FigmaPat is null)
        {
            return;
        }

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

    private async Task ScaffoldIfConfiguredAsync(
        Project project, UpdateProjectGitHubCommand request, CancellationToken cancellationToken)
    {
        if (_scaffolder is null || !project.HasGitHubConfig)
        {
            return;
        }

        try
        {
            var result = await _scaffolder.ScaffoldAsync(request.ProjectId, request.TriggeredBy, cancellationToken);
            if (!result.IsSuccess)
            {
                throw new GitHubScaffoldFailedException(
                    $"Failed to scaffold the .genesis GitHub structure. {result.FailureReason}");
            }
        }
        catch (Exception ex)
        {
            if (ex is GitHubScaffoldFailedException)
            {
                throw;
            }

            _logger.LogWarning(ex, "Scaffold failed {ProjectId}", request.ProjectId);
            throw new GitHubScaffoldFailedException(
                "Failed to scaffold the .genesis GitHub structure. Please try again.");
        }
    }

    private async Task<(bool? ApiVerified, string? ApiError, bool? AppVerified, string? AppError)> VerifyRepositoriesAsync(
        Project project, string? apiRepoUrl, string? appRepoUrl, CancellationToken cancellationToken)
    {
        if (project.GitHubInstallationId is null || _tokenService is null || _contentsService is null)
        {
            return (null, null, null, null);
        }

        bool? apiRepoVerified = null;
        string? apiRepoError = null;
        bool? appRepoVerified = null;
        string? appRepoError = null;

        try
        {
            var token = await _tokenService.GetInstallationTokenAsync(project.GitHubInstallationId, cancellationToken);

            if (apiRepoUrl is not null)
            {
                var (apiOwner, apiRepo) = ParseOwnerRepo(apiRepoUrl);
                (apiRepoVerified, apiRepoError) = await VerifyRepoAsync(token, apiOwner, apiRepo, cancellationToken);
            }

            if (appRepoUrl is not null)
            {
                var (appOwner, appRepo) = ParseOwnerRepo(appRepoUrl);
                (appRepoVerified, appRepoError) = await VerifyRepoAsync(token, appOwner, appRepo, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub verification exception for {ProjectId}", project.Id);
        }

        return (apiRepoVerified, apiRepoError, appRepoVerified, appRepoError);
    }

    private async Task<(bool? Verified, string? Error)> VerifyRepoAsync(
        string token, string owner, string repo, CancellationToken cancellationToken)
    {
        try
        {
            // Step A: Check if repo exists
            var repoExists = await _contentsService!.RepoExistsAsync(token, owner, repo, cancellationToken);
            if (!repoExists)
            {
                return (false, $"Repository not found: {owner}/{repo}. Check the URL.");
            }

            // Step B: Check if repo is already scaffolded
            var exists = await _contentsService!.FileExistsAsync(token, owner, repo, ".genesis/.gitkeep", cancellationToken);
            return (exists ? true : null, null);
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode == System.Net.HttpStatusCode.Forbidden ||
            ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return (false, $"Access denied to {owner}/{repo}. Check genesis-ai-bot permissions.");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "GitHub verification failed for {Owner}/{Repo}", owner, repo);
            return (false, $"Could not verify access to {owner}/{repo}.");
        }
    }
}
