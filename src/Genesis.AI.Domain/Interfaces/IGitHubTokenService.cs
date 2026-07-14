namespace Genesis.AI.Domain.Interfaces;

public interface IGitHubTokenService
{
    Task<string> GetInstallationTokenAsync(string installationId, CancellationToken ct);
}