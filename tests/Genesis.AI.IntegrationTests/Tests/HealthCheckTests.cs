using System.Net;

namespace Genesis.AI.IntegrationTests.Tests;

public class HealthCheckTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public HealthCheckTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task HealthEndpoint_WhenCalled_Returns200Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReadyEndpoint_WhenCalled_Returns200Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthz/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_WhenCalled_DoesNotExposeServerHeader()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.False(response.Headers.Contains("Server"));
    }
}
