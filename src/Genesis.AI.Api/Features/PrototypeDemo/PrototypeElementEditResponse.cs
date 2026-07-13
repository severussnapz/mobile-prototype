using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.PrototypeDemo;

/// <summary>
/// HTTP response payload for the targeted single-element edit endpoint.
/// Wrapped in <see cref="Genesis.AI.Api.Http.ApiResponse{T}"/> so the wire
/// shape is <c>{ "data": { "status", "updatedOuterHtml", "rejectionReason" } }</c>.
///
/// <c>status</c> is the verbatim PascalCase enum name (e.g. <c>"Applied"</c>)
/// because the app-side <c>PrototypeElementEditStatus</c> union uses PascalCase.
/// Project-scoped resources use ConvertToKebabCase in AutoMapper (ProjectMappingProfile.cs);
/// this endpoint does not go through AutoMapper and must not apply that transform.
/// </summary>
public sealed class PrototypeElementEditResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("updatedOuterHtml")]
    public string UpdatedOuterHtml { get; init; } = string.Empty;

    [JsonPropertyName("updatedFullHtml")]
    public string? UpdatedFullHtml { get; init; }

    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; init; }
}
