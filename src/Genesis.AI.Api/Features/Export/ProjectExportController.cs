using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genesis.AI.Api.Features.Export;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/export")]
[Authorize(Policy = AuthorisationPolicies.ProjectRead)]
[Produces("application/json")]
[Consumes("application/json")]
public class ProjectExportController : ControllerBase
{
    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProjectExportController> _logger = NullLogger<ProjectExportController>.Instance;

    public ProjectExportController(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Exports all artefacts for a project as a zip file, organised by stage.
    /// Includes a prototype prompt file for loading in VS Code.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return NotFound(ApiErrorResponse.Create(
                "404",
                "Project not found",
                $"No project found with ID '{projectId}'."));
        }

        using var memoryStream = new MemoryStream();
        var now = _timeProvider.GetUtcNow();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            await WriteReadmeAsync(archive, project, now);
            try
            {
                await WriteStageArtefactsAsync(archive, project, cancellationToken);
                await WritePrototypePromptAsync(archive, project, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export failed for project {ProjectId}", projectId);
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Export failed",
                    Detail = "An error occurred generating the export."
                };
                problemDetails.Extensions["userMessage"] = "Export failed. Please try again.";
                return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
            }
        }

        memoryStream.Position = 0;
        var fileName = $"{project.Code.ToLowerInvariant()}-export-{now:yyyyMMdd}.zip";

        return File(memoryStream.ToArray(), "application/zip", fileName);
    }

    private static async Task WriteReadmeAsync(ZipArchive archive, Domain.AggregatesModel.ProjectAggregate.Project project, DateTimeOffset exportedAt)
    {
        var readme = archive.CreateEntry("README.md");
        await using var writer = new StreamWriter(readme.Open(), Encoding.UTF8);

        await writer.WriteLineAsync($"# {project.Name}");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"**Code:** {project.Code}");
        await writer.WriteLineAsync($"**Compliance Domain:** {project.ComplianceDomain}");
        await writer.WriteLineAsync($"**Exported:** {exportedAt:yyyy-MM-dd HH:mm:ss} UTC");
        await writer.WriteLineAsync();

        if (!string.IsNullOrWhiteSpace(project.Description))
        {
            await writer.WriteLineAsync("## Description");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync(project.Description);
            await writer.WriteLineAsync();
        }

        await writer.WriteLineAsync("## Pipeline Stages");
        await writer.WriteLineAsync();
        foreach (var stage in project.PipelineStages.OrderBy(stage => stage.SortOrder))
        {
            var statusEmoji = stage.Status.ToString() switch
            {
                "Complete" => "✅",
                "InProgress" => "🔄",
                "Blocked" => "🚫",
                _ => "⬜"
            };
            await writer.WriteLineAsync($"- {statusEmoji} {stage.StageType} ({stage.Status})");
        }
    }

    private async Task WriteStageArtefactsAsync(ZipArchive archive, Domain.AggregatesModel.ProjectAggregate.Project project, CancellationToken cancellationToken)
    {
        var artefacts = await _artefactRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        if (artefacts.Count == 0)
            return;

        // Group by file path and take latest version only
        var latestByFile = artefacts
            .GroupBy(artefact => artefact.FilePath)
            .Select(group => group.OrderByDescending(artefact => artefact.Version).First())
            .OrderBy(artefact => artefact.FilePath);

        foreach (var artefact in latestByFile)
        {
            var entryPath = "artefacts/" + artefact.FilePath.TrimStart('/');
            var entry = archive.CreateEntry(entryPath);

            if (IsBinaryContent(artefact.ContentType))
            {
                var binaryContent = await _artefactStorageService.GetBinaryContentAsync(artefact.S3Key, cancellationToken);
                if (binaryContent is { Length: > 0 })
                {
                    await using var binaryStream = entry.Open();
                    await binaryStream.WriteAsync(binaryContent, cancellationToken);
                }

                continue;
            }

            var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);

            if (!string.IsNullOrEmpty(content))
            {
                await using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                await writer.WriteAsync(content);
            }
        }
    }

    private static bool IsBinaryContent(string contentType)
    {
        return !IsTextContent(contentType);
    }

    /// <summary>
    /// Returns true only for content types we can safely round-trip as UTF-8 text.
    /// Everything else (office documents, PDFs, images, archives) is treated as
    /// binary. We deliberately avoid a bare "contains xml" check because Office
    /// Open XML types such as the xlsx spreadsheet content type contain the
    /// substring "openxml" yet are binary zip containers.
    /// </summary>
    private static bool IsTextContent(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        // Strip any parameters (e.g. "; charset=utf-8") and normalise.
        var mediaType = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();

        if (mediaType.StartsWith("text/", StringComparison.Ordinal))
            return true;

        if (mediaType.EndsWith("+json", StringComparison.Ordinal)
            || mediaType.EndsWith("+xml", StringComparison.Ordinal))
            return true;

        return mediaType switch
        {
            "application/json" => true,
            "application/xml" => true,
            "application/markdown" => true,
            "application/yaml" => true,
            "application/x-yaml" => true,
            "application/csv" => true,
            _ => false
        };
    }

    private async Task WritePrototypePromptAsync(ZipArchive archive, Domain.AggregatesModel.ProjectAggregate.Project project, CancellationToken cancellationToken)
    {
        var prototypePrompt = archive.CreateEntry(".vscode/prototype-prompt.md");
        await using var writer = new StreamWriter(prototypePrompt.Open(), Encoding.UTF8);

        var template = $"""
            # Prototype Generation Prompt

            Use this prompt with your AI coding assistant (e.g. GitHub Copilot) to generate a working prototype from the requirements in this export.

            ## Instructions

            1. Open this folder in VS Code
            2. Load the requirements from the `01-requirements_discovery/` folder
            3. Use the following prompt with your AI assistant:

            ---

            ```
            You are building a clickable static prototype for "{project.Name}".

            Read all the requirement files in the `01-requirements_discovery/` folder.
            Build a single-page HTML/CSS/JS prototype (no build tools, no frameworks) that:
            - Demonstrates the core user flows described in the requirements
            - Uses realistic mock data
            - Is navigable with clickable elements
            - Follows NHS design patterns (NHS Blue #005EB8, 8px grid, accessible)
            - Can be opened directly in a browser (file:// protocol)

            Output the prototype as `prototype/index.html` with embedded CSS and JS.
            ```

            ---

            ## What's included in this export

            | Folder | Contents |
            |--------|----------|
            """;

        await writer.WriteAsync(string.Join('\n', template.Split('\n').Select(line => line.TrimStart())));
        await WriteExportTableRowsAsync(writer, project, cancellationToken);
    }

    private async Task WriteExportTableRowsAsync(StreamWriter writer, Domain.AggregatesModel.ProjectAggregate.Project project, CancellationToken cancellationToken)
    {
        var artefacts = await _artefactRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        if (artefacts.Count > 0)
        {
            var fileCount = artefacts.GroupBy(artefact => artefact.FilePath).Count();
            await writer.WriteLineAsync($"| `artefacts/` | {fileCount} artefact(s) |");
        }
    }

    private static string ToKebabCase(string value)
    {
        return Regex.Replace(value, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
    }
}
