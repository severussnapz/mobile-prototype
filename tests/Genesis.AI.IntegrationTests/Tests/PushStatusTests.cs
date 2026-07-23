using System.Net;
using System.Text.Json;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.IntegrationTests.Tests;

public sealed class PushStatusTests
{
    [Fact]
    public async Task GetPushStatus_NoFailures_ReturnsZeroCount()
    {
        var projectId = Guid.NewGuid();
        var repositoryMock = new Mock<IPushFailureLogRepository>();
        repositoryMock
            .Setup(repository => repository.GetUnresolvedCountAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        using var factory = new TestWebApplicationFactory(repositoryMock.Object);
        var client = factory.CreateAdminClient();

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/push-status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(0, document.RootElement.GetProperty("data").GetProperty("unresolvedCount").GetInt32());
    }

    [Fact]
    public async Task GetPushStatus_HasFailures_ReturnsCorrectCount()
    {
        var projectId = Guid.NewGuid();
        var repositoryMock = new Mock<IPushFailureLogRepository>();
        repositoryMock
            .Setup(repository => repository.GetUnresolvedCountAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        using var factory = new TestWebApplicationFactory(repositoryMock.Object);
        var client = factory.CreateAdminClient();

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/push-status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(3, document.RootElement.GetProperty("data").GetProperty("unresolvedCount").GetInt32());
    }

    [Fact]
    public async Task GetPushStatus_ProjectId_PassedToRepository()
    {
        var projectId = Guid.NewGuid();
        var repositoryMock = new Mock<IPushFailureLogRepository>();
        repositoryMock
            .Setup(repository => repository.GetUnresolvedCountAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        using var factory = new TestWebApplicationFactory(repositoryMock.Object);
        var client = factory.CreateAdminClient();

        await client.GetAsync($"/api/v1/projects/{projectId}/push-status");

        repositoryMock.Verify(
            repository => repository.GetUnresolvedCountAsync(projectId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}