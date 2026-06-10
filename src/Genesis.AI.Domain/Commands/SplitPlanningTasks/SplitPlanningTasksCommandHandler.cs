using System.Text.Json;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Planning;
using MediatR;

namespace Genesis.AI.Domain.Commands.SplitPlanningTasks;

public sealed class SplitPlanningTasksCommandHandler
    : IRequestHandler<SplitPlanningTasksCommand, SplitPlanningTasksResult>
{
    private const string TasksDataFilePath = "output/planning/tasks_data.json";
    private const string EmApprovalFilePath = "output/planning/EM_APPROVAL.json";
    private const string TaskIndexFilePath = "output/tasks/task_index.json";
    private const string SplitStatusFilePath = "output/tasks/SPLIT_STATUS.json";
    private const string JsonContentType = "application/json";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;

    public SplitPlanningTasksCommandHandler(
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

    public async Task<SplitPlanningTasksResult> Handle(
        SplitPlanningTasksCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return new SplitPlanningTasksResult(
                SplitPlanningTasksStatus.ProjectNotFound, 0, [], [], [],
                $"No project found with ID '{request.ProjectId}'.");
        }

        var tasksDataArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId, TasksDataFilePath, cancellationToken);

        if (tasksDataArtefact is null)
        {
            return new SplitPlanningTasksResult(
                SplitPlanningTasksStatus.TasksDataMissing, 0, [], [], [],
                $"'{TasksDataFilePath}' must exist before splitting tasks.");
        }

        var approvalFailure = await ValidateEmApprovalAsync(request.ProjectId, tasksDataArtefact, cancellationToken);
        if (approvalFailure is not null)
        {
            return approvalFailure;
        }

        var (loadFailure, tasks) = await LoadTasksAsync(request.ProjectId, tasksDataArtefact, cancellationToken);
        if (loadFailure is not null)
        {
            return loadFailure;
        }

        var duplicateFailure = ValidateNoDuplicates(tasks);
        if (duplicateFailure is not null)
        {
            return duplicateFailure;
        }

        var createdArtefacts = await PersistSplitOutputsAsync(request, tasks, cancellationToken);

        return new SplitPlanningTasksResult(
            SplitPlanningTasksStatus.Success,
            tasks.Count,
            [],
            [],
            createdArtefacts,
            null);
    }

    private async Task<SplitPlanningTasksResult?> ValidateEmApprovalAsync(
        Guid projectId,
        Artefact tasksDataArtefact,
        CancellationToken cancellationToken)
    {
        var emApprovalArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            projectId, EmApprovalFilePath, cancellationToken);

        if (emApprovalArtefact is null)
        {
            return new SplitPlanningTasksResult(
                SplitPlanningTasksStatus.EmApprovalMissing, 0, [], [], [],
                "EM review must be approved before splitting tasks.");
        }

        var emApprovalContent = await _artefactStorageService.GetContentAsync(emApprovalArtefact.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(emApprovalContent))
        {
            return new SplitPlanningTasksResult(
                SplitPlanningTasksStatus.EmApprovalMissing, 0, [], [], [],
                "EM approval artefact is empty.");
        }

        try
        {
            using var approvalDoc = JsonDocument.Parse(emApprovalContent);
            var approvedTasksDataVersion = approvalDoc.RootElement.TryGetProperty("tasksDataVersion", out var approvedVersionEl)
                ? approvedVersionEl.GetInt32()
                : -1;

            if (approvedTasksDataVersion != tasksDataArtefact.Version)
            {
                return new SplitPlanningTasksResult(
                    SplitPlanningTasksStatus.EmApprovalStale, 0, [], [], [],
                    $"EM approval was for tasks_data.json v{approvedTasksDataVersion} but current version is v{tasksDataArtefact.Version}. Re-approve before splitting.");
            }
        }
        catch (JsonException)
        {
            return new SplitPlanningTasksResult(
                SplitPlanningTasksStatus.EmApprovalMissing, 0, [], [], [],
                "EM approval artefact contains invalid JSON.");
        }

        return null;
    }

    private async Task<(SplitPlanningTasksResult? Failure, List<JsonElement> Tasks)> LoadTasksAsync(
        Guid projectId,
        Artefact tasksDataArtefact,
        CancellationToken cancellationToken)
    {
        var tasksDataContent = await _artefactStorageService.GetContentAsync(tasksDataArtefact.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(tasksDataContent))
        {
            return (new SplitPlanningTasksResult(
                SplitPlanningTasksStatus.InvalidTasksData, 0, [], [], [],
                $"'{TasksDataFilePath}' is empty."), []);
        }

        JsonElement tasksArray;
        try
        {
            using var doc = JsonDocument.Parse(tasksDataContent);
            var root = doc.RootElement.Clone();

            if (!root.TryGetProperty("tasks", out tasksArray) || tasksArray.ValueKind != JsonValueKind.Array)
            {
                return (new SplitPlanningTasksResult(
                    SplitPlanningTasksStatus.InvalidTasksData, 0, [], [], [],
                    $"'{TasksDataFilePath}' must contain a top-level 'tasks' array."), []);
            }
        }
        catch (JsonException jsonException)
        {
            return (new SplitPlanningTasksResult(
                SplitPlanningTasksStatus.InvalidTasksData, 0, [], [], [],
                $"'{TasksDataFilePath}' is not valid JSON: {jsonException.Message}"), []);
        }

        return (null, tasksArray.EnumerateArray().ToList());
    }

    private static SplitPlanningTasksResult? ValidateNoDuplicates(List<JsonElement> tasks)
    {
        var duplicateTaskIds = tasks
            .GroupBy(task => task.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty)
            .Where(group => group.Count() > 1 && !string.IsNullOrWhiteSpace(group.Key))
            .Select(group => group.Key)
            .ToList();

        if (duplicateTaskIds.Count > 0)
        {
            return new SplitPlanningTasksResult(
                SplitPlanningTasksStatus.DuplicateTaskIds, 0, duplicateTaskIds, [], [],
                $"Duplicate task IDs detected: {string.Join(", ", duplicateTaskIds)}");
        }

        var duplicateCheckAssignments = CollectCheckAssignments(tasks)
            .GroupBy(check => check, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateCheckAssignments.Count > 0)
        {
            return new SplitPlanningTasksResult(
                SplitPlanningTasksStatus.DuplicateCheckAssignments, 0, [], duplicateCheckAssignments, [],
                $"Duplicate CHECK assignments detected: {string.Join(", ", duplicateCheckAssignments)}");
        }

        return null;
    }

    private static List<string> CollectCheckAssignments(List<JsonElement> tasks)
    {
        var allCheckAssignments = new List<string>();
        foreach (var task in tasks)
        {
            if (task.TryGetProperty("context", out var contextEl)
                && contextEl.TryGetProperty("checks_embedded", out var checksEl)
                && checksEl.ValueKind == JsonValueKind.Array)
            {
                allCheckAssignments.AddRange(
                    checksEl.EnumerateArray()
                        .Where(check => check.ValueKind == JsonValueKind.String)
                        .Select(check => check.GetString())
                        .Where(check => !string.IsNullOrWhiteSpace(check))
                        .Cast<string>());
            }
        }

        return allCheckAssignments;
    }

    private async Task<List<PlanningArtefactSummary>> PersistSplitOutputsAsync(
        SplitPlanningTasksCommand request,
        List<JsonElement> tasks,
        CancellationToken cancellationToken)
    {
        var createdArtefacts = new List<PlanningArtefactSummary>();
        var taskIndexEntries = new List<object>();

        foreach (var task in tasks)
        {
            var taskId = task.TryGetProperty("id", out var idEl) ? (idEl.GetString() ?? $"TASK-{Guid.NewGuid():N}") : $"TASK-{Guid.NewGuid():N}";
            var taskFilePath = $"output/tasks/{taskId}.json";
            var taskPayload = task.GetRawText();

            var savedArtefact = await PersistArtefactAsync(request.ProjectId, taskFilePath, taskPayload, request.UserId, cancellationToken);
            createdArtefacts.Add(new PlanningArtefactSummary(savedArtefact.Id, savedArtefact.FilePath, savedArtefact.Version, savedArtefact.CreatedAt));

            taskIndexEntries.Add(new { id = taskId });
        }

        var taskIndexPayload = JsonSerializer.Serialize(new
        {
            tasks = taskIndexEntries,
            generatedAtUtc = _timeProvider.GetUtcNow(),
            taskCount = tasks.Count
        });

        var taskIndexArtefact = await PersistArtefactAsync(request.ProjectId, TaskIndexFilePath, taskIndexPayload, request.UserId, cancellationToken);
        createdArtefacts.Add(new PlanningArtefactSummary(taskIndexArtefact.Id, taskIndexArtefact.FilePath, taskIndexArtefact.Version, taskIndexArtefact.CreatedAt));

        var splitStatusPayload = JsonSerializer.Serialize(new
        {
            status = "passed",
            taskCount = tasks.Count,
            duplicateTaskIds = Array.Empty<string>(),
            duplicateCheckAssignments = Array.Empty<string>(),
            splitAtUtc = _timeProvider.GetUtcNow()
        });

        var splitStatusArtefact = await PersistArtefactAsync(request.ProjectId, SplitStatusFilePath, splitStatusPayload, request.UserId, cancellationToken);
        createdArtefacts.Add(new PlanningArtefactSummary(splitStatusArtefact.Id, splitStatusArtefact.FilePath, splitStatusArtefact.Version, splitStatusArtefact.CreatedAt));

        return createdArtefacts;
    }

    private async Task<Artefact> PersistArtefactAsync(
        Guid projectId,
        string filePath,
        string payload,
        string userId,
        CancellationToken cancellationToken)
    {
        var existing = await _artefactRepository.GetByProjectAndFilePathAsync(projectId, filePath, cancellationToken);

        if (existing is not null)
        {
            var nextVersion = existing.Version + 1;
            var storageKey = await _artefactStorageService.SaveContentAsync(
                projectId, filePath, nextVersion, payload, JsonContentType, cancellationToken);

            var tracked = await _artefactRepository.GetByIdAsync(existing.Id, cancellationToken);
            tracked!.ReplaceContent(nextVersion, storageKey, JsonContentType, payload.Length, userId, _timeProvider);
            await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return tracked;
        }

        var newStorageKey = await _artefactStorageService.SaveContentAsync(
            projectId, filePath, 1, payload, JsonContentType, cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            projectId, 1, filePath, newStorageKey, JsonContentType, payload.Length, userId, _timeProvider);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return artefact;
    }
}
