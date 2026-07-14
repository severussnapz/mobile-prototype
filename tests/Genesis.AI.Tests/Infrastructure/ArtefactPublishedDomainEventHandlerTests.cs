using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

/// <summary>
/// Tests for <see cref="ArtefactPublishedDomainEventHandler"/> — which fetches published
/// artefact content from object storage and indexes it into the project knowledge namespace.
///
/// Verifies content-type filtering (only markdown/plain text), null-content skipping, and
/// best-effort error handling (indexing failures must never rethrow).
/// </summary>
public class ArtefactPublishedDomainEventHandlerTests
{
    private readonly Mock<IArtefactStorageService> _storageService = new();
    private readonly Mock<IKnowledgeService> _knowledgeService = new();
    private readonly Mock<IGitHubArtefactPushService> _pushService = new();

    private ArtefactPublishedDomainEventHandler CreateHandler() => new(
        _storageService.Object,
        _knowledgeService.Object,
        _pushService.Object,
        NullLogger<ArtefactPublishedDomainEventHandler>.Instance);

    [Fact]
    public async Task Handler_WhenContentTypeIsMarkdown_CallsIndexDocumentAsync()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var notification = new ArtefactPublishedDomainEvent(
            projectId,
            "requirements/REQ-001.md",
            "projects/p/artefacts/requirements/REQ-001.md/v1",
            "text/markdown",
            Guid.NewGuid(),
            1,
            "test-user@emisgroup.com");

        _storageService
            .Setup(storage => storage.GetContentAsync(notification.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("# REQ-001\n\nRequirement content");

        // Act
        await CreateHandler().Handle(notification, CancellationToken.None);

        // Assert
        _knowledgeService.Verify(
            knowledge => knowledge.IndexDocumentAsync(
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                "requirements/REQ-001.md",
                "# REQ-001\n\nRequirement content",
                It.Is<Dictionary<string, string>>(metadata =>
                    metadata["contentType"] == "text/markdown"
                    && metadata["filePath"] == "requirements/REQ-001.md"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handler_WhenContentTypeIsPlainText_CallsIndexDocumentAsync()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var notification = new ArtefactPublishedDomainEvent(
            projectId,
            "notes/context.txt",
            "projects/p/artefacts/notes/context.txt/v1",
            "text/plain",
            Guid.NewGuid(),
            1,
            "test-user@emisgroup.com");

        _storageService
            .Setup(storage => storage.GetContentAsync(notification.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("plain content");

        // Act
        await CreateHandler().Handle(notification, CancellationToken.None);

        // Assert
        _knowledgeService.Verify(
            knowledge => knowledge.IndexDocumentAsync(
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                "notes/context.txt",
                "plain content",
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handler_WhenContentTypeIsHtml_SkipsIndexing()
    {
        // Arrange
        var notification = new ArtefactPublishedDomainEvent(
            Guid.NewGuid(),
            "prototype/index.html",
            "projects/p/artefacts/prototype/index.html/v1",
            "text/html",
            Guid.NewGuid(),
            1,
            "test-user@emisgroup.com");

        // Act
        await CreateHandler().Handle(notification, CancellationToken.None);

        // Assert — never fetches content, never indexes
        _storageService.Verify(
            storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _knowledgeService.Verify(
            knowledge => knowledge.IndexDocumentAsync(
                It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handler_WhenS3ContentIsNull_SkipsIndexing()
    {
        // Arrange
        var notification = new ArtefactPublishedDomainEvent(
            Guid.NewGuid(),
            "requirements/REQ-002.md",
            "projects/p/artefacts/requirements/REQ-002.md/v1",
            "text/markdown",
            Guid.NewGuid(),
            1,
            "test-user@emisgroup.com");

        _storageService
            .Setup(storage => storage.GetContentAsync(notification.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        await CreateHandler().Handle(notification, CancellationToken.None);

        // Assert
        _knowledgeService.Verify(
            knowledge => knowledge.IndexDocumentAsync(
                It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handler_WhenIndexingThrows_DoesNotRethrow()
    {
        // Arrange
        var notification = new ArtefactPublishedDomainEvent(
            Guid.NewGuid(),
            "requirements/REQ-003.md",
            "projects/p/artefacts/requirements/REQ-003.md/v1",
            "text/markdown",
            Guid.NewGuid(),
            1,
            "test-user@emisgroup.com");

        _storageService
            .Setup(storage => storage.GetContentAsync(notification.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("# content");
        _knowledgeService
            .Setup(knowledge => knowledge.IndexDocumentAsync(
                It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Bedrock unavailable"));

        // Act + Assert — must not throw
        var exception = await Record.ExceptionAsync(
            () => CreateHandler().Handle(notification, CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Handle_PublishedEvent_CallsPushServiceAfterIndexing()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();
        var pushServiceMock = new Mock<IGitHubArtefactPushService>();

        var notification = new ArtefactPublishedDomainEvent(
            projectId,
            "requirements/REQ-001.md",
            "projects/p/artefacts/requirements/REQ-001.md/v1",
            "text/markdown",
            artefactId,
            1,
            "user@emisgroup.com");

        _storageService
            .Setup(storage => storage.GetContentAsync(notification.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("# REQ-001");

        var handler = new ArtefactPublishedDomainEventHandler(
            _storageService.Object,
            _knowledgeService.Object,
            pushServiceMock.Object,
            NullLogger<ArtefactPublishedDomainEventHandler>.Instance);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        pushServiceMock.Verify(
            push => push.PushAsync(
                projectId,
                artefactId,
                "requirements/REQ-001.md",
                1,
                "text/markdown",
                "projects/p/artefacts/requirements/REQ-001.md/v1",
                "user@emisgroup.com",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_IndexingFails_PushStillCalled()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();
        var pushServiceMock = new Mock<IGitHubArtefactPushService>();

        var notification = new ArtefactPublishedDomainEvent(
            projectId,
            "requirements/REQ-001.md",
            "projects/p/artefacts/requirements/REQ-001.md/v1",
            "text/markdown",
            artefactId,
            1,
            "user@emisgroup.com");

        _storageService
            .Setup(storage => storage.GetContentAsync(notification.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("# REQ-001");
        _knowledgeService
            .Setup(knowledge => knowledge.IndexDocumentAsync(
                It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Indexing failed"));

        var handler = new ArtefactPublishedDomainEventHandler(
            _storageService.Object,
            _knowledgeService.Object,
            pushServiceMock.Object,
            NullLogger<ArtefactPublishedDomainEventHandler>.Instance);

        // Act
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(notification, CancellationToken.None));

        // Assert
        Assert.Null(exception);
        pushServiceMock.Verify(
            push => push.PushAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PushFails_IndexingResultUnaffected()
    {
        // Arrange
        var pushServiceMock = new Mock<IGitHubArtefactPushService>();

        var notification = new ArtefactPublishedDomainEvent(
            Guid.NewGuid(),
            "requirements/REQ-001.md",
            "projects/p/artefacts/requirements/REQ-001.md/v1",
            "text/markdown",
            Guid.NewGuid(),
            1,
            "user@emisgroup.com");

        _storageService
            .Setup(storage => storage.GetContentAsync(notification.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("# REQ-001");
        pushServiceMock
            .Setup(push => push.PushAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Push failed"));

        var handler = new ArtefactPublishedDomainEventHandler(
            _storageService.Object,
            _knowledgeService.Object,
            pushServiceMock.Object,
            NullLogger<ArtefactPublishedDomainEventHandler>.Instance);

        // Act + Assert — must not throw
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(notification, CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Handle_NonIndexableContentType_PushStillCalled()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var artefactId = Guid.NewGuid();
        var pushServiceMock = new Mock<IGitHubArtefactPushService>();

        var notification = new ArtefactPublishedDomainEvent(
            projectId,
            "clinical-safety/DCB0129-001.xlsx",
            "projects/p/artefacts/clinical-safety/DCB0129-001.xlsx/v1",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            artefactId,
            1,
            "user@emisgroup.com");

        var handler = new ArtefactPublishedDomainEventHandler(
            _storageService.Object,
            _knowledgeService.Object,
            pushServiceMock.Object,
            NullLogger<ArtefactPublishedDomainEventHandler>.Instance);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — indexing is skipped for xlsx, but push is still called
        _knowledgeService.Verify(
            knowledge => knowledge.IndexDocumentAsync(
                It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        pushServiceMock.Verify(
            push => push.PushAsync(
                projectId,
                artefactId,
                "clinical-safety/DCB0129-001.xlsx",
                1,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "projects/p/artefacts/clinical-safety/DCB0129-001.xlsx/v1",
                "user@emisgroup.com",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
