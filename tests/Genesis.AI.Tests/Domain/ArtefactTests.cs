using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;

namespace Genesis.AI.Tests.Domain;

public class ArtefactTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public void CreateS3Artefact_WhenCalled_SetsAllMetadata()
    {
        var projectId = Guid.NewGuid();

        var artefact = Artefact.CreateS3Artefact(
            projectId,
            2,
            "requirements/REQ-001.md",
            "projects/abc/artefacts/requirements/REQ-001.md/v2",
            "text/markdown",
            1024,
            "user-1", _timeProvider, true);

        Assert.NotEqual(Guid.Empty, artefact.Id);
        Assert.Equal(projectId, artefact.ProjectId);
        Assert.Equal(2, artefact.Version);
        Assert.Equal("requirements/REQ-001.md", artefact.FilePath);
        Assert.Equal("projects/abc/artefacts/requirements/REQ-001.md/v2", artefact.S3Key);
        Assert.Equal("text/markdown", artefact.ContentType);
        Assert.Equal(1024, artefact.SizeBytes);
        Assert.Equal("user-1", artefact.CreatedBy);
    }

    [Fact]
    public void CreateS3Artefact_WhenCalled_SetsCreatedAtTimestamp()
    {
        var artefact = Artefact.CreateS3Artefact(
            Guid.NewGuid(), 1, "manifest.md", "s3-key", "text/markdown", 10, "user-1", _timeProvider, true);

        Assert.True(artefact.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(artefact.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateS3Artefact_WithBlankFilePath_ThrowsArgumentException(string filePath)
    {
        Assert.Throws<ArgumentException>(() => Artefact.CreateS3Artefact(
            Guid.NewGuid(), 1, filePath, "s3-key", "text/markdown", 10, "user-1", _timeProvider, true));
    }

    [Fact]
    public void CreateS3Artefact_WithNullFilePath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Artefact.CreateS3Artefact(
            Guid.NewGuid(), 1, null!, "s3-key", "text/markdown", 10, "user-1", _timeProvider, true));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateS3Artefact_WithBlankS3Key_ThrowsArgumentException(string s3Key)
    {
        Assert.Throws<ArgumentException>(() => Artefact.CreateS3Artefact(
            Guid.NewGuid(), 1, "manifest.md", s3Key, "text/markdown", 10, "user-1", _timeProvider, true));
    }

    [Fact]
    public void CreateS3Artefact_WithNullS3Key_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Artefact.CreateS3Artefact(
            Guid.NewGuid(), 1, "manifest.md", null!, "text/markdown", 10, "user-1", _timeProvider, true));
    }

    [Fact]
    public void PromoteToPublished_WhenDraft_FlipsToPublishedAndReturnsTrue()
    {
        var artefact = Artefact.CreateS3Artefact(
            Guid.NewGuid(), 1, "prototype/fragments/screen-01.html", "s3-key", "text/html", 10, "user-1", _timeProvider, false);

        var promoted = artefact.PromoteToPublished();

        Assert.True(promoted);
        Assert.True(artefact.IsPublished);
    }

    [Fact]
    public void PromoteToPublished_WhenAlreadyPublished_ReturnsFalseAndStaysPublished()
    {
        var artefact = Artefact.CreateS3Artefact(
            Guid.NewGuid(), 1, "requirements/REQ-001.md", "s3-key", "text/markdown", 10, "user-1", _timeProvider, true);

        var promoted = artefact.PromoteToPublished();

        Assert.False(promoted);
        Assert.True(artefact.IsPublished);
    }
}
