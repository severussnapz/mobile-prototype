using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.BypassNormalisationPlanningGate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class BypassNormalisationPlanningGateCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly BypassNormalisationPlanningGateCommandHandler _handler;

    private const string BypassAuditFilePath = "output/NORMALISATION_BYPASS_AUDIT.json";

    public BypassNormalisationPlanningGateCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _artefactRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-key");

        _handler = new BypassNormalisationPlanningGateCommandHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
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

        var command = new BypassNormalisationPlanningGateCommand(projectId, "user-1", "Manual override");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(BypassNormalisationPlanningGateStatus.ProjectNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_NoExistingAudit_CreatesNewArtefact()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(project.Id, BypassAuditFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var command = new BypassNormalisationPlanningGateCommand(project.Id, "user-1", "Manual override");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(BypassNormalisationPlanningGateStatus.Success, result.Status);
        _artefactRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingAudit_ReplacesContentAndDoesNotAdd()
    {
        var project = CreateProject();
        var existing = Artefact.CreateS3Artefact(
            project.Id, 1, BypassAuditFilePath, "s3-existing", "application/json", 10, "user-1", _timeProvider);

        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(project.Id, BypassAuditFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new BypassNormalisationPlanningGateCommand(project.Id, "user-1", "Re-confirm override");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(BypassNormalisationPlanningGateStatus.Success, result.Status);
        Assert.Equal(2, existing.Version);
        _artefactRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
