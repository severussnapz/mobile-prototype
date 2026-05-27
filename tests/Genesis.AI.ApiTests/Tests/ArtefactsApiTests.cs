using System.Net;
using System.Text.Json;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class ArtefactsApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    #region Helpers

    private async Task<Guid> CreateProjectAsync()
    {
        var body = new
        {
            code = $"ART-{Guid.NewGuid():N}"[..10],
            name = $"Artefact Test {DateTime.UtcNow:HHmmss}",
            description = "Created for artefact API tests",
            complianceDomain = "Generic"
        };
        var response = await Msvc.Api.CreateProjectAsync(ValidToken, body);
        var content = await ReadContentAsync(response);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    #endregion

    #region GET /api/v1/projects/{projectId}/artefacts

    [Fact]
    public async Task GetArtefacts_WithValidProject_ReturnsOk()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GetArtefactsByProjectAsync(ValidToken, projectId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetArtefacts_WithoutToken_ReturnsUnauthorized()
    {

        var response = await Msvc.Api.GetArtefactsByProjectAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GET /api/v1/projects/{projectId}/artefacts/{artefactId}

    [Fact]
    public async Task GetArtefactById_WithNonExistentId_ReturnsNotFound()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GetArtefactByIdAsync(ValidToken, projectId, Guid.NewGuid());

        AssertNotFound(response);
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/artefacts

    [Fact]
    public async Task CreateArtefacts_WithValidPayload_ReturnsOk()
    {
        var projectId = await CreateProjectAsync();
        var body = new
        {
            artefacts = new[]
            {
                new
                {
                    filePath = "test/api-test-artefact.md",
                    content = "# API Test Artefact\n\nCreated by automated test.",
                    contentType = "text/markdown"
                }
            }
        };

        var response = await Msvc.Api.CreateArtefactsAsync(ValidToken, projectId, body);

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created,
            $"Expected OK or Created but got {response.StatusCode}");
    }

    [Fact]
    public async Task CreateArtefacts_WithoutToken_ReturnsUnauthorized()
    {
        var body = new
        {
            artefacts = new[]
            {
                new { filePath = "test.md", content = "test", contentType = "text/markdown" }
            }
        };

        var response = await Msvc.Api.CreateArtefactsAsync("", Guid.NewGuid(), body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
