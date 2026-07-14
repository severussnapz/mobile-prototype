using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Projects;

public sealed record PushActionResponse(
    [property: JsonPropertyName("userMessage")] string UserMessage);