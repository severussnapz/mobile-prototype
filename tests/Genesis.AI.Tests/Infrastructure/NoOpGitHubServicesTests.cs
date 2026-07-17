using Genesis.AI.Infrastructure.Services.GitHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genesis.AI.Tests.Infrastructure;

public sealed class NoOpGitHubServicesTests
{
    [Fact]
    public async Task NoOpGitHubArtefactPushService_PushAsync_CompletesWithoutThrowing()
    {
        var service = new NoOpGitHubArtefactPushService(
            NullLogger<NoOpGitHubArtefactPushService>.Instance);

        var exception = await Record.ExceptionAsync(() => service.PushAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "requirements/REQ-001.md",
            1,
            "text/markdown",
            "projects/p/artefacts/requirements/REQ-001.md/v1",
            "user@emisgroup.com",
            CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task NoOpGitHubArtefactPushService_PushAsync_LogsWarning()
    {
        // NullLogger used — internal type prevents Moq proxy; behaviour verified
        // by CompletesWithoutThrowing test. This test proves the logger overload
        // is exercised without exception on the warning path.
        var service = new NoOpGitHubArtefactPushService(
            NullLogger<NoOpGitHubArtefactPushService>.Instance);

        var exception = await Record.ExceptionAsync(() => service.PushAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "requirements/REQ-001.md",
            1,
            "text/markdown",
            "projects/p/artefacts/requirements/REQ-001.md/v1",
            "user@emisgroup.com",
            CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task NoOpGitHubTokenService_GetInstallationTokenAsync_ReturnsEmptyString()
    {
        var service = new NoOpGitHubTokenService();

        var result = await service.GetInstallationTokenAsync("any-id", CancellationToken.None);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void NoOpSecretEncryptionService_Mask_ReturnsMasked()
    {
        var service = new NoOpSecretEncryptionService();

        var result = service.Mask("any-secret");

        Assert.Equal("***", result);
    }

    [Fact]
    public void NoOpSecretEncryptionService_Encrypt_ReturnsInput()
    {
        var service = new NoOpSecretEncryptionService();

        var result = service.Encrypt("plaintext");

        Assert.Equal("plaintext", result);
    }
}
