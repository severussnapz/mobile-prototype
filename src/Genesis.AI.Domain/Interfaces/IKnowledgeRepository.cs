using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate;
using Genesis.AI.Domain.Enums;
using Pgvector;

namespace Genesis.AI.Domain.Interfaces;

public interface IKnowledgeRepository
{
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Deletes all existing chunks for the given namespace + sourcePath + projectId,
    /// then inserts the new chunks. Atomic — both operations in one transaction.
    /// Embeddings must be computed BEFORE calling this method.
    /// </summary>
    Task IndexAsync(
        IReadOnlyList<KnowledgeDocument> documents,
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        string sourcePath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes cosine similarity search. Returns chunks ordered by similarity
    /// descending (score = 1 - distance, higher = more similar).
    /// topN is clamped to 20 by the caller.
    /// </summary>
    Task<IReadOnlyList<KnowledgeChunk>> QuerySimilarAsync(
        Vector queryEmbedding,
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        int topN,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all chunks matching namespace + sourcePath + projectId.
    /// NULL projectId matches only rows where project_id IS NULL.
    /// </summary>
    Task DeleteBySourcePathAsync(
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        string sourcePath,
        CancellationToken cancellationToken);
}
