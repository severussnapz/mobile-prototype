using System.Text.Json;
using Genesis.AI.Api.Features.Projects;

namespace Genesis.AI.Tests.Api.Projects;

public sealed class UpdateProjectGitHubResponseMappingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void UpdateProjectGitHubResponse_MapsAllResultFields()
    {
        // Arrange
        const string figmaPatPlaintext = "test-pat";
        const bool apiRepoVerified = true;
        const string? apiRepoError = null;
        const bool appRepoVerified = false;
        const string appRepoError = "Access denied";

        var response = new UpdateProjectGitHubResponse(
            figmaPatPlaintext,
            apiRepoVerified,
            apiRepoError,
            appRepoVerified,
            appRepoError);

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert
        Assert.NotEmpty(json);

        var jsonDocument = JsonDocument.Parse(json);
        var root = jsonDocument.RootElement;

        // Verify all five fields are present in JSON
        Assert.True(root.TryGetProperty("figmaPatPlaintext", out var figmaPatElement), "figmaPatPlaintext field missing");
        Assert.True(root.TryGetProperty("apiRepoVerified", out var apiRepoVerifiedElement), "apiRepoVerified field missing");
        Assert.True(root.TryGetProperty("apiRepoError", out var apiRepoErrorElement), "apiRepoError field missing");
        Assert.True(root.TryGetProperty("appRepoVerified", out var appRepoVerifiedElement), "appRepoVerified field missing");
        Assert.True(root.TryGetProperty("appRepoError", out var appRepoErrorElement), "appRepoError field missing");

        // Verify field values
        Assert.Equal(figmaPatPlaintext, figmaPatElement.GetString());
        Assert.Equal(apiRepoVerified, apiRepoVerifiedElement.GetBoolean());
        Assert.True(apiRepoErrorElement.ValueKind == JsonValueKind.Null, "apiRepoError should be null");
        Assert.Equal(appRepoVerified, appRepoVerifiedElement.GetBoolean());
        Assert.Equal(appRepoError, appRepoErrorElement.GetString());
    }
}
