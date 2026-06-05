using System.Globalization;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.HazardLog;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.GenerateHazardLog;

/// <summary>
/// Handles <see cref="GenerateHazardLogCommand"/>: loads the project and its hazard
/// registry, parses the registry into structured hazards, renders the IF678 hazard
/// log spreadsheet, and persists it as a versioned binary artefact under
/// <c>feedback/</c>.
/// </summary>
public class GenerateHazardLogCommandHandler : IRequestHandler<GenerateHazardLogCommand, GenerateHazardLogResult>
{
    // guardrail:skip=AUTH-002:Artefact file path, not an OAuth scope.
    private const string RegistryFilePath = "requirements/HAZARD-REGISTRY.md";
    private const string SpreadsheetContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IHazardRegistryParser _registryParser;
    private readonly IHazardLogExcelBuilder _excelBuilder;
    private readonly TimeProvider _timeProvider;

    public GenerateHazardLogCommandHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IHazardRegistryParser registryParser,
        IHazardLogExcelBuilder excelBuilder,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _registryParser = registryParser ?? throw new ArgumentNullException(nameof(registryParser));
        _excelBuilder = excelBuilder ?? throw new ArgumentNullException(nameof(excelBuilder));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GenerateHazardLogResult> Handle(
        GenerateHazardLogCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return GenerateHazardLogResult.Failure(
                GenerateHazardLogStatus.ProjectNotFound,
                $"No project found with ID '{request.ProjectId}'.");
        }

        var registryArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId, RegistryFilePath, cancellationToken);
        if (registryArtefact is null)
        {
            return GenerateHazardLogResult.Failure(
                GenerateHazardLogStatus.RegistryNotFound,
                $"No hazard registry ('{RegistryFilePath}') exists for this project. "
                + "Complete the Clinical Safety stage to produce a hazard registry first.");
        }

        var registryContent = await _artefactStorageService.GetContentAsync(
            registryArtefact.S3Key, cancellationToken);
        var hazards = string.IsNullOrWhiteSpace(registryContent)
            ? []
            : _registryParser.Parse(registryContent);

        if (hazards.Count == 0)
        {
            return GenerateHazardLogResult.Failure(
                GenerateHazardLogStatus.RegistryNotFound,
                "The hazard registry contains no hazards to export.");
        }

        var generatedAt = _timeProvider.GetUtcNow();
        var displayDate = generatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var fileStamp = generatedAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var content = _excelBuilder.Build(hazards, project.Name, displayDate);

        var artefactId = await PersistAsync(request, project.Code, content, cancellationToken);

        var downloadFileName = $"{project.Code.ToLowerInvariant()}-hazard-log-{fileStamp}.xlsx";

        return GenerateHazardLogResult.Succeeded(content, downloadFileName, artefactId, hazards.Count);
    }

    private async Task<Guid> PersistAsync(
        GenerateHazardLogCommand request,
        string projectCode,
        byte[] content,
        CancellationToken cancellationToken)
    {
        // Stable file path (no date stamp): the hazard log is a derived artefact that is
        // regenerated in place. Re-running keeps a single artefact row whose version
        // climbs (v1 → v2 → … → vN) rather than creating a new file each time.
        var filePath = $"feedback/HAZARD_LOG_{projectCode}.xlsx";

        var existing = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId, filePath, cancellationToken);

        if (existing is not null)
        {
            var nextVersion = existing.Version + 1;
            var existingStorageKey = await _artefactStorageService.SaveBinaryContentAsync(
                request.ProjectId, filePath, nextVersion, content, SpreadsheetContentType, cancellationToken);

            var tracked = await _artefactRepository.GetByIdAsync(existing.Id, cancellationToken);
            tracked!.ReplaceContent(
                nextVersion,
                existingStorageKey,
                SpreadsheetContentType,
                content.Length,
                request.UserId,
                _timeProvider);

            await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return tracked.Id;
        }

        var storageKey = await _artefactStorageService.SaveBinaryContentAsync(
            request.ProjectId, filePath, 1, content, SpreadsheetContentType, cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            request.ProjectId,
            1,
            filePath,
            storageKey,
            SpreadsheetContentType,
            content.Length,
            request.UserId,
            _timeProvider);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return artefact.Id;
    }
}
