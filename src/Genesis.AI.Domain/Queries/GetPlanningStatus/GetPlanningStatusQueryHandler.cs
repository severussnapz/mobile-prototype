using System.Text.Json;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetPlanningStatus;

public sealed class GetPlanningStatusQueryHandler
    : IRequestHandler<GetPlanningStatusQuery, GetPlanningStatusResult>
{
    private const string PreflightStatusFilePath = "output/planning/PREFLIGHT_STATUS.json";
    private const string EmApprovalFilePath = "output/planning/EM_APPROVAL.json";
    private const string SplitStatusFilePath = "output/tasks/SPLIT_STATUS.json";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IPlanningGateService _planningGateService;

    public GetPlanningStatusQueryHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IPlanningGateService planningGateService)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _planningGateService = planningGateService ?? throw new ArgumentNullException(nameof(planningGateService));
    }

    public async Task<GetPlanningStatusResult> Handle(
        GetPlanningStatusQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return new GetPlanningStatusResult(false, false, null, [], false, false, false, false, null, null, false, 0, false, [], []);
        }

        var evaluation = await _planningGateService.EvaluateAsync(request.ProjectId, cancellationToken);

        var preflightArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId, PreflightStatusFilePath, cancellationToken);
        DateTimeOffset? lastPreflightAt = preflightArtefact?.CreatedAt;

        var emApprovalArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId, EmApprovalFilePath, cancellationToken);
        var (approvedBy, approvedAtUtc) = await ReadEmApprovalAsync(emApprovalArtefact, cancellationToken);

        var splitStatusArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId, SplitStatusFilePath, cancellationToken);
        var taskCount = await ReadTaskCountAsync(splitStatusArtefact, cancellationToken);

        return new GetPlanningStatusResult(
            true,
            evaluation.PreflightPassed,
            lastPreflightAt,
            evaluation.Errors.Where(error => error.StartsWith("Preflight:", StringComparison.Ordinal)).ToList(),
            evaluation.TaskPlanExists,
            evaluation.TasksDataExists,
            evaluation.EmApproved,
            evaluation.EmApprovalIsStale,
            approvedBy,
            approvedAtUtc,
            evaluation.SplitPassed,
            taskCount,
            evaluation.GatePassed,
            evaluation.Errors,
            evaluation.OutputArtefacts);
    }

    private async Task<(string? ApprovedBy, DateTimeOffset? ApprovedAtUtc)> ReadEmApprovalAsync(
        Artefact? emApprovalArtefact,
        CancellationToken cancellationToken)
    {
        if (emApprovalArtefact is null)
        {
            return (null, null);
        }

        var emApprovalContent = await _artefactStorageService.GetContentAsync(emApprovalArtefact.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(emApprovalContent))
        {
            return (null, null);
        }

        string? approvedBy = null;
        DateTimeOffset? approvedAtUtc = null;

        try
        {
            using var emDoc = JsonDocument.Parse(emApprovalContent);
            var emRoot = emDoc.RootElement;

            if (emRoot.TryGetProperty("approvedBy", out var approvedByEl) && approvedByEl.ValueKind == JsonValueKind.String)
                approvedBy = approvedByEl.GetString();

            if (emRoot.TryGetProperty("approvedAtUtc", out var approvedAtEl)
                && approvedAtEl.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(approvedAtEl.GetString(), out var parsedApprovedAt))
                approvedAtUtc = parsedApprovedAt;
        }
        catch (JsonException)
        {
            // Ignore malformed — evaluation already handles stale state.
        }

        return (approvedBy, approvedAtUtc);
    }

    private async Task<int> ReadTaskCountAsync(
        Artefact? splitStatusArtefact,
        CancellationToken cancellationToken)
    {
        if (splitStatusArtefact is null)
        {
            return 0;
        }

        var splitContent = await _artefactStorageService.GetContentAsync(splitStatusArtefact.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(splitContent))
        {
            return 0;
        }

        try
        {
            using var splitDoc = JsonDocument.Parse(splitContent);
            if (splitDoc.RootElement.TryGetProperty("taskCount", out var taskCountEl)
                && taskCountEl.ValueKind == JsonValueKind.Number)
                return taskCountEl.GetInt32();
        }
        catch (JsonException) { }

        return 0;
    }
}
