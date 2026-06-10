using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.SplitPlanningTasks;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class SplitPlanningTasksCommandHandlerTests
{
    private const string TasksDataFilePath = "output/planning/tasks_data.json";
    private const string EmApprovalFilePath = "output/planning/EM_APPROVAL.json";

    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly SplitPlanningTasksCommandHandler _handler;

    public SplitPlanningTasksCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _artefactRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-output-key");

        _handler = new SplitPlanningTasksCommandHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _timeProvider);
    }

    private Project CreateProject()
    {
        return new Project("ACME", "ACME Portal", null, "PORTASK0001045", ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
    }

    private Artefact CreateArtefact(Guid projectId, string filePath, int version)
    {
        return Artefact.CreateS3Artefact(
            projectId, version, filePath, $"s3-{filePath}", "application/json", 10, "user-1", _timeProvider);
    }

    private void SetupTasksData(Project project, int version, string content)
    {
        var tasksDataArtefact = CreateArtefact(project.Id, TasksDataFilePath, version);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, TasksDataFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasksDataArtefact);
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync(tasksDataArtefact.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
    }

    private void SetupEmApproval(Project project, string content)
    {
        var emApprovalArtefact = CreateArtefact(project.Id, EmApprovalFilePath, 1);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, EmApprovalFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emApprovalArtefact);
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync(emApprovalArtefact.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsProjectNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await _handler.Handle(new SplitPlanningTasksCommand(projectId, "user-1"), CancellationToken.None);

        Assert.Equal(SplitPlanningTasksStatus.ProjectNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_TasksDataMissing_ReturnsTasksDataMissing()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, TasksDataFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await _handler.Handle(new SplitPlanningTasksCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(SplitPlanningTasksStatus.TasksDataMissing, result.Status);
    }

    [Fact]
    public async Task Handle_EmApprovalMissing_ReturnsEmApprovalMissing()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        SetupTasksData(project, 1, "{\"tasks\":[{\"id\":\"TASK-001\"}]}");
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, EmApprovalFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await _handler.Handle(new SplitPlanningTasksCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(SplitPlanningTasksStatus.EmApprovalMissing, result.Status);
    }

    [Fact]
    public async Task Handle_EmApprovalForOlderVersion_ReturnsEmApprovalStale()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        SetupTasksData(project, 2, "{\"tasks\":[{\"id\":\"TASK-001\"}]}");
        SetupEmApproval(project, "{\"tasksDataVersion\":1}");

        var result = await _handler.Handle(new SplitPlanningTasksCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(SplitPlanningTasksStatus.EmApprovalStale, result.Status);
    }

    [Fact]
    public async Task Handle_TasksDataWithoutTasksArray_ReturnsInvalidTasksData()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        SetupTasksData(project, 1, "{\"notTasks\":[]}");
        SetupEmApproval(project, "{\"tasksDataVersion\":1}");

        var result = await _handler.Handle(new SplitPlanningTasksCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(SplitPlanningTasksStatus.InvalidTasksData, result.Status);
    }

    [Fact]
    public async Task Handle_DuplicateTaskIds_ReturnsDuplicateTaskIds()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        SetupTasksData(project, 1, "{\"tasks\":[{\"id\":\"TASK-001\"},{\"id\":\"TASK-001\"}]}");
        SetupEmApproval(project, "{\"tasksDataVersion\":1}");

        var result = await _handler.Handle(new SplitPlanningTasksCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(SplitPlanningTasksStatus.DuplicateTaskIds, result.Status);
        Assert.Contains("TASK-001", result.DuplicateTaskIds);
    }

    [Fact]
    public async Task Handle_DuplicateCheckAssignments_ReturnsDuplicateCheckAssignments()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        SetupTasksData(
            project,
            1,
            "{\"tasks\":[{\"id\":\"TASK-001\",\"context\":{\"checks_embedded\":[\"CHK-1\"]}},{\"id\":\"TASK-002\",\"context\":{\"checks_embedded\":[\"CHK-1\"]}}]}");
        SetupEmApproval(project, "{\"tasksDataVersion\":1}");

        var result = await _handler.Handle(new SplitPlanningTasksCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(SplitPlanningTasksStatus.DuplicateCheckAssignments, result.Status);
        Assert.Contains("CHK-1", result.DuplicateCheckAssignments);
    }

    [Fact]
    public async Task Handle_ValidSingleTask_ReturnsSuccessAndPersistsOutputs()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        SetupTasksData(
            project,
            1,
            "{\"tasks\":[{\"id\":\"TASK-001\",\"context\":{\"checks_embedded\":[\"CHK-1\"]}}]}");
        SetupEmApproval(project, "{\"tasksDataVersion\":1}");
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, It.Is<string>(path => path.StartsWith("output/tasks/")), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await _handler.Handle(new SplitPlanningTasksCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(SplitPlanningTasksStatus.Success, result.Status);
        Assert.Equal(1, result.TaskCount);
        // One task file + task_index.json + SPLIT_STATUS.json.
        _artefactRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }
}
