using System.Text.Json;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.ApproveEmReview;

public sealed class ApproveEmReviewCommandHandler
    : IRequestHandler<ApproveEmReviewCommand, ApproveEmReviewResult>
{
    private const string TaskPlanFilePath = "output/planning/Task_Plan.md";
    private const string TasksDataFilePath = "output/planning/tasks_data.json";
    private const string EmApprovalFilePath = "output/planning/EM_APPROVAL.json";
    private const string JsonContentType = "application/json";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;

    public ApproveEmReviewCommandHandler(
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

    public async Task<ApproveEmReviewResult> Handle(
        ApproveEmReviewCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return new ApproveEmReviewResult(
                ApproveEmReviewStatus.ProjectNotFound,
                $"No project found with ID '{request.ProjectId}'.");
        }

        var taskPlanArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId,
            TaskPlanFilePath,
            cancellationToken);

        if (taskPlanArtefact is null)
        {
            return new ApproveEmReviewResult(
                ApproveEmReviewStatus.TaskPlanMissing,
                $"'{TaskPlanFilePath}' must exist before EM review can be approved.");
        }

        var tasksDataArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId,
            TasksDataFilePath,
            cancellationToken);

        if (tasksDataArtefact is null)
        {
            return new ApproveEmReviewResult(
                ApproveEmReviewStatus.TasksDataMissing,
                $"'{TasksDataFilePath}' must exist before EM review can be approved.");
        }

        var approvedAtUtc = _timeProvider.GetUtcNow();
        var payload = JsonSerializer.Serialize(new
        {
            approvedBy = request.UserId,
            approvedAtUtc,
            notes = request.Notes,
            taskPlanVersion = taskPlanArtefact.Version,
            tasksDataVersion = tasksDataArtefact.Version
        });

        await PersistArtefactAsync(request.ProjectId, payload, request.UserId, cancellationToken);

        return new ApproveEmReviewResult(ApproveEmReviewStatus.Success, null);
    }

    private async Task PersistArtefactAsync(
        Guid projectId,
        string payload,
        string userId,
        CancellationToken cancellationToken)
    {
        var existing = await _artefactRepository.GetByProjectAndFilePathAsync(
            projectId,
            EmApprovalFilePath,
            cancellationToken);

        if (existing is not null)
        {
            var nextVersion = existing.Version + 1;
            var storageKey = await _artefactStorageService.SaveContentAsync(
                projectId,
                EmApprovalFilePath,
                nextVersion,
                payload,
                JsonContentType,
                cancellationToken);

            var tracked = await _artefactRepository.GetByIdAsync(existing.Id, cancellationToken);
            tracked!.ReplaceContent(nextVersion, storageKey, JsonContentType, payload.Length, userId, _timeProvider);
            await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var newStorageKey = await _artefactStorageService.SaveContentAsync(
            projectId,
            EmApprovalFilePath,
            1,
            payload,
            JsonContentType,
            cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            projectId,
            1,
            EmApprovalFilePath,
            newStorageKey,
            JsonContentType,
            payload.Length,
            userId, _timeProvider, true);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
