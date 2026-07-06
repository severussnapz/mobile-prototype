using System.Globalization;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Dpia;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.GenerateDpiaReport;

/// <summary>
/// Handles <see cref="GenerateDpiaReportCommand"/>: loads the project and DPIA
/// JSON artefact, renders the PR1625 Word report, and persists it as a versioned
/// binary artefact under feedback/.
/// </summary>
public sealed class GenerateDpiaReportCommandHandler
    : IRequestHandler<GenerateDpiaReportCommand, GenerateDpiaReportResult>
{
    private const string DpiaDataFilePath = "output/PR1625_DPIA_DATA.json";
    private const string WordContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IDpiaDocxBuilder _docxBuilder;
    private readonly TimeProvider _timeProvider;

    public GenerateDpiaReportCommandHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IDpiaDocxBuilder docxBuilder,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _docxBuilder = docxBuilder ?? throw new ArgumentNullException(nameof(docxBuilder));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GenerateDpiaReportResult> Handle(
        GenerateDpiaReportCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return GenerateDpiaReportResult.Failure(
                GenerateDpiaReportStatus.ProjectNotFound,
                $"No project found with ID '{request.ProjectId}'.");
        }

        var sourceArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId,
            DpiaDataFilePath,
            cancellationToken);

        if (sourceArtefact is null)
        {
            return GenerateDpiaReportResult.Failure(
                GenerateDpiaReportStatus.DataNotFound,
                $"No DPIA data artefact ('{DpiaDataFilePath}') exists for this project.");
        }

        var dpiaJson = await _artefactStorageService.GetContentAsync(sourceArtefact.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(dpiaJson))
        {
            return GenerateDpiaReportResult.Failure(
                GenerateDpiaReportStatus.DataInvalid,
                "The DPIA data artefact is empty.");
        }

        byte[] content;
        try
        {
            content = _docxBuilder.Build(dpiaJson);
        }
        catch (Exception ex)
        {
            return GenerateDpiaReportResult.Failure(
                GenerateDpiaReportStatus.DataInvalid,
                $"DPIA report generation failed: {ex.Message}");
        }

        var generatedAt = _timeProvider.GetUtcNow();
        var fileStamp = generatedAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var downloadFileName = $"{project.Code.ToLowerInvariant()}-data-protection-impact-assessment-{fileStamp}.docx";

        var artefactId = await PersistAsync(request, project.Code, content, cancellationToken);

        return GenerateDpiaReportResult.Succeeded(content, downloadFileName, artefactId);
    }

    private async Task<Guid> PersistAsync(
        GenerateDpiaReportCommand request,
        string projectCode,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var filePath = $"feedback/PR1625_DPIA_{projectCode}.docx";

        var existing = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId,
            filePath,
            cancellationToken);

        if (existing is not null)
        {
            var nextVersion = existing.Version + 1;
            var existingStorageKey = await _artefactStorageService.SaveBinaryContentAsync(
                request.ProjectId,
                filePath,
                nextVersion,
                content,
                WordContentType,
                cancellationToken);

            var tracked = await _artefactRepository.GetByIdAsync(existing.Id, cancellationToken);
            tracked!.ReplaceContent(
                nextVersion,
                existingStorageKey,
                WordContentType,
                content.Length,
                request.UserId,
                _timeProvider);

            await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return tracked.Id;
        }

        var storageKey = await _artefactStorageService.SaveBinaryContentAsync(
            request.ProjectId,
            filePath,
            1,
            content,
            WordContentType,
            cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            request.ProjectId,
            1,
            filePath,
            storageKey,
            WordContentType,
            content.Length,
            request.UserId, _timeProvider, true);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return artefact.Id;
    }
}
