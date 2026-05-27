namespace Genesis.AI.Api.Dtos;

public sealed class CreateArtefactRequestItem
{
    public string FilePath { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? ContentType { get; init; }
}
