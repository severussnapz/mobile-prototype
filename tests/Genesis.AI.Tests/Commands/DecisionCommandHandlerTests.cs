using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;
using Genesis.AI.Domain.Commands.CreateDecision;
using Genesis.AI.Domain.Commands.DeleteDecision;
using Genesis.AI.Domain.Commands.UpdateDecision;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Tests.Commands;

public class DecisionCommandHandlerTests
{
    private readonly Mock<IProjectDecisionRepository> _decisionRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;

    public DecisionCommandHandlerTests()
    {
        _decisionRepositoryMock = new Mock<IProjectDecisionRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _decisionRepositoryMock.Setup(repository => repository.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static ProjectDecision NewDecision(Guid projectId, TimeProvider timeProvider) =>
        new(projectId, "Title", "Context", "Decision", "Consequences", null, null, null, timeProvider);

    [Fact]
    public async Task Handle_WhenProjectExists_CreatesDecision()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.ExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateDecisionCommandHandler(
            _decisionRepositoryMock.Object, _projectRepositoryMock.Object, _timeProvider);

        var result = await handler.Handle(
            new CreateDecisionCommand(projectId, "Use Postgres", "Need a store", "Chose Postgres", "Ops learn it", "ern", "Ada", "Lovelace"),
            CancellationToken.None);

        Assert.True(result.ProjectFound);
        Assert.NotNull(result.Decision);
        Assert.Equal("Use Postgres", result.Decision!.Title);
        _decisionRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<ProjectDecision>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectMissing_ReturnsProjectNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(repository => repository.ExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateDecisionCommandHandler(
            _decisionRepositoryMock.Object, _projectRepositoryMock.Object, _timeProvider);

        var result = await handler.Handle(
            new CreateDecisionCommand(projectId, "T", "C", "D", "X", null, null, null), CancellationToken.None);

        Assert.False(result.ProjectFound);
        _decisionRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<ProjectDecision>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDecisionExistsForProject_UpdatesFields()
    {
        var projectId = Guid.NewGuid();
        var decision = NewDecision(projectId, _timeProvider);
        _decisionRepositoryMock
            .Setup(repository => repository.GetByIdAsync(decision.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var handler = new UpdateDecisionCommandHandler(_decisionRepositoryMock.Object, _timeProvider);

        var result = await handler.Handle(
            new UpdateDecisionCommand(projectId, decision.Id, "New title", "New context", "New decision", "New consequences"),
            CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal("New title", result.Decision!.Title);
        Assert.Equal("New consequences", result.Decision.Consequences);
    }

    [Fact]
    public async Task Handle_WhenDecisionBelongsToDifferentProject_ReturnsNotFound()
    {
        var decision = NewDecision(Guid.NewGuid(), _timeProvider);
        _decisionRepositoryMock
            .Setup(repository => repository.GetByIdAsync(decision.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var handler = new UpdateDecisionCommandHandler(_decisionRepositoryMock.Object, _timeProvider);

        var result = await handler.Handle(
            new UpdateDecisionCommand(Guid.NewGuid(), decision.Id, "T", "C", "D", "X"), CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_WhenDecisionExists_DeletesDecision()
    {
        var projectId = Guid.NewGuid();
        var decision = NewDecision(projectId, _timeProvider);
        _decisionRepositoryMock
            .Setup(repository => repository.GetByIdAsync(decision.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var handler = new DeleteDecisionCommandHandler(_decisionRepositoryMock.Object);

        var deleted = await handler.Handle(new DeleteDecisionCommand(projectId, decision.Id), CancellationToken.None);

        Assert.True(deleted);
        _decisionRepositoryMock.Verify(repository => repository.Remove(decision), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDecisionMissing_ReturnsFalse()
    {
        _decisionRepositoryMock
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectDecision?)null);

        var handler = new DeleteDecisionCommandHandler(_decisionRepositoryMock.Object);

        var deleted = await handler.Handle(
            new DeleteDecisionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(deleted);
        _decisionRepositoryMock.Verify(repository => repository.Remove(It.IsAny<ProjectDecision>()), Times.Never);
    }
}
