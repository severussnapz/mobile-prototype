using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure;
using Genesis.AI.TestFramework;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Genesis.AI.IntegrationTests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Genesis.AI.Api.Program>
{
    private readonly MockTokenGenerator _tokenGenerator;
    private readonly string _databaseName;

    public MockTokenGenerator TokenGenerator => _tokenGenerator;

    public TestWebApplicationFactory()
    {
        _tokenGenerator = new MockTokenGenerator();
        _databaseName = $"TestDb_{Guid.NewGuid()}";
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
            var mockAiService = new Mock<IAiService>();
            services.AddSingleton(mockAiService.Object);
        });

        builder.ConfigureTestServices(services =>
        {
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
