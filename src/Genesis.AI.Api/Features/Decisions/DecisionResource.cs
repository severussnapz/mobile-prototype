namespace Genesis.AI.Api.Features.Decisions;

public sealed class DecisionResource
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = null!;
    public string Context { get; init; } = null!;
    public string Decision { get; init; } = null!;
    public string Consequences { get; init; } = null!;
    public string? AuthorErn { get; init; }
    public string? AuthorGivenName { get; init; }
    public string? AuthorFamilyName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
