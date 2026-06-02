using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Http;

/// <summary>
/// Standard success envelope for API responses. Serialises to <c>{ "data": ... }</c>.
/// </summary>
/// <typeparam name="T">The type of the payload carried in the <c>data</c> member.</typeparam>
public sealed class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public required T Data { get; init; }
}
