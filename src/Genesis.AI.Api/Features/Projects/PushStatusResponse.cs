using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Projects;

public sealed record PushStatusResponse(
	[property: JsonPropertyName("unresolvedCount")] int UnresolvedCount,
	[property: JsonPropertyName("failedArtefactIds")] IReadOnlyList<Guid> FailedArtefactIds);