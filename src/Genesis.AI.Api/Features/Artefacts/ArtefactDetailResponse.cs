namespace Genesis.AI.Api.Features.Artefacts;

public sealed class ArtefactDetailResponse
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public int Version { get; init; }
    public string FilePath { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public string? Content { get; init; }
    public long? SizeBytes { get; init; }
    public string CreatedBy { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? GitHubPushedAt { get; init; }
}
