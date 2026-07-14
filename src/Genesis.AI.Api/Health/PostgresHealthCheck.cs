using Genesis.AI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Genesis.AI.Api.Health;

/// <summary>
/// Readiness health check that verifies connectivity to the PostgreSQL database
/// through the EF Core <see cref="GenesisAiDbContext"/>. Routing the check via
/// the DbContext means it reuses the IAM-authenticated data source in AWS.
/// </summary>
public sealed class PostgresHealthCheck(GenesisAiDbContext dbContext, ILogger<PostgresHealthCheck> logger)
    : IHealthCheck
{
    private readonly GenesisAiDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ILogger<PostgresHealthCheck> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL database is healthy.")
                : HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL database.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "PostgreSQL health check failed with an exception");
            return HealthCheckResult.Unhealthy("PostgreSQL health check failed.", exception);
        }
    }
}
