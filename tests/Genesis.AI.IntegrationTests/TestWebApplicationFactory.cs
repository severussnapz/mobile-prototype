using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.GitHub;
using Genesis.AI.Infrastructure;
using Genesis.AI.TestFramework;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Genesis.AI.IntegrationTests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Genesis.AI.Api.Program>
{
    private readonly MockTokenGenerator _tokenGenerator;
    private readonly string _databaseName;
    private readonly Mock<IAiService> _aiServiceMock;
    private readonly Mock<IPrototypeDemoEditService> _prototypeDemoEditServiceMock;
    private readonly IPushFailureLogRepository? _pushFailureLogRepository;

    public MockTokenGenerator TokenGenerator => _tokenGenerator;
    public Mock<IAiService> AiServiceMock => _aiServiceMock;
    public Mock<IPrototypeDemoEditService> PrototypeDemoEditServiceMock => _prototypeDemoEditServiceMock;

    public TestWebApplicationFactory(IPushFailureLogRepository? pushFailureLogRepository = null)
    {
        _tokenGenerator = new MockTokenGenerator();
        _databaseName = $"TestDb_{Guid.NewGuid()}";
        _aiServiceMock = new Mock<IAiService>();
        _prototypeDemoEditServiceMock = new Mock<IPrototypeDemoEditService>();
        _pushFailureLogRepository = pushFailureLogRepository;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "https://test-authority.example.com/v2.0/",
                ["Authentication:Audience"] = "test-audience",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core / Npgsql registrations
            var descriptorsToRemove = services.Where(descriptor =>
                descriptor.ServiceType == typeof(DbContextOptions<GenesisAiDbContext>) ||
                descriptor.ServiceType == typeof(GenesisAiDbContext) ||
                descriptor.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                descriptor.ServiceType.FullName?.Contains("Npgsql") == true ||
                descriptor.ServiceType.FullName?.Contains("HealthCheck") == true
            ).ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database
            var dbName = _databaseName;
            services.AddDbContext<GenesisAiDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            // Re-add basic health checks (Npgsql-specific ones were removed above)
            services.AddHealthChecks();

            // Mock IAiService — integration tests don't call AWS Bedrock
            services.AddSingleton(_aiServiceMock.Object);

            // Mock IPrototypeDemoEditService — the edit controller's HTTP/auth/serialisation
            // contract is exercised here; the service's internal validation is covered by its
            // own Day 2b unit tests. Last registration wins, overriding the scoped Bedrock impl.
            // Fake IArtefactStorageService — integration tests don't call S3/LocalStack.
            // Keeps content in memory so save/read round-trips work.
            var storageMock = new Mock<IArtefactStorageService>();
            var contentStore = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
            var binaryStore = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();
            storageMock
                .Setup(storage => storage.SaveContentAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((Guid projectId, string filePath, int version, string content, string _, CancellationToken _) =>
                {
                    var key = $"projects/{projectId}/artefacts/{filePath}/v{version}";
                    contentStore[key] = content;
                    return Task.FromResult(key);
                });
            storageMock
                .Setup(storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string key, CancellationToken _) =>
                    Task.FromResult(contentStore.TryGetValue(key, out var stored) ? stored : null));
            storageMock
                .Setup(storage => storage.SaveBinaryContentAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((Guid projectId, string filePath, int version, byte[] content, string _, CancellationToken _) =>
                {
                    var key = $"projects/{projectId}/artefacts/{filePath}/v{version}";
                    binaryStore[key] = content;
                    return Task.FromResult(key);
                });
            storageMock
                .Setup(storage => storage.GetBinaryContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string key, CancellationToken _) =>
                    Task.FromResult(binaryStore.TryGetValue(key, out var stored) ? stored : null));
            services.AddSingleton(storageMock.Object);

            // Mock IKnowledgeService — the ArtefactPublishedDomainEvent handler indexes published
            // artefacts into pgvector via Bedrock embeddings; integration tests don't call AWS.
            services.AddSingleton(new Mock<IKnowledgeService>().Object);

            if (_pushFailureLogRepository is not null)
            {
                services.AddSingleton(_pushFailureLogRepository);
            }
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGitHubTokenService>();
            services.AddSingleton<IGitHubTokenService>(_ =>
            {
                var mock = new Mock<IGitHubTokenService>();
                mock.Setup(service => service.GetInstallationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("test-token");
                return mock.Object;
            });

            services.RemoveAll<IGitHubContentsService>();
            services.AddSingleton<IGitHubContentsService>(_ =>
            {
                var mock = new Mock<IGitHubContentsService>();
                mock.Setup(service => service.PushFileAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<byte[]>(),
                        It.IsAny<string>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new GitHubPushResult("sha123", "https://github.com/test"));
                mock.Setup(service => service.FileExistsAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
                mock.Setup(service => service.GetFileShaAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((string?)null);
                return mock.Object;
            });

            services.RemoveAll<IGenesisStructureScaffolder>();
            services.AddScoped<IGenesisStructureScaffolder>(_ =>
            {
                var mock = new Mock<IGenesisStructureScaffolder>();
                mock.Setup(service => service.ScaffoldAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                return mock.Object;
            });

            services.RemoveAll<IGitHubArtefactPushService>();
            services.AddScoped<IGitHubArtefactPushService>(_ =>
            {
                var mock = new Mock<IGitHubArtefactPushService>();
                mock.Setup(service => service.PushAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                return mock.Object;
            });

            services.RemoveAll<ISecretEncryptionService>();
            services.AddSingleton<ISecretEncryptionService>(_ =>
            {
                var mock = new Mock<ISecretEncryptionService>();
                mock.Setup(service => service.Encrypt(It.IsAny<string>())).Returns("encrypted-test-value");
                mock.Setup(service => service.Decrypt(It.IsAny<string>())).Returns("decrypted-test-value");
                mock.Setup(service => service.Mask(It.IsAny<string>())).Returns("••••••••");
                mock.Setup(service => service.MaskWithSuffix(It.IsAny<string>(), It.IsAny<int>()))
                    .Returns("••••••••test");
                return mock.Object;
            });

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _tokenGenerator.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _tokenGenerator.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _tokenGenerator.SigningKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

            // Mock IPrototypeDemoEditService — must run after Infrastructure.AddInfrastructure()
            // so this singleton registration wins over the scoped Bedrock implementation.
            services.AddSingleton(_prototypeDemoEditServiceMock.Object);
        });

        builder.UseEnvironment("Testing");
    }

    public HttpClient CreateAdminClient()
    {
        return CreateClientWithToken(_tokenGenerator.CreateAdminToken());
    }

    public HttpClient CreateReadOnlyClient()
    {
        return CreateClientWithToken(_tokenGenerator.CreateReadOnlyToken());
    }

    public HttpClient CreateWriteClient()
    {
        return CreateClientWithToken(_tokenGenerator.CreateWriteToken());
    }

    public HttpClient CreateClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task SeedDatabaseAsync(Func<GenesisAiDbContext, Task> seedAction)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GenesisAiDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await seedAction(dbContext);
    }
}
