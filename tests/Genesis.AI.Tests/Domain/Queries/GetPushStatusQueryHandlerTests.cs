using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetPushStatus;
using Moq;

namespace Genesis.AI.Tests.Domain.Queries;

public sealed class GetPushStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoFailures_ReturnsEmptyFailedArtefactIds()
    {
        var projectId = Guid.NewGuid();
        var repository = new Mock<IPushFailureLogRepository>();

        repository
            .Setup(pushFailureLogRepository => pushFailureLogRepository.GetUnresolvedCountAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        repository
            .Setup(pushFailureLogRepository => pushFailureLogRepository.GetFailedArtefactIdsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var handler = new GetPushStatusQueryHandler(repository.Object);

        var result = await handler.Handle(new GetPushStatusQuery(projectId), CancellationToken.None);

        Assert.Empty(result.FailedArtefactIds);
    }

    [Fact]
    public async Task Handle_WithFailures_ReturnsFailedArtefactIds()
    {
        var projectId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var repository = new Mock<IPushFailureLogRepository>();
        repository
            .Setup(pushFailureLogRepository => pushFailureLogRepository.GetUnresolvedCountAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        repository
            .Setup(pushFailureLogRepository => pushFailureLogRepository.GetFailedArtefactIdsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { id1, id2 });

        var handler = new GetPushStatusQueryHandler(repository.Object);

        var result = await handler.Handle(new GetPushStatusQuery(projectId), CancellationToken.None);

        Assert.Contains(id1, result.FailedArtefactIds);
        Assert.Contains(id2, result.FailedArtefactIds);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectUnresolvedCount()
    {
        var projectId = Guid.NewGuid();
        var repository = new Mock<IPushFailureLogRepository>();

        repository
            .Setup(pushFailureLogRepository => pushFailureLogRepository.GetUnresolvedCountAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        repository
            .Setup(pushFailureLogRepository => pushFailureLogRepository.GetFailedArtefactIdsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var handler = new GetPushStatusQueryHandler(repository.Object);

        var result = await handler.Handle(new GetPushStatusQuery(projectId), CancellationToken.None);

        Assert.Equal(3, result.UnresolvedCount);
    }
}
