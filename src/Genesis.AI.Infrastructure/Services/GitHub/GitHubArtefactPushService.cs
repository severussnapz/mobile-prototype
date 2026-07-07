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
        };

    private readonly IProjectRepository _projectRepository;
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
        ILogger<GitHubArtefactPushService> logger)
    {
        _projectRepository = projectRepository;
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
            // Step 1: Load project
            var project = await _projectRepository.GetByIdAsync(projectId, ct);
            if (project is null || !project.HasGitHubConfig)
            {
                return;
            }

            // Extract GitHub config (guaranteed non-null by HasGitHubConfig check)
            var installationId = project.GitHubInstallationId ?? throw new InvalidOperationException("GitHub config inconsistent");
            var owner = project.GitHubRepoOwner ?? throw new InvalidOperationException("GitHub config inconsistent");
            var repoName = project.GitHubRepoName ?? throw new InvalidOperationException("GitHub config inconsistent");

            // Step 2: Map path
            var targetPath = MapPath(filePath);
            if (targetPath is null)
            {
                _logger.LogWarning("Unmapped path {FilePath} — skipping push", filePath);
                return;
            }

            // Step 3: Determine if binary and read content
            var isBinary = IsContentTypeBinary(contentType);
            byte[] content;

            if (isBinary)
            {
                var binaryContent = await _artefactStorageService.GetBinaryContentAsync(s3Key, ct);
                if (binaryContent is null)
                {
                    await _pushFailureLogRepository.AddAsync(
                        new PushFailureLog(projectId, artefactId, filePath, "S3 binary content returned null", _timeProvider),
                        ct);
                    return;
                }
                content = binaryContent;
            }
            else
            {
                var textContent = await _artefactStorageService.GetContentAsync(s3Key, ct);
                if (textContent is null)
                {
                    await _pushFailureLogRepository.AddAsync(
                        new PushFailureLog(projectId, artefactId, filePath, "S3 text content returned null", _timeProvider),
                        ct);
                    return;
                }
                content = Encoding.UTF8.GetBytes(textContent);
            }

            // Step 4: Mint token
            var token = await _tokenService.GetInstallationTokenAsync(
                installationId, ct);

            // Step 5: Resolve existing SHA
            var existingSha = await _contentsService.GetFileShaAsync(
                token,
                owner,
                repoName,
                targetPath,
                ct);

            // Step 6: Build commit message
            var appVersion = _versionProvider.GetVersion();
            var commitMessage =
                $"feat(artefacts): publish {filePath} v{version}\n\n" +
                $"Triggered-By: {triggeredBy}\n" +
                $"Approved-By: {triggeredBy}\n" +
                $"Project-ID: {projectId}\n" +
                $"Artefact-ID: {artefactId}\n" +
                $"Genesis-AI-Version: {appVersion}";

            // Step 7: Push
            await _contentsService.PushFileAsync(
                token,
                project.GitHubRepoOwner!,
                project.GitHubRepoName!,
                targetPath,
                content,
                commitMessage,
                existingSha,
                ct);
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

    private static string? MapPath(string filePath)
    {
        foreach (var (prefix, target) in PathPrefixMap)
        {
            if (filePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return target + filePath[prefix.Length..];
            }
        }
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
