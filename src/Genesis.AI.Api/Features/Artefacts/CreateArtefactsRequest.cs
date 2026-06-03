namespace Genesis.AI.Api.Features.Artefacts;

public sealed class CreateArtefactsRequest
{
    public List<CreateArtefactRequestItem> Artefacts { get; init; } = [];
}
