namespace Genesis.AI.Api.Features.Artefacts;

public sealed class ArtefactSummaryResponse
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public int Version { get; init; }
    public string FilePath { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long? SizeBytes { get; init; }
    public string CreatedBy { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
}
