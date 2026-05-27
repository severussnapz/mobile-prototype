using System.Net;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class SecurityHeadersApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    [Fact]
    public async Task GetProjects_WithValidToken_DoesNotExposeServerHeader()
    {

        var response = await Msvc.Api.GetProjectsAsync(ValidToken);

        // Assert — SEC-005: suppress Kestrel Server header
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Server"),
            "Server header should not be exposed (SEC-005)");
    }

    [Fact]
    public async Task GetProjects_WithValidToken_DoesNotExposeXPoweredByHeader()
    {

        var response = await Msvc.Api.GetProjectsAsync(ValidToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Powered-By"),
            "X-Powered-By header should not be exposed");
    }

    [Fact]
    public async Task GetProjects_WithValidToken_ReturnsJsonContentType()
    {

        var response = await Msvc.Api.GetProjectsAsync(ValidToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.ContentHeaders?.ContentType?.MediaType);
    }
}
