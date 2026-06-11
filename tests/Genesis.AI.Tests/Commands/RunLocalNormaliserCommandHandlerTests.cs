using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.RunLocalNormaliser;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Normalisation;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class RunLocalNormaliserCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly Mock<INormalisationGateService> _normalisationGateServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly RunLocalNormaliserCommandHandler _handler;

    public RunLocalNormaliserCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        _normalisationGateServiceMock = new Mock<INormalisationGateService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _artefactRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new RunLocalNormaliserCommandHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _normalisationGateServiceMock.Object,
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

        var result = await _handler.Handle(new RunLocalNormaliserCommand(projectId, "user-1"), CancellationToken.None);

        Assert.Equal(RunLocalNormaliserStatus.ProjectNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_PrerequisitesMissing_ReturnsConflictStatus()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _normalisationGateServiceMock
            .Setup(service => service.EvaluateAsync(project.Id, project.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NormalisationGateEvaluation(
                RunPrerequisitesMet: false,
                GatePassed: false,
                Errors: ["Missing prerequisite artefact: manifest.md"],
                OutputArtefacts: []));

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                "output/NORMALISATION_RUN_STATUS.json",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                project.Id,
                "output/NORMALISATION_RUN_STATUS.json",
                1,
                It.IsAny<string>(),
                "application/json",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-normalisation-run-status");

        var result = await _handler.Handle(new RunLocalNormaliserCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(RunLocalNormaliserStatus.PrerequisitesMissing, result.Status);
        Assert.Equal("failed", result.RunStatus);
    }

    [Fact]
    public async Task Handle_ValidGateEvaluation_ReturnsSuccess()
    {
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
                project.Id,
                "output/NORMALISATION_RUN_STATUS.json",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                project.Id,
                "output/NORMALISATION_RUN_STATUS.json",
                1,
                It.IsAny<string>(),
                "application/json",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-normalisation-run-status");

        var result = await _handler.Handle(new RunLocalNormaliserCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(RunLocalNormaliserStatus.Success, result.Status);
        Assert.Equal("completed", result.RunStatus);
        Assert.True(result.GatePassed);
    }
}
