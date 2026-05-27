using System.Net;

namespace Genesis.AI.IntegrationTests.Tests;

public class AuthenticationTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public AuthenticationTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetProjects_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();
        var content = new StringContent(
            """{"code":"AUTH","name":"Auth Test","description":"Test","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/projects", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_WithExpiredToken_Returns401Unauthorised()
    {
        var token = _factory.TokenGenerator.CreateExpiredToken("genai-req.read");
        var client = _factory.CreateClientWithToken(token);

        var response = await client.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithReadOnlyToken_Returns403Forbidden()
    {
        var client = _factory.CreateReadOnlyClient();
        var content = new StringContent(
            """{"code":"READONLY","name":"Read Only Test","description":"Test","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/projects", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_WithReadOnlyToken_Returns403Forbidden()
    {
        var client = _factory.CreateReadOnlyClient();

        var response = await client.DeleteAsync($"/api/v1/projects/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
