namespace Genesis.AI.Api.Features.Decisions;

public sealed class UpdateDecisionRequest
{
    public string Title { get; init; } = null!;
    public string Context { get; init; } = null!;
    public string Decision { get; init; } = null!;
    public string Consequences { get; init; } = null!;
}
