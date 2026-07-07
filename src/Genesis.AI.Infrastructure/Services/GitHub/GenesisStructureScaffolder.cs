using System.Text;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services.GitHub;

public sealed class GenesisStructureScaffolder : IGenesisStructureScaffolder
{
    private readonly IProjectRepository _projectRepository;
    private readonly IGitHubTokenService _tokenService;
    private readonly IGitHubContentsService _contentsService;
    private readonly ICodeownersGenerator _codeownersGenerator;
    private readonly IProjectMarkdownGenerator _markdownGenerator;
    private readonly IAssemblyVersionProvider _versionProvider;
    private readonly ILogger<GenesisStructureScaffolder> _logger;

    public GenesisStructureScaffolder(
        IProjectRepository projectRepository,
        IGitHubTokenService tokenService,
        IGitHubContentsService contentsService,
        ICodeownersGenerator codeownersGenerator,
        IProjectMarkdownGenerator markdownGenerator,
        IAssemblyVersionProvider versionProvider,
        ILogger<GenesisStructureScaffolder> logger)
    {
        _projectRepository = projectRepository;
        _tokenService = tokenService;
        _contentsService = contentsService;
        _codeownersGenerator = codeownersGenerator;
        _markdownGenerator = markdownGenerator;
        _versionProvider = versionProvider;
        _logger = logger;
    }

    public async Task ScaffoldAsync(Guid projectId, string userErn, CancellationToken ct)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, ct).ConfigureAwait(false);
        if (project is null)
        {
            _logger.LogWarning("GenesisStructureScaffolder: project {ProjectId} not found — skipping scaffold.", projectId);
            return;
        }

        if (!project.HasGitHubConfig)
        {
            _logger.LogInformation("GenesisStructureScaffolder: project {ProjectId} has no GitHub config — skipping scaffold.", projectId);
            return;
        }

        var token = await _tokenService.GetInstallationTokenAsync(project.GitHubInstallationId!, ct).ConfigureAwait(false);

        var alreadyScaffolded = await _contentsService.FileExistsAsync(
            token, project.GitHubRepoOwner!, project.GitHubRepoName!,
            ".genesis/.gitkeep", ct).ConfigureAwait(false);

        if (alreadyScaffolded)
        {
            _logger.LogInformation("GenesisStructureScaffolder: project {ProjectId} already scaffolded — skipping.", projectId);
            return;
        }

        var version = _versionProvider.GetVersion();
        var commitMessage =
            $"chore(genesis): scaffold .genesis/ structure\n\n" +
            $"Provisioned-By: genesis-ai[bot]\n" +
            $"Triggered-By: {userErn}\n" +
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

        try
        {
            foreach (var (path, content) in files)
            {
                await _contentsService.PushFileAsync(
                    token,
                    project.GitHubRepoOwner!,
                    project.GitHubRepoName!,
                    path,
                    content,
                    commitMessage,
                    null,
                    ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GenesisStructureScaffolder: push failed for project {ProjectId} — scaffold incomplete.", projectId);
        }
    }
}
