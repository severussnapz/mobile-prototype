using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Commands.CreateArtefacts;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class CreateArtefactsCommandHandlerTests
{
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly CreateArtefactsCommandHandler _handler;

    public CreateArtefactsCommandHandlerTests()
    {
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _artefactStorageServiceMock = new Mock<IArtefactStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _artefactRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _artefactRepositoryMock
            .Setup(repository => repository.GetNextVersionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _artefactStorageServiceMock
            .Setup(storage => storage.SaveContentAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("s3-key");

        _handler = new CreateArtefactsCommandHandler(
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_ValidArtefacts_CreatesAndSaves()
    {
        var projectId = Guid.NewGuid();
        var command = new CreateArtefactsCommand(projectId, "user-1",
        [
            new CreateArtefactItem("requirements/REQ-001.md", "# Requirement", "text/markdown"),
            new CreateArtefactItem("manifest.md", "# Manifest", null)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, result.Count);
        _artefactRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DefaultsContentTypeToMarkdown_WhenNotProvided()
    {
        var projectId = Guid.NewGuid();
        var command = new CreateArtefactsCommand(projectId, "user-1",
        [
            new CreateArtefactItem("manifest.md", "# Manifest", null)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("text/markdown", result[0].ContentType);
    }

    [Fact]
    public async Task Handle_EmptyFilePathOrContent_SkipsItems()
    {
        var projectId = Guid.NewGuid();
        var command = new CreateArtefactsCommand(projectId, "user-1",
        [
            new CreateArtefactItem("", "content", "text/markdown"),
            new CreateArtefactItem("file.md", "", "text/markdown"),
            new CreateArtefactItem("valid.md", "content", "text/markdown")
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("valid.md", result[0].FilePath);
        _artefactRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Artefact>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoValidArtefacts_SavesWithEmptyResult()
    {
        var projectId = Guid.NewGuid();
        var command = new CreateArtefactsCommand(projectId, "user-1", []);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Empty(result);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
