using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.GenerateDpiaReport;
using Genesis.AI.Domain.Dpia;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class GenerateDpiaReportCommandHandlerTests
{
    private const string DpiaDataPath = "output/PR1625_DPIA_DATA.json";

    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _storageServiceMock;
    private readonly Mock<IDpiaDocxBuilder> _docxBuilderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly GenerateDpiaReportCommandHandler _handler;

    public GenerateDpiaReportCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _storageServiceMock = new Mock<IArtefactStorageService>();
        _docxBuilderMock = new Mock<IDpiaDocxBuilder>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _artefactRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new GenerateDpiaReportCommandHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _storageServiceMock.Object,
            _docxBuilderMock.Object,
            _timeProvider);
    }

    private Project CreateProject()
    {
        return new Project("ACME", "ACME Portal", null, "TS-001", ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
    }

    private Artefact CreateDpiaDataArtefact(Guid projectId)
    {
        return Artefact.CreateS3Artefact(
            projectId,
            1,
            DpiaDataPath,
            "s3-dpia-data-key",
            "application/json",
            100,
            "user-1",
            _timeProvider, true);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsProjectNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await _handler.Handle(new GenerateDpiaReportCommand(projectId, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateDpiaReportStatus.ProjectNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_DpiaDataMissing_ReturnsDataNotFound()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                DpiaDataPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artefact?)null);

        var result = await _handler.Handle(new GenerateDpiaReportCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateDpiaReportStatus.DataNotFound, result.Status);
    }

    [Fact]
    public async Task Handle_InvalidDpiaData_ReturnsDataInvalid()
    {
        var project = CreateProject();
        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                DpiaDataPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDpiaDataArtefact(project.Id));
        _storageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-dpia-data-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");
        _docxBuilderMock
            .Setup(builder => builder.Build(It.IsAny<string>()))
            .Throws(new InvalidOperationException("bad payload"));

        var result = await _handler.Handle(new GenerateDpiaReportCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateDpiaReportStatus.DataInvalid, result.Status);
    }

    [Fact]
    public async Task Handle_ValidData_ReturnsSuccessAndPersistsDocx()
    {
        var project = CreateProject();
        var content = new byte[] { 1, 2, 3, 4 };

        _projectRepositoryMock
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                project.Id,
                DpiaDataPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDpiaDataArtefact(project.Id));
        _storageServiceMock
            .Setup(storage => storage.GetContentAsync("s3-dpia-data-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"document_version\":\"1\"}");
        _docxBuilderMock
            .Setup(builder => builder.Build(It.IsAny<string>()))
            .Returns(content);
        _storageServiceMock
            .Setup(storage => storage.SaveBinaryContentAsync(
                project.Id,
                It.IsAny<string>(),
                1,
                content,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-dpia-docx-key");

        Artefact? savedArtefact = null;
        _artefactRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()))
            .Callback<Artefact, CancellationToken>((artefact, _) => savedArtefact = artefact)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new GenerateDpiaReportCommand(project.Id, "user-1"), CancellationToken.None);

        Assert.Equal(GenerateDpiaReportStatus.Success, result.Status);
        Assert.NotNull(savedArtefact);
        Assert.Equal("feedback/PR1625_DPIA_ACME.docx", savedArtefact!.FilePath);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            savedArtefact.ContentType);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
