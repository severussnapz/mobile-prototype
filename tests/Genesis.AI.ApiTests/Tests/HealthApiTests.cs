using System.Net;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class HealthApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    [Fact]
    public async Task GetHealth_WhenCalled_ReturnsHealthyStatus()
    {

        var response = await Msvc.Api.GetHealthAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", response.Content);
    }

    [Fact]
    public async Task GetHealth_WithoutToken_ReturnsOkWithoutAuthentication()
    {

        var response = await Msvc.Api.GetHealthAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthReady_WhenCalled_ReturnsHealthy()
    {

        var response = await Msvc.Api.GetHealthReadyAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", response.Content);
    }
}
