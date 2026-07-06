using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.ApproveEmReview;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class ApproveEmReviewCommandHandlerTests
{
    private const string TaskPlanFilePath = "output/planning/Task_Plan.md";
    private const string TasksDataFilePath = "output/planning/tasks_data.json";
    private const string EmApprovalFilePath = "output/planning/EM_APPROVAL.json";

    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly ApproveEmReviewCommandHandler _handler;

    public ApproveEmReviewCommandHandlerTests()
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

        _handler = new ApproveEmReviewCommandHandler(
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
            projectId, version, filePath, $"s3-{filePath}", "application/json", 10, "user-1", _timeProvider, true);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsProjectNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await _handler.Handle(new ApproveEmReviewCommand(projectId, "user-1", null), CancellationToken.None);

        Assert.Equal(ApproveEmReviewStatus.ProjectNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_TaskPlanMissing_ReturnsTaskPlanMissing()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, TaskPlanFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await _handler.Handle(new ApproveEmReviewCommand(project.Id, "user-1", null), CancellationToken.None);

        Assert.Equal(ApproveEmReviewStatus.TaskPlanMissing, result.Status);
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
                project.Id, TaskPlanFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateArtefact(project.Id, TaskPlanFilePath, 1));

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, TasksDataFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await _handler.Handle(new ApproveEmReviewCommand(project.Id, "user-1", null), CancellationToken.None);

        Assert.Equal(ApproveEmReviewStatus.TasksDataMissing, result.Status);
    }

    [Fact]
    public async Task Handle_PlanAndTasksPresent_ReturnsSuccessAndPersistsApproval()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, TaskPlanFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateArtefact(project.Id, TaskPlanFilePath, 1));

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, TasksDataFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateArtefact(project.Id, TasksDataFilePath, 1));

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, EmApprovalFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                project.Id, EmApprovalFilePath, 1, It.IsAny<string>(), "application/json", It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-em-approval");

        var result = await _handler.Handle(new ApproveEmReviewCommand(project.Id, "user-1", "Looks good"), CancellationToken.None);

        Assert.Equal(ApproveEmReviewStatus.Success, result.Status);
        _artefactRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
