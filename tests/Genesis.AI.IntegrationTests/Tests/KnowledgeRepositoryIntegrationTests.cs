using Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure;
using Genesis.AI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;
using Testcontainers.PostgreSql;
using Xunit;

namespace Genesis.AI.IntegrationTests.Tests;

/// <summary>
/// Minimal IMediator stub for testing — no actual MediatR logic needed.
/// </summary>
#pragma warning disable CA1822
internal sealed class NullMediator : MediatR.IMediator
{
    // IPublisher members
    public Task Publish(MediatR.INotification notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishAsync(MediatR.INotification notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    // IMediator.Send<TResponse> (IRequest<TResponse>)
    public Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    // IMediator.Send<TRequest, TResponse> (TRequest : IRequest<TResponse>)
    public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : MediatR.IRequest<TResponse>
        => throw new NotImplementedException();

    // ISender.Send<TRequest> (TRequest : IRequest)
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : MediatR.IRequest
        => throw new NotImplementedException();

    // ISender.Send(object)
    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    // IMediator.Send(IBaseRequest) — legacy interface
    public Task<object?> Send(MediatR.IBaseRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    // ISender.CreateStream<TResponse>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(MediatR.IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    // ISender.CreateStream(object)
    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    // ISender.Publish<TNotification>
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : MediatR.INotification
        => Task.CompletedTask;
}
#pragma warning restore CA1822

/// <summary>
/// Shared container lifecycle for all KnowledgeRepositoryIntegrationTests.
/// Starts PostgreSQL once, shared across all 4 tests, torn down after all 4 complete.
/// </summary>
public class KnowledgeRepositoryFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private GenesisAiDbContext? _context;
    private string? _connectionString;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;
    private bool _dockerAvailable = true;

    public bool DockerAvailable => _dockerAvailable;
    public GenesisAiDbContext? Context => _context;

    public async ValueTask InitializeAsync()
    {
        // Disable Ryuk resource reaper — known incompatibility with Colima on macOS
        // Ryuk tries to bind-mount Docker socket which fails on Colima
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
        
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("pgvector/pgvector:pg17")
                .WithDatabase("genesis_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();

            var connectionString = _container.GetConnectionString();
            _connectionString = connectionString;

            // Build NpgsqlDataSource with pgvector support
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseVector();
            dataSourceBuilder.MapEnum<KnowledgeNamespace>("knowledge_namespace");

            var dataSource = dataSourceBuilder.Build();

            // Run migrations V18 and V19
            var repoRoot = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            var v18 = await File.ReadAllTextAsync(
                Path.Combine(repoRoot, "db", "migrations", "V18__enable_pgvector.sql"),
                _cancellationToken);
            var v19 = await File.ReadAllTextAsync(
                Path.Combine(repoRoot, "db", "migrations", "V19__add_knowledge_document.sql"),
                _cancellationToken);

            await using var conn = dataSource.OpenConnection();
            using var cmd = conn.CreateCommand();
            
            // Create uuid-ossp extension before applying pgvector migration
            cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";";
            await cmd.ExecuteNonQueryAsync();
            
            cmd.CommandText = v18;
            await cmd.ExecuteNonQueryAsync();
            
            cmd.CommandText = v19;
            await cmd.ExecuteNonQueryAsync();

            // Close connection and dispose dataSource to force Npgsql to reload type cache
            await conn.CloseAsync();
            await dataSource.DisposeAsync();

            // Rebuild dataSource with fresh type information after migrations
            var newDataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            newDataSourceBuilder.UseVector();
            newDataSourceBuilder.MapEnum<KnowledgeNamespace>("knowledge_namespace");
            var newDataSource = newDataSourceBuilder.Build();

            // Create DbContext with real Npgsql provider
            var options = new DbContextOptionsBuilder<GenesisAiDbContext>()
                .UseNpgsql(newDataSource, npgsqlOptions =>
                {
                    npgsqlOptions.UseVector();
                    npgsqlOptions.MapEnum<KnowledgeNamespace>("knowledge_namespace");
                })
                .Options;

            _context = new GenesisAiDbContext(
                options,
                new NullMediator()); // No MediatR needed for these tests
        }
        catch (Exception)
        {
            // Catch ANY exception during Docker/database initialization
            _dockerAvailable = false;
            
            // Ensure container is disposed if it was partially started
            if (_container != null)
            {
                try
                {
                    await _container.StopAsync();
                    await _container.DisposeAsync();
                }
                catch
                {
                    // Ignore disposal errors
                }
                _container = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        if (_dockerAvailable && _container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a fresh GenesisAiDbContext instance using the fixture's connection string.
    /// Used for concurrent operations that cannot share a single DbContext.
    /// </summary>
    public GenesisAiDbContext CreateDbContext()
    {
        if (_connectionString == null)
        {
            throw new InvalidOperationException("Connection string not initialized. Container may not have started.");
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
        dataSourceBuilder.UseVector();
        dataSourceBuilder.MapEnum<KnowledgeNamespace>("knowledge_namespace");
        var dataSource = dataSourceBuilder.Build();

        var options = new DbContextOptionsBuilder<GenesisAiDbContext>()
            .UseNpgsql(dataSource, npgsqlOptions =>
            {
                npgsqlOptions.UseVector();
                npgsqlOptions.MapEnum<KnowledgeNamespace>("knowledge_namespace");
            })
            .Options;

        return new GenesisAiDbContext(options, new NullMediator());
    }
}

public class KnowledgeRepositoryIntegrationTests : IClassFixture<KnowledgeRepositoryFixture>
{
    private readonly KnowledgeRepositoryFixture _fixture;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    public KnowledgeRepositoryIntegrationTests(KnowledgeRepositoryFixture fixture)
    {
        _fixture = fixture;
    }

#pragma warning disable CA1859
    private IKnowledgeRepository CreateRepository() =>
        new KnowledgeRepository(_fixture.Context!);
#pragma warning restore CA1859

    /// <summary>
    /// Creates a deterministic non-zero embedding vector for testing.
    /// Each seed value produces a different but consistent vector.
    /// </summary>
    private static Vector CreateEmbedding(int seed)
    {
        var random = new Random(seed);
        var floats = new float[1024];
        for (int i = 0; i < floats.Length; i++)
        {
            floats[i] = (float)random.NextDouble();
        }
        return new Vector(floats);
    }

    [Fact]
    [Trait("Category", "RequiresDocker")]
    public async Task IndexAsync_DeletesExistingChunks_BeforeInsertingNew()
    {
        if (!_fixture.DockerAvailable)
        {
            Assert.Skip("Docker is not available on this machine.");
        }
        
        // Setup: Index a document with 2 chunks
        var repo = CreateRepository();
        var projectId = Guid.NewGuid();
        var sourcePath = "requirements/REQ-001.md";
        var namespace_ = KnowledgeNamespace.ProjectArtefact;

        var chunk1 = KnowledgeDocument.Create(
            namespace_, projectId, sourcePath, 0,
            "First chunk content",
            CreateEmbedding(1),
            new Dictionary<string, string> { ["chunkIndex"] = "0" },
            TimeProvider.System);

        var chunk2 = KnowledgeDocument.Create(
            namespace_, projectId, sourcePath, 1,
            "Second chunk content",
            CreateEmbedding(2),
            new Dictionary<string, string> { ["chunkIndex"] = "1" },
            TimeProvider.System);

        await repo.IndexAsync(new[] { chunk1, chunk2 }, namespace_, projectId, sourcePath, _cancellationToken);

        var countAfterFirstIndex = await _fixture.Context!.KnowledgeDocument
            .CountAsync(k => k.SourcePath == sourcePath && k.ProjectId == projectId, _cancellationToken);
        Assert.Equal(2, countAfterFirstIndex);

        // Action: Index same sourcePath with 1 new chunk
        var newChunk = KnowledgeDocument.Create(
            namespace_, projectId, sourcePath, 0,
            "New single chunk content (replaced old chunks)",
            new Vector(new float[1024]),
            new Dictionary<string, string> { ["chunkIndex"] = "0" },
            TimeProvider.System);

        await repo.IndexAsync(new[] { newChunk }, namespace_, projectId, sourcePath, _cancellationToken);

        // Assert: Only 1 chunk exists for that sourcePath
        var finalCount = await _fixture.Context!.KnowledgeDocument
            .CountAsync(k => k.SourcePath == sourcePath && k.ProjectId == projectId, _cancellationToken);
        Assert.Equal(1, finalCount);

        var finalChunks = await _fixture.Context!.KnowledgeDocument
            .Where(k => k.SourcePath == sourcePath && k.ProjectId == projectId)
            .ToListAsync(_cancellationToken);
        Assert.Single(finalChunks);
        Assert.Equal("New single chunk content (replaced old chunks)", finalChunks[0].Content);
    }

    [Fact]
    [Trait("Category", "RequiresDocker")]
    public async Task IndexAsync_IsAtomic_WhenCalledTwiceConcurrently_NoOrphanedChunks()
    {
        if (!_fixture.DockerAvailable || _fixture.Context == null)
        {
            Assert.Skip("Docker is not available on this machine.");
        }
        
        // Setup: Two concurrent IndexAsync calls for the same sourcePath with different content
        // Use separate DbContext instances to avoid "connection is already in a transaction" errors
        await using var context1 = _fixture.CreateDbContext();
        await using var context2 = _fixture.CreateDbContext();
        var repo1 = new KnowledgeRepository(context1);
        var repo2 = new KnowledgeRepository(context2);

        var projectId = Guid.NewGuid();
        var sourcePath = "requirements/CONCURRENT.md";
        var namespace_ = KnowledgeNamespace.ProjectArtefact;

        var chunk1A = KnowledgeDocument.Create(
            namespace_, projectId, sourcePath, 0,
            "First call - chunk 1",
            CreateEmbedding(1),
            new Dictionary<string, string> { ["chunkIndex"] = "0" },
            TimeProvider.System);

        var chunk1B = KnowledgeDocument.Create(
            namespace_, projectId, sourcePath, 1,
            "First call - chunk 2",
            CreateEmbedding(2),
            new Dictionary<string, string> { ["chunkIndex"] = "1" },
            TimeProvider.System);

        var chunk2A = KnowledgeDocument.Create(
            namespace_, projectId, sourcePath, 0,
            "Second call - single chunk",
            CreateEmbedding(3),
            new Dictionary<string, string> { ["chunkIndex"] = "0" },
            TimeProvider.System);

        // Action: Run both concurrently with separate DbContext instances
        var task1 = repo1.IndexAsync(new[] { chunk1A, chunk1B }, namespace_, projectId, sourcePath, _cancellationToken);
        var task2 = repo2.IndexAsync(new[] { chunk2A }, namespace_, projectId, sourcePath, _cancellationToken);

        await Task.WhenAll(task1, task2);

        // Assert: Use a fresh context to verify the final state
        await using var verificationContext = _fixture.CreateDbContext();
        var finalCount = await verificationContext.KnowledgeDocument
            .CountAsync(k => k.SourcePath == sourcePath && k.ProjectId == projectId, _cancellationToken);

        Assert.True(finalCount == 1 || finalCount == 2,
            $"Expected 1 or 2 chunks after concurrent calls, but got {finalCount}");

        // Verify no chunk_index collisions within the same source path
        var chunks = await verificationContext.KnowledgeDocument
            .Where(k => k.SourcePath == sourcePath && k.ProjectId == projectId)
            .OrderBy(k => k.ChunkIndex)
            .ToListAsync(_cancellationToken);

        var chunkIndices = chunks.Select(c => c.ChunkIndex).ToList();
        Assert.Equal(chunkIndices.Distinct().Count(), chunkIndices.Count);
    }

    [Fact]
    [Trait("Category", "RequiresDocker")]
    public async Task QuerySimilarAsync_ReturnsChunksOrderedBySimilarityDescending()
    {
        if (!_fixture.DockerAvailable)
        {
            Assert.Skip("Docker is not available on this machine.");
        }
        
        // Setup: Index 3 documents with non-zero embeddings
        var repo = CreateRepository();
        var projectId = Guid.NewGuid();
        var namespace_ = KnowledgeNamespace.ProjectArtefact;

        // Create deterministic non-zero embeddings using a seed
        var embedding1 = CreateEmbedding(1);
        var embedding2 = CreateEmbedding(2);
        var embedding3 = CreateEmbedding(3);

        var doc1 = KnowledgeDocument.Create(
            namespace_, projectId, "req-cs.md", 0,
            "The patient has a clinical safety requirement",
            embedding1,
            new Dictionary<string, string>(),
            TimeProvider.System);

        var doc2 = KnowledgeDocument.Create(
            namespace_, projectId, "weather.md", 0,
            "The weather is sunny today in London",
            embedding2,
            new Dictionary<string, string>(),
            TimeProvider.System);

        var doc3 = KnowledgeDocument.Create(
            namespace_, projectId, "req-hazard.md", 0,
            "Clinical safety hazards must be documented",
            embedding3,
            new Dictionary<string, string>(),
            TimeProvider.System);

        await repo.IndexAsync(new[] { doc1 }, namespace_, projectId, "req-cs.md", _cancellationToken);
        await repo.IndexAsync(new[] { doc2 }, namespace_, projectId, "weather.md", _cancellationToken);
        await repo.IndexAsync(new[] { doc3 }, namespace_, projectId, "req-hazard.md", _cancellationToken);

        // Action: Query with a non-zero embedding similar to embedding1
        var queryEmbedding = CreateEmbedding(1);
        var results = await repo.QuerySimilarAsync(queryEmbedding, namespace_, projectId, 3, _cancellationToken);

        // Assert: Results are ordered by Score descending (higher = more similar)
        Assert.NotEmpty(results);
        Assert.True(results.Count <= 3);

        // Verify all scores are between 0.0 and 1.0 (1 - distance)
        Assert.All(results, chunk => Assert.True(chunk.Score >= 0.0 && chunk.Score <= 1.0,
            $"Score {chunk.Score} is outside valid range [0.0, 1.0]"));

        // Verify results are ordered by score descending
        var scoresInOrder = results.Select(r => r.Score).ToList();
        var sortedDescending = scoresInOrder.OrderByDescending(s => s).ToList();
        Assert.Equal(sortedDescending, scoresInOrder);
    }

    [Fact]
    [Trait("Category", "RequiresDocker")]
    public async Task DeleteBySourcePathAsync_RemovesAllChunksForSourcePath()
    {
        if (!_fixture.DockerAvailable)
        {
            Assert.Skip("Docker is not available on this machine.");
        }
        
        // Setup: Index 2 chunks under REQ-001 and 1 chunk under REQ-002
        var repo = CreateRepository();
        var projectId = Guid.NewGuid();
        var namespace_ = KnowledgeNamespace.ProjectArtefact;

        var req001Chunk1 = KnowledgeDocument.Create(
            namespace_, projectId, "requirements/REQ-001.md", 0,
            "REQ-001 content chunk 1",
            new Vector(new float[1024]),
            new Dictionary<string, string>(),
            TimeProvider.System);

        var req001Chunk2 = KnowledgeDocument.Create(
            namespace_, projectId, "requirements/REQ-001.md", 1,
            "REQ-001 content chunk 2",
            new Vector(new float[1024]),
            new Dictionary<string, string>(),
            TimeProvider.System);

        var req002Chunk1 = KnowledgeDocument.Create(
            namespace_, projectId, "requirements/REQ-002.md", 0,
            "REQ-002 content chunk 1",
            new Vector(new float[1024]),
            new Dictionary<string, string>(),
            TimeProvider.System);

        await repo.IndexAsync(new[] { req001Chunk1, req001Chunk2 }, namespace_, projectId, "requirements/REQ-001.md", _cancellationToken);
        await repo.IndexAsync(new[] { req002Chunk1 }, namespace_, projectId, "requirements/REQ-002.md", _cancellationToken);

        var countBefore = await _fixture.Context!.KnowledgeDocument
            .CountAsync(k => k.ProjectId == projectId, _cancellationToken);
        Assert.Equal(3, countBefore);

        // Action: Delete REQ-001
        await repo.DeleteBySourcePathAsync(namespace_, projectId, "requirements/REQ-001.md", _cancellationToken);

        // Assert: REQ-001 chunks = 0, REQ-002 chunks = 1
        var req001Count = await _fixture.Context!.KnowledgeDocument
            .CountAsync(k => k.SourcePath == "requirements/REQ-001.md" && k.ProjectId == projectId, _cancellationToken);
        var req002Count = await _fixture.Context!.KnowledgeDocument
            .CountAsync(k => k.SourcePath == "requirements/REQ-002.md" && k.ProjectId == projectId, _cancellationToken);

        Assert.Equal(0, req001Count);
        Assert.Equal(1, req002Count);
    }
}
