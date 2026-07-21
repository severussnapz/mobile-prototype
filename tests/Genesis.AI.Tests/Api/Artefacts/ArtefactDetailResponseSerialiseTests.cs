using System.Text.Json;
using Genesis.AI.Api.Features.Artefacts;

namespace Genesis.AI.Tests.Api.Artefacts;

public sealed class ArtefactDetailResponseSerialiseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void ArtefactDetailResponse_SerialisesAllFields()
    {
        // Arrange
        var response = new ArtefactDetailResponse
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Version = 7,
            FilePath = "requirements/REQ-001.md",
            ContentType = "text/markdown",
            Content = "# REQ-001",
            SizeBytes = 1234,
            CreatedBy = "user-1",
            CreatedAt = DateTimeOffset.Parse("2026-07-21T10:11:12+00:00"),
            GitHubPushedAt = DateTimeOffset.Parse("2026-07-21T12:13:14+00:00")
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert
        var root = JsonDocument.Parse(json).RootElement;

        Assert.True(root.TryGetProperty("id", out var idElement), "id field missing");
        Assert.Equal(response.Id, idElement.GetGuid());

        Assert.True(root.TryGetProperty("projectId", out var projectIdElement), "projectId field missing");
        Assert.Equal(response.ProjectId, projectIdElement.GetGuid());

        Assert.True(root.TryGetProperty("version", out var versionElement), "version field missing");
        Assert.Equal(response.Version, versionElement.GetInt32());

        Assert.True(root.TryGetProperty("filePath", out var filePathElement), "filePath field missing");
        Assert.Equal(response.FilePath, filePathElement.GetString());

        Assert.True(root.TryGetProperty("contentType", out var contentTypeElement), "contentType field missing");
        Assert.Equal(response.ContentType, contentTypeElement.GetString());

        Assert.True(root.TryGetProperty("content", out var contentElement), "content field missing");
        Assert.Equal(response.Content, contentElement.GetString());

        Assert.True(root.TryGetProperty("sizeBytes", out var sizeBytesElement), "sizeBytes field missing");
        Assert.Equal(response.SizeBytes, sizeBytesElement.GetInt64());

        Assert.True(root.TryGetProperty("createdBy", out var createdByElement), "createdBy field missing");
        Assert.Equal(response.CreatedBy, createdByElement.GetString());

        Assert.True(root.TryGetProperty("createdAt", out var createdAtElement), "createdAt field missing");
        Assert.Equal(response.CreatedAt, createdAtElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("gitHubPushedAt", out var gitHubPushedAtElement), "gitHubPushedAt field missing");
        Assert.Equal(response.GitHubPushedAt, gitHubPushedAtElement.GetDateTimeOffset());
    }
}
