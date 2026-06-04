namespace Genesis.AI.Api.Features.Notes;

public sealed class NoteResource
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Content { get; init; } = null!;
    public string? AuthorErn { get; init; }
    public string? AuthorGivenName { get; init; }
    public string? AuthorFamilyName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
