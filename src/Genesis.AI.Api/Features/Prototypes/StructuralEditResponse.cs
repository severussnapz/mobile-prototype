namespace Genesis.AI.Api.Features.Prototypes;

public sealed class StructuralEditResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<string> AffectedPaths { get; set; } = [];
}
