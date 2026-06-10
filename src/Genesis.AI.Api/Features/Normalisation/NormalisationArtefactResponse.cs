namespace Genesis.AI.Api.Features.Normalisation;

public sealed class NormalisationArtefactResponse
{
    public Guid ArtefactId { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
