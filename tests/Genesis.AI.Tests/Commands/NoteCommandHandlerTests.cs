using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;
using Genesis.AI.Domain.Commands.CreateNote;
using Genesis.AI.Domain.Commands.DeleteNote;
using Genesis.AI.Domain.Commands.UpdateNote;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Tests.Commands;

public class NoteCommandHandlerTests
{
    private readonly Mock<IProjectNoteRepository> _noteRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;

    public NoteCommandHandlerTests()
    {
        _noteRepositoryMock = new Mock<IProjectNoteRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _noteRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_WhenProjectExists_CreatesNote()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.ExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateNoteCommandHandler(
            _noteRepositoryMock.Object, _projectRepositoryMock.Object, _timeProvider);

        var result = await handler.Handle(
            new CreateNoteCommand(projectId, "A note", "ern", "Ada", "Lovelace"), CancellationToken.None);

        Assert.True(result.ProjectFound);
        Assert.NotNull(result.Note);
        Assert.Equal("A note", result.Note!.Content);
        _noteRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<ProjectNote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectMissing_ReturnsProjectNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.ExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateNoteCommandHandler(
            _noteRepositoryMock.Object, _projectRepositoryMock.Object, _timeProvider);

        var result = await handler.Handle(
            new CreateNoteCommand(projectId, "A note", null, null, null), CancellationToken.None);

        Assert.False(result.ProjectFound);
        _noteRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<ProjectNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoteExistsForProject_UpdatesContent()
    {
        var projectId = Guid.NewGuid();
        var note = new ProjectNote(projectId, "Old", null, null, null, _timeProvider);
        _noteRepositoryMock
            .Setup(repository => repository.GetByIdAsync(note.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        var handler = new UpdateNoteCommandHandler(_noteRepositoryMock.Object, _timeProvider);

        var result = await handler.Handle(
            new UpdateNoteCommand(projectId, note.Id, "New"), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal("New", result.Note!.Content);
    }

    [Fact]
    public async Task Handle_WhenNoteBelongsToDifferentProject_ReturnsNotFound()
    {
        var note = new ProjectNote(Guid.NewGuid(), "Old", null, null, null, _timeProvider);
        _noteRepositoryMock
            .Setup(repository => repository.GetByIdAsync(note.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        var handler = new UpdateNoteCommandHandler(_noteRepositoryMock.Object, _timeProvider);

        var result = await handler.Handle(
            new UpdateNoteCommand(Guid.NewGuid(), note.Id, "New"), CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_WhenNoteExists_DeletesNote()
    {
        var projectId = Guid.NewGuid();
        var note = new ProjectNote(projectId, "Old", null, null, null, _timeProvider);
        _noteRepositoryMock
            .Setup(repository => repository.GetByIdAsync(note.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        var handler = new DeleteNoteCommandHandler(_noteRepositoryMock.Object);

        var deleted = await handler.Handle(new DeleteNoteCommand(projectId, note.Id), CancellationToken.None);

        Assert.True(deleted);
        _noteRepositoryMock.Verify(repository => repository.Remove(note), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoteMissing_ReturnsFalse()
    {
        _noteRepositoryMock
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectNote?)null);

        var handler = new DeleteNoteCommandHandler(_noteRepositoryMock.Object);

        var deleted = await handler.Handle(
            new DeleteNoteCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(deleted);
        _noteRepositoryMock.Verify(repository => repository.Remove(It.IsAny<ProjectNote>()), Times.Never);
    }
}
