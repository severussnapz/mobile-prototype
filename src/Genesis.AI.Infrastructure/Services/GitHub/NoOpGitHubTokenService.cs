using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services.GitHub;

internal sealed class NoOpGitHubTokenService : IGitHubTokenService
{
    public Task<string> GetInstallationTokenAsync(string installationId, CancellationToken ct)
    {
        return Task.FromResult(string.Empty);
    }
}
