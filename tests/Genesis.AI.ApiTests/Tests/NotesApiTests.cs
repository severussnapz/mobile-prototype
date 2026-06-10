using System.Net;
using System.Text.Json;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class NotesApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    #region Helpers

    private async Task<Guid> CreateProjectAsync()
    {
        var body = new
        {
            code = GenerateProjectCode("NOTE"),
            name = $"Notes Test {DateTime.UtcNow:HHmmss}",
            description = "Created for notes API tests",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };
        var response = await Msvc.Api.CreateProjectAsync(ValidToken, body);
        var content = await ReadContentAsync(response);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateNoteAsync(Guid projectId, string noteContent)
    {
        var response = await Msvc.Api.CreateNoteAsync(ValidToken, projectId, new { content = noteContent });
        var content = await ReadContentAsync(response);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    #endregion

    #region GET /api/v1/projects/{projectId}/notes

    [Fact]
    public async Task GetNotes_ForNewProject_ReturnsEmptyArray()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GetNotesAsync(ValidToken, projectId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task GetNotes_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.GetNotesAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    [Fact]
    public async Task GetNotes_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.GetNotesAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/notes

    [Fact]
    public async Task CreateNote_WithValidPayload_ReturnsCreated()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.CreateNoteAsync(
            ValidToken, projectId, new { content = "A note created by the API test." });
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.True(data.TryGetProperty("id", out _));
        Assert.Equal("A note created by the API test.", data.GetProperty("content").GetString());
    }

    [Fact]
    public async Task CreateNote_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.CreateNoteAsync(
            ValidToken, Guid.NewGuid(), new { content = "Orphan note." });

        AssertNotFound(response);
    }

    [Fact]
    public async Task CreateNote_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.CreateNoteAsync("", Guid.NewGuid(), new { content = "No token." });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region PATCH /api/v1/projects/{projectId}/notes/{noteId}

    [Fact]
    public async Task UpdateNote_WithValidPayload_ReturnsUpdatedContent()
    {
        var projectId = await CreateProjectAsync();
        var noteId = await CreateNoteAsync(projectId, "Original content.");

        var response = await Msvc.Api.UpdateNoteAsync(
            ValidToken, projectId, noteId, new { content = "Updated content." });
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Updated content.", doc.RootElement.GetProperty("data").GetProperty("content").GetString());
    }

    [Fact]
    public async Task UpdateNote_ForUnknownNote_ReturnsNotFound()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.UpdateNoteAsync(
            ValidToken, projectId, Guid.NewGuid(), new { content = "Does not exist." });

        AssertNotFound(response);
    }

    #endregion

    #region DELETE /api/v1/projects/{projectId}/notes/{noteId}

    [Fact]
    public async Task DeleteNote_WithValidId_ReturnsNoContent()
    {
        var projectId = await CreateProjectAsync();
        var noteId = await CreateNoteAsync(projectId, "Note to delete.");

        var response = await Msvc.Api.DeleteNoteAsync(ValidToken, projectId, noteId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNote_ForUnknownNote_ReturnsNotFound()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.DeleteNoteAsync(ValidToken, projectId, Guid.NewGuid());

        AssertNotFound(response);
    }

    #endregion
}
