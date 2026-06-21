using AngleSharp;
using AngleSharp.Html.Parser;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class PrototypeFragmentMigrationService : IPrototypeFragmentMigrationService
{
    private const string ShellFragmentPath = "prototype/fragments/_shell.html";
    private const string StylesFragmentPath = "prototype/fragments/_styles.css";
    private const string AppJsFragmentPath = "prototype/fragments/_app.js";
    private const string ScreensFragmentPath = "prototype/fragments/screen-01-legacy.html";
    private const string IndexHtmlPath = "prototype/index.html";

    private readonly IArtefactStorageService _storageService;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IPrototypeAssemblyService _assemblyService;
    private readonly TimeProvider _timeProvider;

    public PrototypeFragmentMigrationService(
        IArtefactStorageService storageService,
        IArtefactRepository artefactRepository,
        IPrototypeAssemblyService assemblyService,
        TimeProvider timeProvider)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _assemblyService = assemblyService ?? throw new ArgumentNullException(nameof(assemblyService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PrototypeFragmentMigrationResult> MigrateIfNeededAsync(
        Guid projectId,
        string initiatedBy,
        CancellationToken cancellationToken)
    {
        // Detection: _shell.html existence is the single binary signal
        var shellArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            projectId, ShellFragmentPath, cancellationToken);

        if (shellArtefact is not null)
        {
            // Shell exists — check if it needs metadata injection (legacy migration without metadata)
            var shellContent = await _storageService.GetContentAsync(
                shellArtefact.S3Key, cancellationToken);

            if (!string.IsNullOrWhiteSpace(shellContent) &&
                !shellContent.Contains("prototype-metadata", StringComparison.OrdinalIgnoreCase))
            {
                await InjectMetadataIntoShellAsync(projectId, shellArtefact, shellContent, cancellationToken);
                return new PrototypeFragmentMigrationResult(Migrated: true);
            }

            return new PrototypeFragmentMigrationResult(Migrated: false);
        }

        // No shell — check if index.html exists
        var indexArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            projectId, IndexHtmlPath, cancellationToken);

        if (indexArtefact is null)
        {
            return new PrototypeFragmentMigrationResult(Migrated: false);
        }

        // Monolith exists, no fragments — migrate
        var html = await _storageService.GetContentAsync(indexArtefact.S3Key, cancellationToken) ?? string.Empty;
        await MigrateAsync(projectId, html, initiatedBy, cancellationToken);
        await _assemblyService.AssemblePrototypeAsync(projectId, cancellationToken);

        return new PrototypeFragmentMigrationResult(Migrated: true);
    }

    private async Task MigrateAsync(
        Guid projectId,
        string html,
        string initiatedBy,
        CancellationToken cancellationToken)
    {
        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var parser = context.GetService<IHtmlParser>()!;
        var document = await parser.ParseDocumentAsync(html, cancellationToken);

        // Extract <style> content
        var styleElement = document.QuerySelector("style");
        var cssContent = styleElement?.TextContent ?? string.Empty;

        // Extract <script> content
        var scriptElement = document.QuerySelector("script");
        var jsContent = scriptElement?.TextContent ?? string.Empty;

        // Extract all .screen divs
        var screenElements = document.QuerySelectorAll(".screen");
        var screensContent = string.Concat(screenElements.Select(element => element.OuterHtml));

        // Remove screens, style, and script from document to get shell
        foreach (var screen in screenElements)
        {
            screen.Remove();
        }

        styleElement?.Remove();
        scriptElement?.Remove();

        var shellContent = document.DocumentElement?.OuterHtml ?? string.Empty;

        // Inject GENESIS markers required by assembly service to stitch fragments back together.
        // The migrated shell has no markers — inject them at the correct positions.
        if (!shellContent.Contains("<!-- GENESIS:STYLES -->", StringComparison.Ordinal))
        {
            shellContent = shellContent.Replace(
                "</head>", "<!-- GENESIS:STYLES -->\n</head>", StringComparison.OrdinalIgnoreCase);
        }

        if (!shellContent.Contains("<!-- GENESIS:SCREENS -->", StringComparison.Ordinal))
        {
            shellContent = shellContent.Replace(
                "</body>", "<!-- GENESIS:SCREENS -->\n<!-- GENESIS:NAV -->\n<!-- GENESIS:DATA -->\n<!-- GENESIS:APP -->\n</body>",
                StringComparison.OrdinalIgnoreCase);
        }

        // Inject prototype-metadata block if missing — required by assembly validation contract.
        if (!shellContent.Contains("prototype-metadata", StringComparison.OrdinalIgnoreCase))
        {
            var metadataStub = "<script id=\"prototype-metadata\" type=\"application/json\">\n"
                + "{\"contractVersion\":\"1.0\",\"stageCode\":\"prototype\",\"prototypeOnly\":true,"
                + "\"generatedAtUtc\":\"2026-01-01T00:00:00Z\",\"requirementsCovered\":[],"
                + "\"flows\":[],\"privacySafetyConstraints\":[\"no real data\"]}\n"
                + "</script>";
            shellContent = shellContent.Replace("</head>", metadataStub + "\n</head>",
                StringComparison.OrdinalIgnoreCase);
        }

        // Inject prototype banner if missing — required by assembly validation contract.
        if (!shellContent.Contains("⚠️ PROTOTYPE ONLY", StringComparison.Ordinal))
        {
            var bannerHtml = $"<div class=\"proto-banner\">{PrototypeBanner}</div>";
            shellContent = shellContent.Replace("<body>", "<body>\n" + bannerHtml,
                StringComparison.OrdinalIgnoreCase);
        }

        // Save all four fragments
        await SaveFragmentAsync(projectId, StylesFragmentPath, cssContent, "text/css", initiatedBy, cancellationToken);
        await SaveFragmentAsync(projectId, AppJsFragmentPath, jsContent, "application/javascript", initiatedBy, cancellationToken);
        await SaveFragmentAsync(projectId, ShellFragmentPath, shellContent, "text/html", initiatedBy, cancellationToken);
        await SaveFragmentAsync(projectId, ScreensFragmentPath, screensContent, "text/html", initiatedBy, cancellationToken);

        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    private const string PrototypeBanner = "⚠️ PROTOTYPE ONLY — Requirements validation artefact. Not for production use.";

    private async Task InjectMetadataIntoShellAsync(
        Guid projectId,
        Genesis.AI.Domain.AggregatesModel.ArtefactAggregate.Artefact shellArtefact,
        string shellContent,
        CancellationToken cancellationToken)
    {
        var updatedShell = shellContent;

        // Inject prototype-metadata block if missing
        if (!updatedShell.Contains("prototype-metadata", StringComparison.OrdinalIgnoreCase))
        {
            var metadataStub = "<script id=\"prototype-metadata\" type=\"application/json\">\n"
                + "{\"contractVersion\":\"1.0\",\"stageCode\":\"prototype\",\"prototypeOnly\":true,"
                + "\"generatedAtUtc\":\"2026-01-01T00:00:00Z\",\"requirementsCovered\":[],"
                + "\"flows\":[],\"privacySafetyConstraints\":[\"no real data\"]}\n"
                + "</script>";
            updatedShell = updatedShell.Replace("</head>", metadataStub + "\n</head>",
                StringComparison.OrdinalIgnoreCase);
        }

        // Inject prototype banner if missing
        if (!updatedShell.Contains("⚠️ PROTOTYPE ONLY", StringComparison.Ordinal))
        {
            var bannerHtml = $"<div class=\"proto-banner\">{PrototypeBanner}</div>";
            updatedShell = updatedShell.Replace("<body>", "<body>\n" + bannerHtml,
                StringComparison.OrdinalIgnoreCase);
        }

        await SaveFragmentAsync(projectId, ShellFragmentPath, updatedShell, "text/html",
            "system-migration", cancellationToken);

        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        await _assemblyService.AssemblePrototypeAsync(projectId, cancellationToken);
    }

    private async Task SaveFragmentAsync(
        Guid projectId,
        string filePath,
        string content,
        string contentType,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
            projectId, filePath, cancellationToken);

        var s3Key = await _storageService.SaveContentAsync(
            projectId, filePath, nextVersion, content, contentType, cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            projectId,
            nextVersion,
            filePath,
            s3Key,
            contentType,
            System.Text.Encoding.UTF8.GetByteCount(content),
            createdBy,
            _timeProvider,
            true);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
    }
}
