using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Http;

/// <summary>
/// Standard error envelope for API responses. Serialises to <c>{ "errors": [ ... ] }</c>.
/// </summary>
public sealed class ApiErrorResponse
{
    [JsonPropertyName("errors")]
    public required IReadOnlyList<ApiError> Errors { get; init; }

    /// <summary>
    /// Creates an error response containing a single error with a title.
    /// </summary>
    public static ApiErrorResponse Create(string status, string title, string detail)
    {
        return new ApiErrorResponse
        {
            Errors = [new ApiError { Status = status, Title = title, Detail = detail }]
        };
    }

    /// <summary>
    /// Creates an error response containing a single error without a title.
    /// </summary>
    public static ApiErrorResponse Create(string status, string detail)
    {
        return new ApiErrorResponse
        {
            Errors = [new ApiError { Status = status, Detail = detail }]
        };
    }
}
