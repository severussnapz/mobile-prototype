using System.Text.Json;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Planning;

namespace Genesis.AI.Infrastructure.Services;

public sealed class PlanningGateService : IPlanningGateService
{
    private const string PreflightStatusFilePath = "output/planning/PREFLIGHT_STATUS.json";
    private const string TaskPlanFilePath = "output/planning/Task_Plan.md";
    private const string TasksDataFilePath = "output/planning/tasks_data.json";
    private const string EmApprovalFilePath = "output/planning/EM_APPROVAL.json";
    private const string SplitStatusFilePath = "output/tasks/SPLIT_STATUS.json";
    private const string TaskIndexFilePath = "output/tasks/task_index.json";

    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;

    public PlanningGateService(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
    }

    public async Task<PlanningGateEvaluation> EvaluateAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var allArtefacts = await _artefactRepository.GetByProjectIdAsync(projectId, cancellationToken);

        var latestByPath = BuildLatestByPath(allArtefacts);
        var errors = new List<string>();

        var preflightPassed = await EvaluatePreflightAsync(latestByPath, errors, cancellationToken);

        var taskPlanExists = latestByPath.ContainsKey(TaskPlanFilePath);
        if (!taskPlanExists)
            errors.Add($"'{TaskPlanFilePath}' does not exist. Generate the task plan in chat.");

        var (tasksDataExists, currentTasksDataVersion) = await EvaluateTasksDataAsync(latestByPath, errors, cancellationToken);

        var currentTaskPlanVersion = latestByPath.TryGetValue(TaskPlanFilePath, out var taskPlanArtefact)
            ? taskPlanArtefact.Version : -1;

        var (emApproved, emApprovalIsStale) = await EvaluateEmApprovalAsync(
            latestByPath, errors, currentTaskPlanVersion, currentTasksDataVersion, cancellationToken);

        var splitPassed = await EvaluateSplitAsync(latestByPath, errors, cancellationToken);

        if (!latestByPath.ContainsKey(TaskIndexFilePath))
            errors.Add($"'{TaskIndexFilePath}' does not exist. Run Generate Task Files.");

        var taskFileCount = latestByPath.Keys.Count(path =>
            path.StartsWith("output/tasks/TASK-", StringComparison.OrdinalIgnoreCase) &&
            path.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

        if (taskFileCount == 0)
            errors.Add("No TASK-*.json files found under output/tasks/. Run Generate Task Files.");

        var runPrerequisitesMet = tasksDataExists && emApproved;

        var gatePassed = preflightPassed
            && taskPlanExists
            && tasksDataExists
            && emApproved
            && !emApprovalIsStale
            && splitPassed
            && latestByPath.ContainsKey(TaskIndexFilePath)
            && taskFileCount > 0;

        var outputArtefacts = BuildOutputArtefacts(latestByPath);

        return new PlanningGateEvaluation(
            runPrerequisitesMet,
            preflightPassed,
            taskPlanExists,
            tasksDataExists,
            emApproved,
            emApprovalIsStale,
            splitPassed,
            gatePassed,
            errors,
            outputArtefacts);
    }

    private static Dictionary<string, Artefact> BuildLatestByPath(IReadOnlyList<Artefact> allArtefacts)
    {
        return allArtefacts
            .GroupBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(artefact => artefact.Version).ThenByDescending(artefact => artefact.CreatedAt).First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> EvaluatePreflightAsync(
        Dictionary<string, Artefact> latestByPath,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var preflightPassed = false;
        if (latestByPath.TryGetValue(PreflightStatusFilePath, out var preflightArtefact))
        {
            var preflightContent = await _artefactStorageService.GetContentAsync(preflightArtefact.S3Key, cancellationToken);
            if (!string.IsNullOrWhiteSpace(preflightContent))
            {
                try
                {
                    using var preflightDoc = JsonDocument.Parse(preflightContent);
                    if (preflightDoc.RootElement.TryGetProperty("status", out var statusEl)
                        && statusEl.GetString() == "passed")
                    {
                        preflightPassed = true;
                    }
                }
                catch (JsonException) { }
            }
        }

        if (!preflightPassed)
            errors.Add("Preflight: has not passed. Run preflight and resolve all errors.");

        return preflightPassed;
    }

    private async Task<(bool Exists, int Version)> EvaluateTasksDataAsync(
        Dictionary<string, Artefact> latestByPath,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var tasksDataExists = false;
        var currentTasksDataVersion = -1;
        if (latestByPath.TryGetValue(TasksDataFilePath, out var tasksDataArtefact))
        {
            currentTasksDataVersion = tasksDataArtefact.Version;
            var tasksDataContent = await _artefactStorageService.GetContentAsync(tasksDataArtefact.S3Key, cancellationToken);
            if (!string.IsNullOrWhiteSpace(tasksDataContent))
            {
                try
                {
                    using var tasksDoc = JsonDocument.Parse(tasksDataContent);
                    if (tasksDoc.RootElement.TryGetProperty("tasks", out var tasksEl)
                        && tasksEl.ValueKind == JsonValueKind.Array
                        && tasksEl.GetArrayLength() > 0)
                    {
                        tasksDataExists = true;
                    }
                    else
                    {
                        errors.Add($"'{TasksDataFilePath}' exists but 'tasks' array is missing or empty.");
                    }
                }
                catch (JsonException jsonException)
                {
                    errors.Add($"'{TasksDataFilePath}' is not valid JSON: {jsonException.Message}");
                }
            }
            else
            {
                errors.Add($"'{TasksDataFilePath}' is empty.");
            }
        }
        else
        {
            errors.Add($"'{TasksDataFilePath}' does not exist. Save tasks_data.json in chat.");
        }

        return (tasksDataExists, currentTasksDataVersion);
    }

    private async Task<(bool Approved, bool Stale)> EvaluateEmApprovalAsync(
        Dictionary<string, Artefact> latestByPath,
        List<string> errors,
        int currentTaskPlanVersion,
        int currentTasksDataVersion,
        CancellationToken cancellationToken)
    {
        var emApproved = false;
        var emApprovalIsStale = false;

        if (latestByPath.TryGetValue(EmApprovalFilePath, out var emApprovalArtefact))
        {
            var emApprovalContent = await _artefactStorageService.GetContentAsync(emApprovalArtefact.S3Key, cancellationToken);
            if (!string.IsNullOrWhiteSpace(emApprovalContent))
            {
                try
                {
                    using var emDoc = JsonDocument.Parse(emApprovalContent);
                    var approvedTaskPlanVersion = emDoc.RootElement.TryGetProperty("taskPlanVersion", out var tpvEl) ? tpvEl.GetInt32() : -1;
                    var approvedTasksDataVersion = emDoc.RootElement.TryGetProperty("tasksDataVersion", out var tdvEl) ? tdvEl.GetInt32() : -1;

                    if (currentTaskPlanVersion == approvedTaskPlanVersion && currentTasksDataVersion == approvedTasksDataVersion)
                    {
                        emApproved = true;
                    }
                    else
                    {
                        emApprovalIsStale = true;
                        errors.Add("EM approval is stale — Task_Plan.md or tasks_data.json has been regenerated. Re-approve the plan.");
                    }
                }
                catch (JsonException)
                {
                    errors.Add("EM approval artefact contains invalid JSON.");
                }
            }
        }
        else
        {
            errors.Add("EM review has not been approved. Click Approve Plan after reviewing Task_Plan.md.");
        }

        return (emApproved, emApprovalIsStale);
    }

    private async Task<bool> EvaluateSplitAsync(
        Dictionary<string, Artefact> latestByPath,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var splitPassed = false;
        if (latestByPath.TryGetValue(SplitStatusFilePath, out var splitStatusArtefact))
        {
            var splitContent = await _artefactStorageService.GetContentAsync(splitStatusArtefact.S3Key, cancellationToken);
            if (!string.IsNullOrWhiteSpace(splitContent))
            {
                try
                {
                    using var splitDoc = JsonDocument.Parse(splitContent);
                    if (splitDoc.RootElement.TryGetProperty("status", out var splitStatusEl)
                        && splitStatusEl.GetString() == "passed")
                    {
                        splitPassed = true;
                    }
                    else
                    {
                        errors.Add("Split did not pass. Check SPLIT_STATUS.json for details.");
                    }
                }
                catch (JsonException) { errors.Add("SPLIT_STATUS.json contains invalid JSON."); }
            }
        }
        else
        {
            errors.Add("Tasks have not been split yet. Click Generate Task Files.");
        }

        return splitPassed;
    }

    private static List<PlanningArtefactSummary> BuildOutputArtefacts(Dictionary<string, Artefact> latestByPath)
    {
        return latestByPath.Values
            .Where(artefact =>
                artefact.FilePath.StartsWith("output/planning/", StringComparison.OrdinalIgnoreCase) ||
                artefact.FilePath.StartsWith("output/tasks/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(artefact => artefact.Version)
            .Select(artefact => new PlanningArtefactSummary(artefact.Id, artefact.FilePath, artefact.Version, artefact.CreatedAt))
            .ToList();
    }
}
