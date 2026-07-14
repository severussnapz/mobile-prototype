using Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate;
using Microsoft.Extensions.Time.Testing;

namespace Genesis.AI.Tests.Domain.PushFailureLogAggregate;

public sealed class PushFailureLogTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var timeProvider = new FakeTimeProvider();
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();
        var filePath = "req/REQ-001.md";
        var errorMessage = "push failed";

        var log = new PushFailureLog(projectId, artefactId, filePath, errorMessage, timeProvider);

        Assert.NotEqual(Guid.Empty, log.Id);
        Assert.Equal(projectId, log.ProjectId);
        Assert.Equal(artefactId, log.ArtefactId);
        Assert.Equal(filePath, log.FilePath);
        Assert.Equal(errorMessage, log.ErrorMessage);
        Assert.Equal(0, log.RetryCount);
        Assert.Null(log.ResolvedAt);
        Assert.Equal(timeProvider.GetUtcNow(), log.FailedAt);
    }

    [Fact]
    public void Constructor_NullFilePath_ThrowsArgumentException()
    {
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();

        Assert.ThrowsAny<ArgumentException>(() =>
            new PushFailureLog(projectId, artefactId, null!, "error", TimeProvider.System));
    }

    [Fact]
    public void Constructor_NullErrorMessage_ThrowsArgumentException()
    {
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();

        Assert.ThrowsAny<ArgumentException>(() =>
            new PushFailureLog(projectId, artefactId, "path.md", null!, TimeProvider.System));
    }
}
