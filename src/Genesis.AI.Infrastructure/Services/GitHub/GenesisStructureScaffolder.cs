using System.Text;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services.GitHub;

public sealed class GenesisStructureScaffolder : IGenesisStructureScaffolder
{
    private readonly IProjectRepository _projectRepository;
    private readonly IGitHubTokenService _tokenService;
    private readonly IGitHubContentsService _contentsService;
    private readonly IPushFailureLogRepository _pushFailureLogRepository;
    private readonly ICodeownersGenerator _codeownersGenerator;
    private readonly IProjectMarkdownGenerator _markdownGenerator;
    private readonly IAssemblyVersionProvider _versionProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GenesisStructureScaffolder> _logger;

    public GenesisStructureScaffolder(
        IProjectRepository projectRepository,
        IGitHubTokenService tokenService,
        IGitHubContentsService contentsService,
        IPushFailureLogRepository pushFailureLogRepository,
        ICodeownersGenerator codeownersGenerator,
        IProjectMarkdownGenerator markdownGenerator,
        IAssemblyVersionProvider versionProvider,
        TimeProvider timeProvider,
        ILogger<GenesisStructureScaffolder> logger)
    {
        _projectRepository = projectRepository;
        _tokenService = tokenService;
        _contentsService = contentsService;
        _pushFailureLogRepository = pushFailureLogRepository;
        _codeownersGenerator = codeownersGenerator;
        _markdownGenerator = markdownGenerator;
        _versionProvider = versionProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ScaffoldResult> ScaffoldAsync(Guid projectId, string triggeredBy, CancellationToken ct)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, ct).ConfigureAwait(false);
        if (project is null)
        {
            _logger.LogWarning("GenesisStructureScaffolder: project {ProjectId} not found — skipping scaffold.", projectId);
            return ScaffoldResult.Failure("Project not found for GitHub scaffold.");
        }

        if (!project.HasGitHubConfig)
        {
            _logger.LogInformation("GenesisStructureScaffolder: project {ProjectId} has no GitHub config — skipping scaffold.", projectId);
            return ScaffoldResult.Failure("GitHub is not configured for this project.");
        }

        var token = await _tokenService.GetInstallationTokenAsync(project.GitHubInstallationId!, ct).ConfigureAwait(false);

        var alreadyScaffolded = await SentinelFileExistsAsync(
            token,
            project.GitHubRepoOwner!,
            project.GitHubRepoName!,
            ct).ConfigureAwait(false);

        if (alreadyScaffolded)
        {
            _logger.LogInformation("GenesisStructureScaffolder: project {ProjectId} already scaffolded — skipping.", projectId);
            return ScaffoldResult.Success();
        }

        var version = _versionProvider.GetVersion();
        var commitMessage =
            $"chore(genesis): scaffold .genesis/ structure\n\n" +
            $"Provisioned-By: genesis-ai[bot]\n" +
            $"Triggered-By: {triggeredBy}\n" +
            $"Project-ID: {projectId}\n" +
            $"Genesis-AI-Version: {version}";

        var files = new (string Path, byte[] Content)[]
        {
            (".genesis/requirements/.gitkeep",    Array.Empty<byte>()),
            (".genesis/architecture/.gitkeep",    Array.Empty<byte>()),
            (".genesis/clinical-safety/.gitkeep", Array.Empty<byte>()),
            (".genesis/ig/.gitkeep",              Array.Empty<byte>()),
            (".genesis/security/.gitkeep",        Array.Empty<byte>()),
            (".genesis/prototype/.gitkeep",       Array.Empty<byte>()),
            (".genesis/session-close/.gitkeep",   Array.Empty<byte>()),
            (".genesis/project/.gitkeep",         Array.Empty<byte>()),
            (".genesis/CODEOWNERS",               Encoding.UTF8.GetBytes(_codeownersGenerator.Generate())),
            (".genesis/project/PROJECT.md",       Encoding.UTF8.GetBytes(_markdownGenerator.Generate(project))),
            (".genesis/.gitkeep",                 Array.Empty<byte>()),
        };

        return await PushScaffoldFilesAsync(
            projectId,
            project.GitHubRepoOwner!,
            project.GitHubRepoName!,
            token,
            files,
            commitMessage,
            ct).ConfigureAwait(false);
    }

    private Task<bool> SentinelFileExistsAsync(
        string token,
        string owner,
        string repoName,
        CancellationToken ct)
    {
        return _contentsService.FileExistsAsync(
            token,
            owner,
            repoName,
            ".genesis/.gitkeep",
            ct);
    }

    private async Task<ScaffoldResult> PushScaffoldFilesAsync(
        Guid projectId,
        string owner,
        string repoName,
        string token,
        IReadOnlyList<(string Path, byte[] Content)> files,
        string commitMessage,
        CancellationToken ct)
    {
        try
        {
            foreach (var (path, content) in files)
            {
                await _contentsService.PushFileAsync(
                    token,
                    owner,
                    repoName,
                    path,
                    content,
                    commitMessage,
                    null,
                    ct).ConfigureAwait(false);
            }

            return ScaffoldResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GenesisStructureScaffolder: push failed for project {ProjectId} — scaffold incomplete.", projectId);
            await _pushFailureLogRepository.AddAsync(
                new Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate.PushFailureLog(
                    projectId,
                    Guid.Empty,
                    ".genesis/scaffold",
                    ex.Message,
                    _timeProvider),
                ct).ConfigureAwait(false);

            return ScaffoldResult.Failure(ex.Message);
        }
    }
}
