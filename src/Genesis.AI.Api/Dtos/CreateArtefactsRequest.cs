namespace Genesis.AI.Api.Dtos;

public sealed class CreateArtefactsRequest
{
    public List<CreateArtefactRequestItem> Artefacts { get; init; } = [];
}
