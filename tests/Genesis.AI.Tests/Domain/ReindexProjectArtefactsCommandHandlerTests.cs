using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Commands.ReindexProjectArtefacts;
using Microsoft.Extensions.Logging.Abstractions;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Tests.Domain;

public sealed class ReindexProjectArtefactsCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAllArtefactsHaveContent_ReturnsAllIndexed()
    {
        var projectId = Guid.NewGuid();
        var artefact1 = CreatePublishedArtefact(projectId, 1, "requirements/REQ-001.md", "text/markdown");
        var artefact2 = CreatePublishedArtefact(projectId, 2, "requirements/REQ-002.md", "text/markdown");
        var artefact3 = CreatePublishedArtefact(projectId, 3, "requirements/REQ-003.md", "text/markdown");
        var manifest = new List<Artefact> { artefact1, artefact2, artefact3 };

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetProjectArtefactManifestAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# content");

        var knowledgeService = new Mock<IKnowledgeService>();

        var handler = new ReindexProjectArtefactsCommandHandler(
            artefactRepository.Object,
            artefactStorageService.Object,
            knowledgeService.Object,
            NullLogger<ReindexProjectArtefactsCommandHandler>.Instance);

        var result = await handler.Handle(new ReindexProjectArtefactsCommand(projectId, "user-1"), CancellationToken.None);

        Assert.Equal(3, result.Indexed);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Failed);

        knowledgeService.Verify(service => service.IndexDocumentAsync(
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task HandleAsync_WhenArtefactHasNoContent_CountsAsSkipped()
    {
        var projectId = Guid.NewGuid();
        var artefactWithContent = CreatePublishedArtefact(projectId, 1, "requirements/REQ-001.md", "text/markdown");
        var artefactWithoutContent = CreatePublishedArtefact(projectId, 2, "requirements/REQ-002.md", "text/markdown");
        var manifest = new List<Artefact> { artefactWithContent, artefactWithoutContent };

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetProjectArtefactManifestAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(artefactWithContent.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("# content");
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(artefactWithoutContent.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var knowledgeService = new Mock<IKnowledgeService>();

        var handler = new ReindexProjectArtefactsCommandHandler(
            artefactRepository.Object,
            artefactStorageService.Object,
            knowledgeService.Object,
            NullLogger<ReindexProjectArtefactsCommandHandler>.Instance);

        var result = await handler.Handle(new ReindexProjectArtefactsCommand(projectId, "user-1"), CancellationToken.None);

        Assert.Equal(1, result.Indexed);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);

        knowledgeService.Verify(service => service.IndexDocumentAsync(
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenIndexingThrows_CountsAsFailed()
    {
        var projectId = Guid.NewGuid();
        var artefact1 = CreatePublishedArtefact(projectId, 1, "requirements/REQ-001.md", "text/markdown");
        var artefact2 = CreatePublishedArtefact(projectId, 2, "requirements/REQ-002.md", "text/markdown");
        var manifest = new List<Artefact> { artefact1, artefact2 };

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetProjectArtefactManifestAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# content");

        var knowledgeService = new Mock<IKnowledgeService>();
        knowledgeService
            .Setup(service => service.IndexDocumentAsync(
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                artefact2.FilePath,
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("index failure"));

        var handler = new ReindexProjectArtefactsCommandHandler(
            artefactRepository.Object,
            artefactStorageService.Object,
            knowledgeService.Object,
            NullLogger<ReindexProjectArtefactsCommandHandler>.Instance);

        var result = await handler.Handle(new ReindexProjectArtefactsCommand(projectId, "user-1"), CancellationToken.None);

        Assert.Equal(1, result.Indexed);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(1, result.Failed);
    }

    [Fact]
    public async Task HandleAsync_OnlyIndexesTextMarkdownAndTextPlain()
    {
        var projectId = Guid.NewGuid();
        var markdownArtefact = CreatePublishedArtefact(projectId, 1, "requirements/REQ-001.md", "text/markdown");
        var plainArtefact = CreatePublishedArtefact(projectId, 2, "notes/NOTE-001.txt", "text/plain");
        var htmlArtefact = CreatePublishedArtefact(projectId, 3, "prototype/index.html", "text/html");
        var manifest = new List<Artefact> { markdownArtefact, plainArtefact, htmlArtefact };

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetProjectArtefactManifestAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var artefactStorageService = new Mock<IArtefactStorageService>();
        artefactStorageService
            .Setup(storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("content");

        var knowledgeService = new Mock<IKnowledgeService>();

        var handler = new ReindexProjectArtefactsCommandHandler(
            artefactRepository.Object,
            artefactStorageService.Object,
            knowledgeService.Object,
            NullLogger<ReindexProjectArtefactsCommandHandler>.Instance);

        var result = await handler.Handle(new ReindexProjectArtefactsCommand(projectId, "user-1"), CancellationToken.None);

        knowledgeService.Verify(service => service.IndexDocumentAsync(
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        Assert.Equal(2, result.Indexed);
    }

    [Fact]
    public async Task HandleAsync_WhenNoPublishedArtefacts_ReturnsAllZero()
    {
        var projectId = Guid.NewGuid();

        var artefactRepository = new Mock<IArtefactRepository>();
        artefactRepository
            .Setup(repository => repository.GetProjectArtefactManifestAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact>());

        var artefactStorageService = new Mock<IArtefactStorageService>();
        var knowledgeService = new Mock<IKnowledgeService>();

        var handler = new ReindexProjectArtefactsCommandHandler(
            artefactRepository.Object,
            artefactStorageService.Object,
            knowledgeService.Object,
            NullLogger<ReindexProjectArtefactsCommandHandler>.Instance);

        var result = await handler.Handle(new ReindexProjectArtefactsCommand(projectId, "user-1"), CancellationToken.None);

        Assert.Equal(0, result.Indexed);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Failed);

        knowledgeService.Verify(service => service.IndexDocumentAsync(
                It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Artefact CreatePublishedArtefact(
        Guid projectId,
        int version,
        string filePath,
        string contentType)
    {
        return Artefact.CreateS3Artefact(
            projectId,
            version,
            filePath,
            $"projects/{projectId}/artefacts/{filePath}/v{version}",
            contentType,
            128,
            "user-1",
            TimeProvider.System,
            isPublished: true);
    }
}
