using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Microsoft.Extensions.Time.Testing;

namespace Genesis.AI.Tests.Domain.ArtefactAggregate;

public sealed class ArtefactGitHubPushedTests
{
    [Fact]
    public void MarkPushedToGitHub_SetsGitHubPushedAt()
    {
        var timeProvider = new FakeTimeProvider();
        var artefact = Artefact.CreateS3Artefact(
            Guid.NewGuid(),
            1,
            "requirements/REQ-001.md",
            "projects/p1/artefacts/requirements/REQ-001.md/v1",
            "text/markdown",
            128,
            "tester@example.com",
            timeProvider,
            true);

        timeProvider.Advance(TimeSpan.FromSeconds(1));

        artefact.MarkPushedToGitHub(timeProvider);

        Assert.Equal(timeProvider.GetUtcNow(), artefact.GitHubPushedAt);
    }

    [Fact]
    public void MarkPushedToGitHub_CalledTwice_UpdatesToLatestTime()
    {
        var timeProvider = new FakeTimeProvider();
        var artefact = Artefact.CreateS3Artefact(
            Guid.NewGuid(),
            1,
            "requirements/REQ-002.md",
            "projects/p1/artefacts/requirements/REQ-002.md/v1",
            "text/markdown",
            256,
            "tester@example.com",
            timeProvider,
            true);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        artefact.MarkPushedToGitHub(timeProvider);

        timeProvider.Advance(TimeSpan.FromSeconds(3));
        var secondTime = timeProvider.GetUtcNow();
        artefact.MarkPushedToGitHub(timeProvider);

        Assert.Equal(secondTime, artefact.GitHubPushedAt);
    }

    [Fact]
    public void NewArtefact_GitHubPushedAt_IsNull()
    {
        var timeProvider = new FakeTimeProvider();
        var artefact = Artefact.CreateS3Artefact(
            Guid.NewGuid(),
            1,
            "requirements/REQ-003.md",
            "projects/p1/artefacts/requirements/REQ-003.md/v1",
            "text/markdown",
            64,
            "tester@example.com",
            timeProvider,
            true);

        Assert.Null(artefact.GitHubPushedAt);
    }
}
