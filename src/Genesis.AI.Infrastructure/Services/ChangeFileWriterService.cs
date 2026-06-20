using System.Globalization;
using System.Text;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class ChangeFileWriterService
{
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;

    public ChangeFileWriterService(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        TimeProvider timeProvider)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task WriteChangeFileAsync(
        RequirementChange change,
        CancellationToken cancellationToken)
    {
        var changeId = FormatChangeId(change.Id);
        var filePath = $"changes/{changeId}.md";
        var content = BuildChangeFileContent(change, changeId);

        var existing = await _artefactRepository.GetByProjectAndFilePathAsync(
            change.ProjectId, filePath, cancellationToken);

        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
            change.ProjectId, filePath, cancellationToken);

        var s3Key = await _artefactStorageService.SaveContentAsync(
            change.ProjectId,
            filePath,
            nextVersion,
            content,
            "text/markdown",
            cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            change.ProjectId,
            nextVersion,
            filePath,
            s3Key,
            "text/markdown",
            Encoding.UTF8.GetByteCount(content),
            change.ApprovedBy ?? change.CreatedBy,
            _timeProvider,
            true);

        if (existing is null)
        {
            await _artefactRepository.AddAsync(artefact, cancellationToken);
        }

        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string BuildChangeFileContent(RequirementChange change, string changeId)
    {
        var builder = new StringBuilder();

        builder.AppendLine(CultureInfo.InvariantCulture, $"# {changeId}");
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Raised by: {change.RaisingPipeline}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Type: {change.ChangeType.ToString().ToUpperInvariant()}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"REQ: {change.ReqId}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Status: {FormatStatus(change)}");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(change.ApprovedAcText))
        {
            builder.AppendLine("## Change");
            builder.AppendLine();
            if (change.HumanEdited && !string.IsNullOrWhiteSpace(change.ProposedAcText))
            {
                builder.AppendLine("**As proposed by agent:**");
                builder.AppendLine(change.ProposedAcText);
                builder.AppendLine();
                builder.AppendLine("**As approved (human-edited):**");
            }

            builder.AppendLine(change.ApprovedAcText);
            builder.AppendLine();
        }
        else if (!string.IsNullOrWhiteSpace(change.ProposedAcText))
        {
            builder.AppendLine("## Change");
            builder.AppendLine();
            builder.AppendLine(change.ProposedAcText);
            builder.AppendLine();
        }

        builder.AppendLine("## Rationale");
        builder.AppendLine();
        builder.AppendLine(change.Rationale);
        builder.AppendLine();

        builder.AppendLine("## Impact");
        builder.AppendLine();
        builder.AppendLine(FormatImpactLine("Clinical Safety", change.ClinicalSafetyImpact));
        builder.AppendLine(FormatImpactLine("IG", change.IgImpact));
        builder.AppendLine(FormatImpactLine("Security", change.SecurityImpact));

        if (change.HasOpenDefiniteReviews())
        {
            builder.AppendLine();
            builder.AppendLine("## Reviews pending");
            builder.AppendLine();
            AppendPendingReview(builder, "Clinical Safety",
                change.ClinicalSafetyImpact, change.ClinicalSafetyReviewed);
            AppendPendingReview(builder, "IG",
                change.IgImpact, change.IgReviewed);
            AppendPendingReview(builder, "Security",
                change.SecurityImpact, change.SecurityReviewed);
        }

        if (change.Status == ChangeStatus.Undone &&
            !string.IsNullOrWhiteSpace(change.UndoneBy))
        {
            builder.AppendLine();
            builder.AppendLine("## Undo");
            builder.AppendLine();
            builder.AppendLine(CultureInfo.InvariantCulture, $"Undone by: {change.UndoneBy}");
            if (change.UndoneAt.HasValue)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"Date: {change.UndoneAt.Value:yyyy-MM-dd HH:mm:ss} UTC");
            }

            if (!string.IsNullOrWhiteSpace(change.UndoRationale))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"Reason: {change.UndoRationale}");
            }
        }

        return builder.ToString();
    }

    private static string FormatChangeId(Guid id)
    {
        return $"CHANGE-{id.ToString("N")[..8].ToUpperInvariant()}";
    }

    private static string FormatStatus(RequirementChange change)
    {
        return change.Status switch
        {
            ChangeStatus.Approved when change.ApprovedBy is not null && change.ApprovedAt.HasValue =>
                $"Approved — {change.ApprovedAt.Value:yyyy-MM-dd} — {change.ApprovedBy}",
            ChangeStatus.Rejected =>
                $"Rejected — {change.ApprovedBy}",
            ChangeStatus.Undone =>
                $"Undone — {change.UndoneBy}",
            _ => change.Status.ToString()
        };
    }

    private static string FormatImpactLine(string domain, ImpactLevel level)
    {
        return level switch
        {
            ImpactLevel.Definite => $"{domain}: Definite — review required",
            ImpactLevel.Possible => $"{domain}: Possible",
            _ => $"{domain}: None"
        };
    }

    private static void AppendPendingReview(
        StringBuilder builder,
        string domain,
        ImpactLevel impact,
        bool reviewed)
    {
        if (impact == ImpactLevel.Definite && !reviewed)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- [ ] {domain} review — outstanding");
        }
    }
}
