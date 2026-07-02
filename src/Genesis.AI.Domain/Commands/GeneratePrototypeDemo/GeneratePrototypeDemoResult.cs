namespace Genesis.AI.Domain.Commands.GeneratePrototypeDemo;

/// <summary>
/// Result of a <see cref="GeneratePrototypeDemoCommand"/>. On success carries the
/// complete HTML document. On failure carries the reason only.
/// </summary>
public sealed record GeneratePrototypeDemoResult(
    GeneratePrototypeDemoStatus Status,
    string Html,
    string? ErrorDetail)
{
    public static GeneratePrototypeDemoResult Failure(GeneratePrototypeDemoStatus status, string errorDetail)
    {
        return new GeneratePrototypeDemoResult(status, string.Empty, errorDetail);
    }

    public static GeneratePrototypeDemoResult Succeeded(string html)
    {
        return new GeneratePrototypeDemoResult(GeneratePrototypeDemoStatus.Success, html, null);
    }
}
