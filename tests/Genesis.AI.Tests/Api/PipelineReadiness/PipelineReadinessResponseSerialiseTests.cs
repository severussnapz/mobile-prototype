using System.Text.Json;
using Genesis.AI.Api.Features.PipelineReadiness;

namespace Genesis.AI.Tests.Api.PipelineReadiness;

public sealed class PipelineReadinessResponseSerialiseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void PipelineReadinessResponse_SerialisesAllFields()
    {
        // Arrange
        var response = new PipelineReadinessResponse(
            IsReady: false,
            Blockers: ["Missing stage prerequisites"]);

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert
        var root = JsonDocument.Parse(json).RootElement;

        Assert.True(root.TryGetProperty("isReady", out var isReadyElement), "isReady field missing");
        Assert.False(isReadyElement.GetBoolean());

        Assert.True(root.TryGetProperty("blockers", out var blockersElement), "blockers field missing");
        Assert.Equal("Missing stage prerequisites", blockersElement.EnumerateArray().Single().GetString());
    }
}
