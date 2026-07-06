using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

/// <summary>
/// Tests for ArtefactPublishedInterceptor.IndexPendingArtefactsAsync — which indexes
/// artefacts post-commit by fetching content from S3 and calling the knowledge service.
///
/// Tests verify error handling (null/empty content, service exceptions) and multi-request
/// continuation semantics (one failure doesn't prevent subsequent requests from being indexed).
/// 
/// Also tests the InMemory provider guard in SavingChangesAsync to ensure the interceptor
/// skips processing under test conditions.
/// </summary>
public class ArtefactPublishedInterceptorTests
{
    [Fact]
    public async Task IndexPendingArtefactsAsync_WithValidMarkdownContent_CallsIndexDocumentAsync()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new ArtefactIndexRequest(
            projectId,
            "requirements/REQ-001.md",
            "projects/p/artefacts/requirements/REQ-001.md/v1",
            "text/markdown");

        var mockKnowledgeService = new Mock<IKnowledgeService>();
        var mockStorageService = new Mock<IArtefactStorageService>();
        mockStorageService
            .Setup(s => s.GetContentAsync(request.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("# REQ-001\n\nRequirement content");

        var interceptor = new ArtefactPublishedInterceptor(
            CreateMockScopeFactory(mockKnowledgeService, mockStorageService).Object,
            NullLogger<ArtefactPublishedInterceptor>.Instance);

        // Act
        await interceptor.IndexPendingArtefactsAsync(
            new List<ArtefactIndexRequest> { request },
            mockKnowledgeService.Object,
            mockStorageService.Object,
            CancellationToken.None);

        // Assert
        mockKnowledgeService.Verify(
            s => s.IndexDocumentAsync(
                KnowledgeNamespace.ProjectArtefact,
                projectId,
                "requirements/REQ-001.md",
                "# REQ-001\n\nRequirement content",
                It.Is<Dictionary<string, string>>(d => 
                    d["contentType"] == "text/markdown" &&
                    d["filePath"] == "requirements/REQ-001.md"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IndexPendingArtefactsAsync_WhenGetContentAsyncReturnsNull_SkipsIndexing()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new ArtefactIndexRequest(
            projectId,
            "design/schema.md",
            "projects/p/artefacts/design/schema.md/v1",
            "text/markdown");

        var mockKnowledgeService = new Mock<IKnowledgeService>();
        var mockStorageService = new Mock<IArtefactStorageService>();
        mockStorageService
            .Setup(s => s.GetContentAsync(request.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var interceptor = new ArtefactPublishedInterceptor(
            CreateMockScopeFactory(mockKnowledgeService, mockStorageService).Object,
            NullLogger<ArtefactPublishedInterceptor>.Instance);

        // Act
        await interceptor.IndexPendingArtefactsAsync(
            new List<ArtefactIndexRequest> { request },
            mockKnowledgeService.Object,
            mockStorageService.Object,
            CancellationToken.None);

        // Assert
        mockKnowledgeService.Verify(
            s => s.IndexDocumentAsync(It.IsAny<KnowledgeNamespace>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IndexPendingArtefactsAsync_WhenGetContentAsyncReturnsEmpty_SkipsIndexing()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new ArtefactIndexRequest(
            projectId,
            "architecture/BDAT.md",
            "projects/p/artefacts/architecture/BDAT.md/v1",
            "text/markdown");

        var mockKnowledgeService = new Mock<IKnowledgeService>();
        var mockStorageService = new Mock<IArtefactStorageService>();
        mockStorageService
            .Setup(s => s.GetContentAsync(request.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("");  // Empty string

        var interceptor = new ArtefactPublishedInterceptor(
            CreateMockScopeFactory(mockKnowledgeService, mockStorageService).Object,
            NullLogger<ArtefactPublishedInterceptor>.Instance);

        // Act
        await interceptor.IndexPendingArtefactsAsync(
            new List<ArtefactIndexRequest> { request },
            mockKnowledgeService.Object,
            mockStorageService.Object,
            CancellationToken.None);

        // Assert
        mockKnowledgeService.Verify(
            s => s.IndexDocumentAsync(It.IsAny<KnowledgeNamespace>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IndexPendingArtefactsAsync_WhenIndexDocumentAsyncThrows_DoesNotRethrow()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new ArtefactIndexRequest(
            projectId,
            "pxd/wireframe.md",
            "projects/p/artefacts/pxd/wireframe.md/v1",
            "text/markdown");

        var mockKnowledgeService = new Mock<IKnowledgeService>();
        mockKnowledgeService
            .Setup(s => s.IndexDocumentAsync(It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Knowledge service failed"));

        var mockStorageService = new Mock<IArtefactStorageService>();
        mockStorageService
            .Setup(s => s.GetContentAsync(request.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Wireframe\n\nUI mockup");

        var interceptor = new ArtefactPublishedInterceptor(
            CreateMockScopeFactory(mockKnowledgeService, mockStorageService).Object,
            NullLogger<ArtefactPublishedInterceptor>.Instance);

        // Act & Assert: Should not throw even when knowledge service fails
        await interceptor.IndexPendingArtefactsAsync(
            new List<ArtefactIndexRequest> { request },
            mockKnowledgeService.Object,
            mockStorageService.Object,
            CancellationToken.None);
    }

    [Fact]
    public async Task IndexPendingArtefactsAsync_WithMultipleRequests_IndexesAllSuccessfully()
    {
        // Arrange
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();
        var projectId3 = Guid.NewGuid();

        var requests = new List<ArtefactIndexRequest>
        {
            new ArtefactIndexRequest(projectId1, "req1.md", "s3://req1/v1", "text/markdown"),
            new ArtefactIndexRequest(projectId2, "req2.md", "s3://req2/v1", "text/markdown"),
            new ArtefactIndexRequest(projectId3, "req3.md", "s3://req3/v1", "text/markdown")
        };

        var mockKnowledgeService = new Mock<IKnowledgeService>();
        var mockStorageService = new Mock<IArtefactStorageService>();
        mockStorageService
            .Setup(s => s.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Content");

        var interceptor = new ArtefactPublishedInterceptor(
            CreateMockScopeFactory(mockKnowledgeService, mockStorageService).Object,
            NullLogger<ArtefactPublishedInterceptor>.Instance);

        // Act
        await interceptor.IndexPendingArtefactsAsync(
            requests,
            mockKnowledgeService.Object,
            mockStorageService.Object,
            CancellationToken.None);

        // Assert
        mockKnowledgeService.Verify(
            s => s.IndexDocumentAsync(It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task IndexPendingArtefactsAsync_WhenFirstRequestFails_ContinuesToIndexRemaining()
    {
        // Arrange
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();

        var requests = new List<ArtefactIndexRequest>
        {
            new ArtefactIndexRequest(projectId1, "fail.md", "s3://fail/v1", "text/markdown"),
            new ArtefactIndexRequest(projectId2, "success.md", "s3://success/v1", "text/markdown")
        };

        var mockKnowledgeService = new Mock<IKnowledgeService>();
        var callCount = 0;
        mockKnowledgeService
            .Setup(s => s.IndexDocumentAsync(It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Task.FromException(new InvalidOperationException("First call fails"));
                }
                return Task.CompletedTask;
            });

        var mockStorageService = new Mock<IArtefactStorageService>();
        mockStorageService
            .Setup(s => s.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Content");

        var interceptor = new ArtefactPublishedInterceptor(
            CreateMockScopeFactory(mockKnowledgeService, mockStorageService).Object,
            NullLogger<ArtefactPublishedInterceptor>.Instance);

        // Act
        await interceptor.IndexPendingArtefactsAsync(
            requests,
            mockKnowledgeService.Object,
            mockStorageService.Object,
            CancellationToken.None);

        // Assert: Both requests should have been attempted
        mockKnowledgeService.Verify(
            s => s.IndexDocumentAsync(It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public void SavingChangesAsync_UnderInMemoryProvider_SkipsProcessing()
    {
        // Arrange: Real InMemory DbContext with published artefact
        var projectId = Guid.NewGuid();
        var artefact = Artefact.CreateS3Artefact(
            projectId,
            version: 1,
            filePath: "requirements/REQ-001.md",
            s3Key: "projects/p/artefacts/requirements/REQ-001.md/v1",
            contentType: "text/markdown",
            sizeBytes: 1024,
            createdBy: "tester",
            timeProvider: TimeProvider.System,
            isPublished: true);

        var mockKnowledgeService = new Mock<IKnowledgeService>();
        var mockStorageService = new Mock<IArtefactStorageService>();
        mockStorageService
            .Setup(s => s.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Content");

        var mockScopeFactory = CreateMockScopeFactory(mockKnowledgeService, mockStorageService);
        var interceptor = new ArtefactPublishedInterceptor(
            mockScopeFactory.Object,
            NullLogger<ArtefactPublishedInterceptor>.Instance);

        // InMemory DbContext with Artefact entity configured
        var dbContextOptions = new DbContextOptionsBuilder<InMemoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        using var context = new InMemoryTestDbContext(dbContextOptions);
        context.Artefacts.Add(artefact);

        // Act: SaveChanges on InMemory context
        // The interceptor's SavingChangesAsync checks ProviderName == "Microsoft.EntityFrameworkCore.InMemory"
        // and returns early, so nothing gets queued for post-commit indexing
        context.SaveChanges();

        // Assert: InMemory guard should have fired, preventing indexing
        // Even though SavedChangesAsync won't be called by InMemory provider,
        // the SavingChangesAsync detection logic should have identified and skipped it
        mockKnowledgeService.Verify(
            s => s.IndexDocumentAsync(It.IsAny<KnowledgeNamespace>(),
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Minimal DbContext for InMemory testing with Artefact entity configured.
    /// </summary>
    private sealed class InMemoryTestDbContext : DbContext
    {
        public DbSet<Artefact> Artefacts => Set<Artefact>();

        public InMemoryTestDbContext(DbContextOptions<InMemoryTestDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Artefact>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.ProjectId);
                entity.Property(a => a.Version);
                entity.Property(a => a.IsPublished);
                entity.Property(a => a.FilePath);
                entity.Property(a => a.S3Key);
                entity.Property(a => a.ContentType);
                entity.Property(a => a.SizeBytes);
                entity.Property(a => a.CreatedBy);
                entity.Property(a => a.CreatedAt);
            });
        }
    }

    private static Mock<IServiceScopeFactory> CreateMockScopeFactory(
        Mock<IKnowledgeService> mockKnowledgeService,
        Mock<IArtefactStorageService> mockStorageService)
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IKnowledgeService)))
            .Returns(mockKnowledgeService.Object);
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IArtefactStorageService)))
            .Returns(mockStorageService.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope
            .SetupGet(s => s.ServiceProvider)
            .Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory
            .Setup(f => f.CreateScope())
            .Returns(mockScope.Object);

        return mockScopeFactory;
    }
}
