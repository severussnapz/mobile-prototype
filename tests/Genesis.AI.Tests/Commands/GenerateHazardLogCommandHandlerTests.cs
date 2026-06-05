using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.GenerateHazardLog;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.HazardLog;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class GenerateHazardLogCommandHandlerTests
{
    private const string RegistryFilePath = "requirements/HAZARD-REGISTRY.md";

    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _storageServiceMock;
    private readonly Mock<IHazardRegistryParser> _parserMock;
    private readonly Mock<IHazardLogExcelBuilder> _excelBuilderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly GenerateHazardLogCommandHandler _handler;

    public GenerateHazardLogCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _storageServiceMock = new Mock<IArtefactStorageService>();
        _parserMock = new Mock<IHazardRegistryParser>();
        _excelBuilderMock = new Mock<IHazardLogExcelBuilder>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _artefactRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new GenerateHazardLogCommandHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _storageServiceMock.Object,
            _parserMock.Object,
            _excelBuilderMock.Object,
            _timeProvider);
    }

    private Project CreateProject()
    {
        return new Project("ACME", "ACME Portal", null, "TS-001", ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
    }

    private static HazardRecord CreateHazard()
    {
        return new HazardRecord(
            "HAZ-DOC-001", "Patient Identification", "Wrong record", "Harm",
            "REQ-001", "Banner", "Major", "Possible", "High",
            "Major", "Unlikely", "Low", "Active", "Acceptable",
            [new CauseRecord("Cause", [])]);
    }

    private Artefact CreateRegistryArtefact(Guid projectId)
    {
        return Artefact.CreateS3Artefact(
            projectId, 1, RegistryFilePath, "s3-registry-key", "text/markdown", 100, "user-1", _timeProvider);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsProjectNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await _handler.Handle(new GenerateHazardLogCommand(projectId, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateHazardLogStatus.ProjectNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_RegistryArtefactMissing_ReturnsRegistryNotFound()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, RegistryFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await _handler.Handle(new GenerateHazardLogCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateHazardLogStatus.RegistryNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_RegistryContainsNoHazards_ReturnsRegistryNotFound()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, RegistryFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRegistryArtefact(project.Id));
        _storageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-registry-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Empty registry");
        _parserMock
            .Setup(parser => parser.Parse(It.IsAny<string>()))
            .Returns([]);

        var result = await _handler.Handle(new GenerateHazardLogCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateHazardLogStatus.RegistryNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ValidRegistry_ReturnsSuccessWithHazardCount()
    {
        var project = CreateProject();
        var content = new byte[] { 1, 2, 3, 4 };
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, RegistryFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRegistryArtefact(project.Id));
        _storageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-registry-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("## HAZ-DOC-001: ...");
        _parserMock
            .Setup(parser => parser.Parse(It.IsAny<string>()))
            .Returns([CreateHazard()]);
        _excelBuilderMock
            .Setup(builder => builder.Build(
                It.IsAny<IReadOnlyList<HazardRecord>>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(content);
        _artefactRepositoryMock
            .Setup(repository => repository.GetNextVersionForFileAsync(
                project.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _storageServiceMock
            .Setup(storage => storage.SaveBinaryContentAsync(
                project.Id, It.IsAny<string>(), 1, content, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-hazard-log-key");

        var result = await _handler.Handle(new GenerateHazardLogCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateHazardLogStatus.Success, result.Status);
        Assert.Equal(1, result.HazardCount);
        Assert.Equal(content, result.Content);
        Assert.NotNull(result.ArtefactId);
    }

    [Fact]
    public async Task Handle_ValidRegistry_PersistsArtefactUnderFeedbackPath()
    {
        var project = CreateProject();
        var content = new byte[] { 9, 9 };
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, RegistryFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRegistryArtefact(project.Id));
        _storageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-registry-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("## HAZ-DOC-001: ...");
        _parserMock
            .Setup(parser => parser.Parse(It.IsAny<string>()))
            .Returns([CreateHazard()]);
        _excelBuilderMock
            .Setup(builder => builder.Build(
                It.IsAny<IReadOnlyList<HazardRecord>>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(content);
        _artefactRepositoryMock
            .Setup(repository => repository.GetNextVersionForFileAsync(
                project.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _storageServiceMock
            .Setup(storage => storage.SaveBinaryContentAsync(
                project.Id, It.IsAny<string>(), 1, content, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-hazard-log-key");

        Artefact? savedArtefact = null;
        _artefactRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Callback<Artefact, CancellationToken>((artefact, _) => savedArtefact = artefact)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new GenerateHazardLogCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.NotNull(savedArtefact);
        Assert.Equal("feedback/HAZARD_LOG_ACME.xlsx", savedArtefact.FilePath);
        Assert.Equal(1, savedArtefact.Version);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            savedArtefact.ContentType);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HazardLogAlreadyExists_BumpsVersionInPlaceWithoutAddingNewArtefact()
    {
        var project = CreateProject();
        var content = new byte[] { 5, 6, 7 };
        const string hazardLogPath = "feedback/HAZARD_LOG_ACME.xlsx";
        var existing = Artefact.CreateS3Artefact(
            project.Id, 9, hazardLogPath, "s3-hazard-log-key-v9",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            10, "user-0", _timeProvider);

        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, RegistryFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRegistryArtefact(project.Id));
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id, hazardLogPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _storageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-registry-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("## HAZ-DOC-001: ...");
        _parserMock
            .Setup(parser => parser.Parse(It.IsAny<string>()))
            .Returns([CreateHazard()]);
        _excelBuilderMock
            .Setup(builder => builder.Build(
                It.IsAny<IReadOnlyList<HazardRecord>>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(content);
        _storageServiceMock
            .Setup(storage => storage.SaveBinaryContentAsync(
                project.Id, hazardLogPath, 10, content, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-hazard-log-key-v10");

        var result = await _handler.Handle(new GenerateHazardLogCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateHazardLogStatus.Success, result.Status);
        Assert.Equal(existing.Id, result.ArtefactId);
        Assert.Equal(10, existing.Version);
        Assert.Equal("s3-hazard-log-key-v10", existing.S3Key);
        _artefactRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
