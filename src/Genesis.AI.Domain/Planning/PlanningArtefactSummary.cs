namespace Genesis.AI.Domain.Planning;

public sealed record PlanningArtefactSummary(
    Guid ArtefactId,
    string FilePath,
    int Version,
    DateTimeOffset UpdatedAt);
