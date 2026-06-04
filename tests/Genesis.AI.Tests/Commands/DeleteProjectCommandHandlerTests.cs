using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.DeleteProject;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class DeleteProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly DeleteProjectCommandHandler _handler;

    public DeleteProjectCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _projectRepositoryMock
            .Setup(r => r.UnitOfWork)
            .Returns(_unitOfWorkMock.Object);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new DeleteProjectCommandHandler(
            _projectRepositoryMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_ExistingProject_SoftDeletesProject()
    {
        var project = new Project("DOC", "Documents", null, "PORTASK0001045", ComplianceDomain.Generic, "user-1", _timeProvider);
        _projectRepositoryMock
            .Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new DeleteProjectCommand(project.Id);

        await _handler.Handle(command, CancellationToken.None);

        Assert.True(project.IsDeleted);
    }

    [Fact]
    public async Task Handle_ExistingProject_SavesChanges()
    {
        var project = new Project("DOC", "Documents", null, "PORTASK0001045", ComplianceDomain.Generic, "user-1", _timeProvider);
        _projectRepositoryMock
            .Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new DeleteProjectCommand(project.Id);

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ThrowsInvalidOperationException()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new DeleteProjectCommand(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
