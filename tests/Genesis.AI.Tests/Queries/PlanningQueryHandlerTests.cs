using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Planning;
using Genesis.AI.Domain.Queries.GetPlanningArtefacts;
using Genesis.AI.Domain.Queries.GetPlanningStatus;
using Moq;

namespace Genesis.AI.Tests.Queries;

public class PlanningQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly Mock<IPlanningGateService> _planningGateServiceMock;
    private readonly TimeProvider _timeProvider;

    public PlanningQueryHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        _planningGateServiceMock = new Mock<IPlanningGateService>();
        _timeProvider = TimeProvider.System;
    }

    private Project CreateProject()
    {
        return new Project("ACME", "ACME Portal", null, "PORTASK0001045", ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
    }

    private Artefact CreateArtefact(Guid projectId, string filePath)
    {
        return Artefact.CreateS3Artefact(
            projectId, 1, filePath, $"s3-{filePath}", "application/json", 10, "user-1", _timeProvider, true);
    }

    // ========================================================================
    // GetPlanningStatusQueryHandler
    // ========================================================================

    [Fact]
    public async Task Handle_StatusForMissingProject_ReturnsNotFound()
    {
        var handler = new GetPlanningStatusQueryHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _planningGateServiceMock.Object);

        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await handler.Handle(new GetPlanningStatusQuery(projectId), CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_StatusForExistingProject_ReturnsEvaluationValues()
    {
        var handler = new GetPlanningStatusQueryHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _planningGateServiceMock.Object);

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
                EmApproved: true,
                EmApprovalIsStale: false,
                SplitPassed: true,
                GatePassed: true,
                Errors: [],
                OutputArtefacts: []));

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await handler.Handle(new GetPlanningStatusQuery(project.Id), CancellationToken.None);

        Assert.True(result.Found);
        Assert.True(result.GatePassed);
        Assert.True(result.PreflightPassed);
        Assert.Equal(0, result.TaskCount);
        Assert.Null(result.ApprovedBy);
    }

    // ========================================================================
    // GetPlanningArtefactsQueryHandler
    // ========================================================================

    [Fact]
    public async Task Handle_ArtefactsForMissingProject_ReturnsNotFound()
    {
        var handler = new GetPlanningArtefactsQueryHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object);

        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await handler.Handle(new GetPlanningArtefactsQuery(projectId), CancellationToken.None);

        Assert.False(result.Found);
        Assert.Empty(result.Artefacts);
    }

    [Fact]
    public async Task Handle_ArtefactsForExistingProject_ReturnsOnlyPlanningArtefacts()
    {
        var handler = new GetPlanningArtefactsQueryHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object);

        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var artefacts = new List<Artefact>
        {
            CreateArtefact(project.Id, "output/planning/Task_Plan.md"),
            CreateArtefact(project.Id, "output/tasks/TASK-001.json"),
            CreateArtefact(project.Id, "requirements/REQ-001.md")
        };

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artefacts);

        var result = await handler.Handle(new GetPlanningArtefactsQuery(project.Id), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal(2, result.Artefacts.Count);
        Assert.DoesNotContain(result.Artefacts, artefact => artefact.FilePath.StartsWith("requirements/", StringComparison.Ordinal));
    }
}
