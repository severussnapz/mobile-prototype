namespace Genesis.AI.Domain.Normalisation;

public sealed record NormalisationArtefactSummary(
    Guid ArtefactId,
    string FilePath,
    int Version,
    DateTimeOffset UpdatedAt);
