using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.RunPlanningPreflight;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Planning;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class RunPlanningPreflightCommandHandlerTests
{
    private const string PreflightStatusFilePath = "output/planning/PREFLIGHT_STATUS.json";

    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly Mock<IPlanningGateService> _planningGateServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly RunPlanningPreflightCommandHandler _handler;

    public RunPlanningPreflightCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        _planningGateServiceMock = new Mock<IPlanningGateService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _artefactRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new RunPlanningPreflightCommandHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _planningGateServiceMock.Object,
            _timeProvider);
    }

    private Project CreateProject()
    {
        return new Project("ACME", "ACME Portal", null, "PORTASK0001045", ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsProjectNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await _handler.Handle(new RunPlanningPreflightCommand(projectId, "user-1"), CancellationToken.None);

        Assert.Equal(RunPlanningPreflightStatus.ProjectNotFound, result.Status);
        Assert.False(result.PreflightPassed);
    }

    [Fact]
    public async Task Handle_GatePassed_ReturnsSuccessAndPersistsPassedStatus()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _planningGateServiceMock
            .Setup(service => service.EvaluateAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanningGateEvaluation(
                RunPrerequisitesMet: true,
                PreflightPassed: true,
                TaskPlanExists: true,
                TasksDataExists: true,
                EmApproved: false,
                EmApprovalIsStale: false,
                SplitPassed: false,
                GatePassed: false,
                Errors: [],
                OutputArtefacts: []));

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, PreflightStatusFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                project.Id, PreflightStatusFilePath, 1, It.IsAny<string>(), "application/json", It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-preflight-status");

        var result = await _handler.Handle(new RunPlanningPreflightCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(RunPlanningPreflightStatus.Success, result.Status);
        Assert.True(result.PreflightPassed);
        _artefactRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_GateFailed_ReturnsSuccessWithErrors()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _planningGateServiceMock
            .Setup(service => service.EvaluateAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanningGateEvaluation(
                RunPrerequisitesMet: false,
                PreflightPassed: false,
                TaskPlanExists: false,
                TasksDataExists: false,
                EmApproved: false,
                EmApprovalIsStale: false,
                SplitPassed: false,
                GatePassed: false,
                Errors: ["Preflight: has not passed. Run preflight and resolve all errors."],
                OutputArtefacts: []));

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, PreflightStatusFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                project.Id, PreflightStatusFilePath, 1, It.IsAny<string>(), "application/json", It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-preflight-status");

        var result = await _handler.Handle(new RunPlanningPreflightCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(RunPlanningPreflightStatus.Success, result.Status);
        Assert.False(result.PreflightPassed);
        Assert.Single(result.Errors);
    }
}
