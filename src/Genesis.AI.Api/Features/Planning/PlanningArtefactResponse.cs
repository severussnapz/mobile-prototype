namespace Genesis.AI.Api.Features.Planning;

public sealed class PlanningArtefactResponse
{
    public Guid ArtefactId { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
