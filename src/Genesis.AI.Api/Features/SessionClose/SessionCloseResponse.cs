namespace Genesis.AI.Api.Features.SessionClose;

public sealed record SessionCloseResponse(
    Guid ArtefactId,
    string FilePath,
    int Version
);