namespace Genesis.AI.Api.Features.Artefacts;

/// <summary>
/// Represents a version entry in the artefact version history.\n/// Used when recovering or viewing previous versions of a prototype or other file.
/// </summary>
public class ArtefactVersionResponse
{
    public Guid Id { get; init; }

    public int Version { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string? CreatedBy { get; init; }

    public long? SizeBytes { get; init; }

    public string ContentType { get; init; } = string.Empty;
}
