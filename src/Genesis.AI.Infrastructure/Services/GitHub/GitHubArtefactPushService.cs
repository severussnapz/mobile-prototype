using System.Text;
using Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services.GitHub;

public sealed class GitHubArtefactPushService : IGitHubArtefactPushService
{
    private static readonly Dictionary<string, string> PathPrefixMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["requirements/"]    = ".genesis/requirements/",
            ["architecture/"]    = ".genesis/architecture/",
            ["clinical-safety/"] = ".genesis/clinical-safety/",
            ["ig/"]              = ".genesis/ig/",
            ["security/"]        = ".genesis/security/",
            ["prototype/"]       = ".genesis/prototype/",
            ["session-close/"]   = ".genesis/session-close/",
            ["project/"]         = ".genesis/project/",
            ["changes/"]         = ".genesis/changes/",
            ["feedback/"]        = ".genesis/feedback/",
        };

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository? _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IGitHubTokenService _tokenService;
    private readonly IGitHubContentsService _contentsService;
    private readonly IPushFailureLogRepository _pushFailureLogRepository;
    private readonly IAssemblyVersionProvider _versionProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GitHubArtefactPushService> _logger;

    public GitHubArtefactPushService(
        IProjectRepository projectRepository,
        IArtefactStorageService artefactStorageService,
        IGitHubTokenService tokenService,
        IGitHubContentsService contentsService,
        IPushFailureLogRepository pushFailureLogRepository,
        IAssemblyVersionProvider versionProvider,
        TimeProvider timeProvider,
        ILogger<GitHubArtefactPushService> logger,
        IArtefactRepository? artefactRepository = null)
    {
        _projectRepository = projectRepository;
        _artefactRepository = artefactRepository;
        _artefactStorageService = artefactStorageService;
        _tokenService = tokenService;
        _contentsService = contentsService;
        _pushFailureLogRepository = pushFailureLogRepository;
        _versionProvider = versionProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task PushAsync(
        Guid projectId, Guid artefactId, string filePath, int version,
        string contentType, string s3Key, string triggeredBy,
        CancellationToken ct)
    {
        try
        {
            var project = await _projectRepository.GetByIdAsync(projectId, ct);
            if (project is null || !project.HasGitHubConfig)
            {
                return;
            }

            var (installationId, owner, repoName) = GetGitHubProjectConfiguration(project);
            var targetPath = MapPath(filePath);
            if (targetPath is null)
            {
                _logger.LogWarning("Unmapped path {FilePath} — skipping push", filePath);
                return;
            }

            var content = await LoadArtefactContentAsync(projectId, artefactId, filePath, contentType, s3Key, ct);
            if (content is null)
            {
                return;
            }

            var token = await _tokenService.GetInstallationTokenAsync(installationId, ct);
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            await PushContentAsync(
                token, owner, repoName, targetPath, content,
                filePath, version, triggeredBy, projectId, artefactId, ct);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "GitHubArtefactPushService: failed to push {FilePath} (artefact {ArtefactId}) to GitHub",
                filePath,
                artefactId);

            var log = new PushFailureLog(projectId, artefactId, filePath, exception.Message, _timeProvider);
            await _pushFailureLogRepository.AddAsync(log, ct);
        }
    }

    private static (string InstallationId, string Owner, string RepoName) GetGitHubProjectConfiguration(Genesis.AI.Domain.AggregatesModel.ProjectAggregate.Project project)
    {
        var installationId = project.GitHubInstallationId ?? throw new InvalidOperationException("GitHub config inconsistent");
        var owner = project.GitHubRepoOwner ?? throw new InvalidOperationException("GitHub config inconsistent");
        var repoName = project.GitHubRepoName ?? throw new InvalidOperationException("GitHub config inconsistent");
        return (installationId, owner, repoName);
    }

    private async Task PushContentAsync(
        string token,
        string owner,
        string repoName,
        string targetPath,
        byte[] content,
        string filePath,
        int version,
        string triggeredBy,
        Guid projectId,
        Guid artefactId,
        CancellationToken ct)
    {
        var existingSha = await _contentsService.GetFileShaAsync(token, owner, repoName, targetPath, ct);
        var commitMessage = BuildCommitMessage(filePath, version, triggeredBy, projectId, artefactId);

        await _contentsService.PushFileAsync(
            token,
            owner,
            repoName,
            targetPath,
            content,
            commitMessage,
            existingSha,
            ct);

        _logger.LogInformation("Push complete for {TargetPath}", targetPath);

        if (_artefactRepository is not null)
        {
            await _artefactRepository.MarkPushedToGitHubAsync(artefactId, _timeProvider, ct);
        }
    }

    private async Task<byte[]?> LoadArtefactContentAsync(
        Guid projectId,
        Guid artefactId,
        string filePath,
        string contentType,
        string s3Key,
        CancellationToken ct)
    {
        var isBinary = IsContentTypeBinary(contentType);

        if (isBinary)
        {
            var binaryContent = await _artefactStorageService.GetBinaryContentAsync(s3Key, ct);
            if (binaryContent is not null)
            {
                return binaryContent;
            }

            await LogMissingContentAsync(projectId, artefactId, filePath, "S3 binary content returned null", ct);
            return null;
        }

        var textContent = await _artefactStorageService.GetContentAsync(s3Key, ct);
        if (textContent is not null)
        {
            return Encoding.UTF8.GetBytes(textContent);
        }

        await LogMissingContentAsync(projectId, artefactId, filePath, "S3 text content returned null", ct);
        return null;
    }

    private async Task LogMissingContentAsync(Guid projectId, Guid artefactId, string filePath, string message, CancellationToken ct)
    {
        var failureLog = new PushFailureLog(projectId, artefactId, filePath, message, _timeProvider);
        await _pushFailureLogRepository.AddAsync(failureLog, ct);
    }

    private string BuildCommitMessage(string filePath, int version, string triggeredBy, Guid projectId, Guid artefactId)
    {
        var appVersion = _versionProvider.GetVersion();
        return
            $"feat(artefacts): publish {filePath} v{version}\n\n" +
            $"Triggered-By: {triggeredBy}\n" +
            $"Approved-By: {triggeredBy}\n" +
            $"Project-ID: {projectId}\n" +
            $"Artefact-ID: {artefactId}\n" +
            $"Genesis-AI-Version: {appVersion}";
    }

    private static string? MapPath(string filePath)
    {
        foreach (var (prefix, target) in PathPrefixMap)
        {
            if (filePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return target + filePath[prefix.Length..];
            }
        }
        // Root-level files (no subdirectory) map to .genesis/ root
        if (!filePath.Contains('/'))
            return $".genesis/{filePath}";
        return null;
    }

    private static bool IsContentTypeBinary(string contentType)
    {
        return contentType switch
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => true,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => true,
            "application/octet-stream" => true,
            _ => false
        };
    }
}
