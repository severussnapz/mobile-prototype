using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public record KnowledgeChunk(
    string Content,
    string SourcePath,
    double Score,
    Dictionary<string, string> Metadata);

public interface IKnowledgeService
{
    Task IndexDocumentAsync(
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        string sourcePath,
        string content,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeChunk>> QueryAsync(
        string query,
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        int topN,
        CancellationToken cancellationToken);

    Task DeleteBySourcePathAsync(
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        string sourcePath,
        CancellationToken cancellationToken);
}