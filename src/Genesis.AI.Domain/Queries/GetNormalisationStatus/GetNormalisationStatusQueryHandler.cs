using System.Text.Json;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetNormalisationStatus;

public sealed class GetNormalisationStatusQueryHandler
    : IRequestHandler<GetNormalisationStatusQuery, GetNormalisationStatusResult>
{
    private const string RunStatusFilePath = "output/NORMALISATION_RUN_STATUS.json";
    private const string BypassAuditFilePath = "output/NORMALISATION_BYPASS_AUDIT.json";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly INormalisationGateService _normalisationGateService;

    public GetNormalisationStatusQueryHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        INormalisationGateService normalisationGateService)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _normalisationGateService = normalisationGateService ?? throw new ArgumentNullException(nameof(normalisationGateService));
    }

    public async Task<GetNormalisationStatusResult> Handle(
        GetNormalisationStatusQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return new GetNormalisationStatusResult(false, "not-run", null, [], false, false, false, null, null, [], []);
        }

        var evaluation = await _normalisationGateService.EvaluateAsync(
            request.ProjectId,
            project.Code,
            cancellationToken);

        var runStatusArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId,
            RunStatusFilePath,
            cancellationToken);
        var (runStatus, lastRunAt, runErrors) = await ReadRunStatusAsync(runStatusArtefact, cancellationToken);

        var bypassAuditArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId,
            BypassAuditFilePath,
            cancellationToken);
        var (bypassActive, bypassedBy, bypassedAtUtc) = await ReadBypassAuditAsync(bypassAuditArtefact, cancellationToken);

        var planningEligible = evaluation.GatePassed || bypassActive;

        return new GetNormalisationStatusResult(
            true,
            runStatus,
            lastRunAt,
            runErrors,
            evaluation.GatePassed,
            planningEligible,
            bypassActive,
            bypassedBy,
            bypassedAtUtc,
            evaluation.Errors,
            evaluation.OutputArtefacts);
    }

    private async Task<(string RunStatus, DateTimeOffset? LastRunAt, List<string> RunErrors)> ReadRunStatusAsync(
        Artefact? runStatusArtefact,
        CancellationToken cancellationToken)
    {
        if (runStatusArtefact is null)
        {
            return ("not-run", null, []);
        }

        var runStatusJson = await _artefactStorageService.GetContentAsync(runStatusArtefact.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(runStatusJson))
        {
            return ("not-run", runStatusArtefact.CreatedAt, []);
        }

        return ParseRunStatusDocument(runStatusJson, runStatusArtefact.CreatedAt);
    }

    private static (string RunStatus, DateTimeOffset? LastRunAt, List<string> RunErrors) ParseRunStatusDocument(
        string runStatusJson,
        DateTimeOffset fallbackLastRunAt)
    {
        var runStatus = "not-run";
        DateTimeOffset? lastRunAt = fallbackLastRunAt;
        var runErrors = new List<string>();

        try
        {
            using var document = JsonDocument.Parse(runStatusJson);
            var root = document.RootElement;

            if (root.TryGetProperty("runStatus", out var runStatusElement)
                && runStatusElement.ValueKind == JsonValueKind.String)
            {
                runStatus = runStatusElement.GetString() ?? runStatus;
            }

            if (root.TryGetProperty("recordedAtUtc", out var recordedElement)
                && recordedElement.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(recordedElement.GetString(), out var parsedTimestamp))
            {
                lastRunAt = parsedTimestamp;
            }

            if (root.TryGetProperty("errors", out var errorsElement)
                && errorsElement.ValueKind == JsonValueKind.Array)
            {
                runErrors = errorsElement
                    .EnumerateArray()
                    .Where(errorElement => errorElement.ValueKind == JsonValueKind.String)
                    .Select(errorElement => errorElement.GetString())
                    .Where(errorText => !string.IsNullOrWhiteSpace(errorText))
                    .Cast<string>()
                    .ToList();
            }
        }
        catch (JsonException)
        {
            runStatus = "failed";
            runErrors = ["Run status artefact contains invalid JSON."];
        }

        return (runStatus, lastRunAt, runErrors);
    }

    private async Task<(bool Active, string? BypassedBy, DateTimeOffset? BypassedAtUtc)> ReadBypassAuditAsync(
        Artefact? bypassAuditArtefact,
        CancellationToken cancellationToken)
    {
        if (bypassAuditArtefact is null)
        {
            return (false, null, null);
        }

        var bypassAuditJson = await _artefactStorageService.GetContentAsync(bypassAuditArtefact.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(bypassAuditJson))
        {
            return (false, null, null);
        }

        return ParseBypassAuditDocument(bypassAuditJson);
    }

    private static (bool Active, string? BypassedBy, DateTimeOffset? BypassedAtUtc) ParseBypassAuditDocument(string bypassAuditJson)
    {
        var bypassActive = false;
        string? bypassedBy = null;
        DateTimeOffset? bypassedAtUtc = null;

        try
        {
            using var document = JsonDocument.Parse(bypassAuditJson);
            var root = document.RootElement;

            if (root.TryGetProperty("activeBypass", out var activeBypassElement)
                && activeBypassElement.ValueKind == JsonValueKind.Object)
            {
                bypassActive = true;

                if (activeBypassElement.TryGetProperty("bypassedBy", out var bypassedByElement)
                    && bypassedByElement.ValueKind == JsonValueKind.String)
                {
                    bypassedBy = bypassedByElement.GetString();
                }

                if (activeBypassElement.TryGetProperty("bypassedAtUtc", out var bypassedAtElement)
                    && bypassedAtElement.ValueKind == JsonValueKind.String
                    && DateTimeOffset.TryParse(bypassedAtElement.GetString(), out var parsedBypassedAt))
                {
                    bypassedAtUtc = parsedBypassedAt;
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed bypass audit artefact and treat bypass as inactive.
        }

        return (bypassActive, bypassedBy, bypassedAtUtc);
    }
}
