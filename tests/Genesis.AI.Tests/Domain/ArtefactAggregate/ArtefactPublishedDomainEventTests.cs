using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;

namespace Genesis.AI.Tests.Domain.ArtefactAggregate;

public sealed class ArtefactPublishedDomainEventTests
{
    [Fact]
    public void Event_ContainsAllSevenProperties()
    {
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();
        var filePath = "requirements/REQ-001.md";
        var s3Key = "projects/{id}/artefacts/requirements/REQ-001.md/v2";
        var contentType = "text/markdown";
        var version = 2;
        var publishedByErn = "user@emisgroup.com";

        var @event = new ArtefactPublishedDomainEvent(
            projectId, filePath, s3Key, contentType, artefactId, version, publishedByErn);

        Assert.Equal(projectId, @event.ProjectId);
        Assert.Equal(filePath, @event.FilePath);
        Assert.Equal(s3Key, @event.S3Key);
        Assert.Equal(contentType, @event.ContentType);
        Assert.Equal(artefactId, @event.ArtefactId);
        Assert.Equal(version, @event.Version);
        Assert.Equal(publishedByErn, @event.PublishedByErn);
    }

    [Fact]
    public void Event_IsRecord_EqualityByValue()
    {
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();

        var event1 = new ArtefactPublishedDomainEvent(
            projectId, "req/REQ-001.md", "s3key1", "text/markdown", artefactId, 1, "user@emisgroup.com");
        var event2 = new ArtefactPublishedDomainEvent(
            projectId, "req/REQ-001.md", "s3key1", "text/markdown", artefactId, 1, "user@emisgroup.com");
        var event3 = new ArtefactPublishedDomainEvent(
            projectId, "req/REQ-001.md", "s3key1", "text/markdown", Guid.NewGuid(), 1, "user@emisgroup.com");

        Assert.Equal(event1, event2);
        Assert.NotEqual(event1, event3);
    }
}
