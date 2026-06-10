using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;

namespace Genesis.AI.Tests.Services;

public class PlanningGateServiceTests
{
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly TimeProvider _timeProvider;
    private readonly PlanningGateService _service;

    public PlanningGateServiceTests()
    {
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        _timeProvider = TimeProvider.System;
        _service = new PlanningGateService(_artefactRepositoryMock.Object, _artefactStorageServiceMock.Object);
    }

    private Artefact CreateArtefact(Guid projectId, string filePath, int version, string? content)
    {
        var artefact = Artefact.CreateS3Artefact(
            projectId, version, filePath, $"s3-{filePath}", "application/json", 10, "user-1", _timeProvider);

        if (content is not null)
        {
            _artefactStorageServiceMock
                .Setup(storage => storage.GetContentAsync(artefact.S3Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(content);
        }

        return artefact;
    }

    [Fact]
    public async Task EvaluateAsync_NoArtefacts_ReturnsGateFailedWithErrors()
    {
        var projectId = Guid.NewGuid();
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var evaluation = await _service.EvaluateAsync(projectId, CancellationToken.None);

        Assert.False(evaluation.GatePassed);
        Assert.False(evaluation.RunPrerequisitesMet);
        Assert.NotEmpty(evaluation.Errors);
    }

    [Fact]
    public async Task EvaluateAsync_AllRequirementsMet_ReturnsGatePassed()
    {
        var projectId = Guid.NewGuid();
        var artefacts = new List<Artefact>
        {
            CreateArtefact(projectId, "output/planning/PREFLIGHT_STATUS.json", 1, "{\"status\":\"passed\"}"),
            CreateArtefact(projectId, "output/planning/Task_Plan.md", 1, "plan"),
            CreateArtefact(projectId, "output/planning/tasks_data.json", 1, "{\"tasks\":[{\"id\":\"TASK-001\"}]}"),
            CreateArtefact(projectId, "output/planning/EM_APPROVAL.json", 1, "{\"taskPlanVersion\":1,\"tasksDataVersion\":1}"),
            CreateArtefact(projectId, "output/tasks/SPLIT_STATUS.json", 1, "{\"status\":\"passed\"}"),
            CreateArtefact(projectId, "output/tasks/task_index.json", 1, "{\"tasks\":[{\"id\":\"TASK-001\"}]}"),
            CreateArtefact(projectId, "output/tasks/TASK-001.json", 1, "{\"id\":\"TASK-001\"}")
        };

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artefacts);

        var evaluation = await _service.EvaluateAsync(projectId, CancellationToken.None);

        Assert.True(evaluation.GatePassed);
        Assert.True(evaluation.PreflightPassed);
        Assert.True(evaluation.TaskPlanExists);
        Assert.True(evaluation.TasksDataExists);
        Assert.True(evaluation.EmApproved);
        Assert.False(evaluation.EmApprovalIsStale);
        Assert.True(evaluation.SplitPassed);
        Assert.Empty(evaluation.Errors);
    }

    [Fact]
    public async Task EvaluateAsync_EmApprovalVersionMismatch_ReturnsStale()
    {
        var projectId = Guid.NewGuid();
        var artefacts = new List<Artefact>
        {
            CreateArtefact(projectId, "output/planning/PREFLIGHT_STATUS.json", 1, "{\"status\":\"passed\"}"),
            CreateArtefact(projectId, "output/planning/Task_Plan.md", 2, "plan"),
            CreateArtefact(projectId, "output/planning/tasks_data.json", 1, "{\"tasks\":[{\"id\":\"TASK-001\"}]}"),
            CreateArtefact(projectId, "output/planning/EM_APPROVAL.json", 1, "{\"taskPlanVersion\":1,\"tasksDataVersion\":1}")
        };

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artefacts);

        var evaluation = await _service.EvaluateAsync(projectId, CancellationToken.None);

        Assert.True(evaluation.EmApprovalIsStale);
        Assert.False(evaluation.EmApproved);
        Assert.False(evaluation.GatePassed);
    }
}
