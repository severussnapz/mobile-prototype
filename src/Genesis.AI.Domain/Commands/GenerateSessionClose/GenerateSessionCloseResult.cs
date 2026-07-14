namespace Genesis.AI.Domain.Commands.GenerateSessionClose;

public sealed record GenerateSessionCloseResult(
    Guid ArtefactId,
    string FilePath,
    int Version
);