using System.Globalization;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.SecurityReviewReport;
using MediatR;

namespace Genesis.AI.Domain.Commands.GenerateSecurityReviewReport;

/// <summary>
/// Handles <see cref="GenerateSecurityReviewReportCommand"/>: loads the project
/// and security source artefacts, renders the workbook report, and persists it as
/// a versioned binary artefact under feedback/.
/// </summary>
public sealed class GenerateSecurityReviewReportCommandHandler
    : IRequestHandler<GenerateSecurityReviewReportCommand, GenerateSecurityReviewReportResult>
{
    private const string SecurityAssuranceDataFilePath = "output/SECURITY_ASSURANCE_DATA.json";
    private const string SdpEvidenceFilePath = "output/SDP_EVIDENCE.json";
    private const string SpreadsheetContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly ISecurityReviewReportBuilder _reportBuilder;
    private readonly TimeProvider _timeProvider;

    public GenerateSecurityReviewReportCommandHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        ISecurityReviewReportBuilder reportBuilder,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _reportBuilder = reportBuilder ?? throw new ArgumentNullException(nameof(reportBuilder));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GenerateSecurityReviewReportResult> Handle(
        GenerateSecurityReviewReportCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return GenerateSecurityReviewReportResult.Failure(
                GenerateSecurityReviewReportStatus.ProjectNotFound,
                $"No project found with ID '{request.ProjectId}'.");
        }

        var (failure, securityAssuranceJson, sdpEvidenceJson) = await LoadSourceDataAsync(request.ProjectId, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        byte[] content;
        try
        {
            content = _reportBuilder.Build(securityAssuranceJson, sdpEvidenceJson);
        }
        catch (Exception ex)
        {
            return GenerateSecurityReviewReportResult.Failure(
                GenerateSecurityReviewReportStatus.DataInvalid,
                $"Security review report generation failed: {ex.Message}");
        }

        var generatedAt = _timeProvider.GetUtcNow();
        var fileStamp = generatedAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var downloadFileName = $"{project.Code.ToLowerInvariant()}-security-review-report-{fileStamp}.xlsx";

        var artefactId = await PersistAsync(request, content, cancellationToken);

        return GenerateSecurityReviewReportResult.Succeeded(content, downloadFileName, artefactId);
    }

    private async Task<(GenerateSecurityReviewReportResult? Failure, string SecurityAssuranceJson, string SdpEvidenceJson)> LoadSourceDataAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var securityAssuranceArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            projectId,
            SecurityAssuranceDataFilePath,
            cancellationToken);

        if (securityAssuranceArtefact is null)
        {
            return (GenerateSecurityReviewReportResult.Failure(
                GenerateSecurityReviewReportStatus.SecurityAssuranceDataNotFound,
                $"No security assurance data artefact ('{SecurityAssuranceDataFilePath}') exists for this project."), string.Empty, string.Empty);
        }

        var sdpEvidenceArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            projectId,
            SdpEvidenceFilePath,
            cancellationToken);

        if (sdpEvidenceArtefact is null)
        {
            return (GenerateSecurityReviewReportResult.Failure(
                GenerateSecurityReviewReportStatus.SdpEvidenceNotFound,
                $"No SDP evidence artefact ('{SdpEvidenceFilePath}') exists for this project."), string.Empty, string.Empty);
        }

        var securityAssuranceJson = await _artefactStorageService.GetContentAsync(
            securityAssuranceArtefact.S3Key,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(securityAssuranceJson))
        {
            return (GenerateSecurityReviewReportResult.Failure(
                GenerateSecurityReviewReportStatus.DataInvalid,
                "The security assurance data artefact is empty."), string.Empty, string.Empty);
        }

        var sdpEvidenceJson = await _artefactStorageService.GetContentAsync(
            sdpEvidenceArtefact.S3Key,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(sdpEvidenceJson))
        {
            return (GenerateSecurityReviewReportResult.Failure(
                GenerateSecurityReviewReportStatus.DataInvalid,
                "The SDP evidence artefact is empty."), string.Empty, string.Empty);
        }

        return (null, securityAssuranceJson, sdpEvidenceJson);
    }

    private async Task<Guid> PersistAsync(
        GenerateSecurityReviewReportCommand request,
        byte[] content,
        CancellationToken cancellationToken)
    {
        const string filePath = "feedback/SECURITY_REVIEW_REPORT.xlsx";

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
                SpreadsheetContentType,
                cancellationToken);

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
            request.ProjectId,
            filePath,
            1,
            content,
            SpreadsheetContentType,
            cancellationToken);

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
