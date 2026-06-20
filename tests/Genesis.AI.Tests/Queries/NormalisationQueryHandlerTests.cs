using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Normalisation;
using Genesis.AI.Domain.Queries.GetNormalisationArtefacts;
using Genesis.AI.Domain.Queries.GetNormalisationStatus;
using Moq;

namespace Genesis.AI.Tests.Queries;

public class NormalisationQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly Mock<INormalisationGateService> _normalisationGateServiceMock;
    private readonly TimeProvider _timeProvider;

    public NormalisationQueryHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        _normalisationGateServiceMock = new Mock<INormalisationGateService>();
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
    // GetNormalisationStatusQueryHandler
    // ========================================================================

    [Fact]
    public async Task Handle_StatusForMissingProject_ReturnsNotFound()
    {
        var handler = new GetNormalisationStatusQueryHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _normalisationGateServiceMock.Object);

        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await handler.Handle(new GetNormalisationStatusQuery(projectId), CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_StatusGatePassedWithoutBypass_ReturnsPlanningEligible()
    {
        var handler = new GetNormalisationStatusQueryHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _normalisationGateServiceMock.Object);

        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _normalisationGateServiceMock
            .Setup(service => service.EvaluateAsync(project.Id, project.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NormalisationGateEvaluation(
                RunPrerequisitesMet: true,
                GatePassed: true,
                Errors: [],
                OutputArtefacts: []));

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await handler.Handle(new GetNormalisationStatusQuery(project.Id), CancellationToken.None);

        Assert.True(result.Found);
        Assert.True(result.GatePassed);
        Assert.True(result.PlanningEligible);
        Assert.False(result.BypassActive);
        Assert.Equal("not-run", result.RunStatus);
    }

    // ========================================================================
    // GetNormalisationArtefactsQueryHandler
    // ========================================================================

    [Fact]
    public async Task Handle_ArtefactsForMissingProject_ReturnsNotFound()
    {
        var handler = new GetNormalisationArtefactsQueryHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object);

        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await handler.Handle(new GetNormalisationArtefactsQuery(projectId), CancellationToken.None);

        Assert.False(result.Found);
        Assert.Empty(result.Artefacts);
    }

    [Fact]
    public async Task Handle_ArtefactsForExistingProject_ReturnsOnlyOutputArtefacts()
    {
        var handler = new GetNormalisationArtefactsQueryHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object);

        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var artefacts = new List<Artefact>
        {
            CreateArtefact(project.Id, "output/REQ-001/checks.json"),
            CreateArtefact(project.Id, "output/CS_Guardrails.json"),
            CreateArtefact(project.Id, "requirements/REQ-001.md")
        };

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artefacts);

        var result = await handler.Handle(new GetNormalisationArtefactsQuery(project.Id), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal(2, result.Artefacts.Count);
        Assert.DoesNotContain(result.Artefacts, artefact => artefact.FilePath.StartsWith("requirements/", StringComparison.Ordinal));
    }
}
