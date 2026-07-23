using System.Net;
using System.Text.Json;
using Genesis.AI.IntegrationTests;

namespace Genesis.AI.IntegrationTests.Tests;

/// <summary>
/// Verifies that a dedicated "types" Swagger document exists, is reachable, and
/// is free of the APIM-routing artefacts injected by SwaggerEndpointFilter and
/// CorsOptionsOperationFilter (which must appear only in the "v1" document).
///
/// All tests are RED until:
///   1. A second named Swagger doc "types" is registered in AddSwaggerGen (§1a).
///   2. The "types" doc is served via app.UseSwagger() / UseSwaggerUI() with
///      route template "swagger/{documentName}/swagger.json".
///   3. SupportNonNullableReferenceTypes() is added to AddSwaggerGen (§1c).
/// </summary>
public class SwaggerTypesDocumentTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SwaggerTypesDocumentTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    // ─── Document availability ────────────────────────────────────────────────

    [Fact]
    public async Task GetTypesDocument_WhenRequested_Returns200Ok()
    {
        // The "types" doc does not exist yet → currently 404. RED.
        var response = await _client.GetAsync("/swagger/types/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTypesDocument_WhenRequested_ReturnsValidJsonWithOpenApiVersion()
    {
        var response = await _client.GetAsync("/swagger/types/swagger.json");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        Assert.True(
            doc.RootElement.TryGetProperty("openapi", out var version),
            "Document must contain an 'openapi' version field.");
        Assert.StartsWith("3.", version.GetString());
    }

    // ─── APIM filter isolation ─────────────────────────────────────────────────

    [Fact]
    public async Task GetTypesDocument_WhenParsed_DoesNotContainSwaggerWildcardPath()
    {
        // SwaggerEndpointFilter must NOT run on the types doc.
        // If it does, NSwag generates a garbage method for /swagger/*.
        var doc = await FetchTypesDocumentAsync();

        Assert.False(
            doc.RootElement.GetProperty("paths").TryGetProperty("/swagger/*", out _),
            "Types document must not expose the /swagger/* APIM routing path.");
    }

    [Fact]
    public async Task GetTypesDocument_WhenParsed_ContainsNoOptionsOperations()
    {
        // CorsOptionsOperationFilter must NOT run on the types doc.
        // If it does, NSwag generates duplicate OPTIONS client methods.
        var doc = await FetchTypesDocumentAsync();

        var paths = doc.RootElement.GetProperty("paths");
        foreach (var path in paths.EnumerateObject())
        {
            Assert.False(
                path.Value.TryGetProperty("options", out _),
                $"Types document must not contain an OPTIONS operation on '{path.Name}'. " +
                "Remove CorsOptionsOperationFilter from the types document registration.");
        }
    }

    // ─── Real API paths present ────────────────────────────────────────────────

    [Fact]
    public async Task GetTypesDocument_WhenParsed_ContainsProjectsGetPath()
    {
        var doc = await FetchTypesDocumentAsync();

        Assert.True(
            doc.RootElement.GetProperty("paths").TryGetProperty("/api/v1/projects", out _),
            "Types document must expose /api/v1/projects so NSwag can generate project types.");
    }

    [Fact]
    public async Task GetTypesDocument_WhenParsed_ContainsConversationsPath()
    {
        var doc = await FetchTypesDocumentAsync();

        Assert.True(
            doc.RootElement.GetProperty("paths").TryGetProperty("/api/v1/conversations", out _),
            "Types document must expose /api/v1/conversations so NSwag can generate conversation types.");
    }

    // ─── Nullability configuration ─────────────────────────────────────────────

    [Fact]
    public async Task GetTypesDocument_ProjectResourceDescriptionSchema_IsMarkedNullable()
    {
        // SupportNonNullableReferenceTypes() must be called in AddSwaggerGen so that
        // string? properties emit "nullable: true" in the schema.
        // ProjectResource.Description is declared as string? — the canonical canary.
        var doc = await FetchTypesDocumentAsync();

        var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");

        Assert.True(
            schemas.TryGetProperty("ProjectResource", out var projectSchema),
            "ProjectResource schema must be present in the types document.");

        var properties = projectSchema.GetProperty("properties");

        Assert.True(
            properties.TryGetProperty("description", out var descriptionSchema),
            "ProjectResource must have a 'description' property in the schema.");

        Assert.True(
            descriptionSchema.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean(),
            "ProjectResource.description is declared as string? and must be marked nullable: true. " +
            "Add SupportNonNullableReferenceTypes() to AddSwaggerGen.");
    }

    // ─── v1 document is not broken ─────────────────────────────────────────────

    [Fact]
    public async Task GetV1Document_WhenTypesDocumentAdded_StillContainsSwaggerWildcardPath()
    {
        // Adding a second doc must not disturb the existing APIM doc.
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        Assert.True(
            doc.RootElement.GetProperty("paths").TryGetProperty("/swagger/*", out _),
            "The v1 APIM document must still contain the /swagger/* wildcard path.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<JsonDocument> FetchTypesDocumentAsync()
    {
        var response = await _client.GetAsync("/swagger/types/swagger.json");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body);
    }
}
