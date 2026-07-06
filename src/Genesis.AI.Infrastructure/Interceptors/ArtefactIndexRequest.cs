namespace Genesis.AI.Infrastructure.Interceptors;

internal sealed record ArtefactIndexRequest(
    Guid ProjectId,
    string FilePath,
    string S3Key,
    string ContentType);
