using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly GenesisAiDbContext _context;

    public KnowledgeRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task IndexAsync(
        IReadOnlyList<KnowledgeDocument> documents,
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Delete existing chunks for this source path
            await _context.KnowledgeDocument
                .Where(knowledgeDocument => knowledgeDocument.Namespace == knowledgeNamespace
                    && knowledgeDocument.SourcePath == sourcePath
                    && (projectId == null ? knowledgeDocument.ProjectId == null : knowledgeDocument.ProjectId == projectId))
                .ExecuteDeleteAsync(cancellationToken);

            // Insert new chunks
            _context.KnowledgeDocument.AddRange(documents);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> QuerySimilarAsync(
        Vector queryEmbedding,
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        int topN,
        CancellationToken cancellationToken)
    {
        // Use LINQ-to-Entities with the Pgvector Vector API for cosine similarity.
        // The Vector.CosineDistance instance method translates to the pgvector <=> operator.
        // OrderBy before Select ensures the HNSW index is used for the ORDER BY.
        var results = await _context.KnowledgeDocument
            .Where(knowledgeDocument => knowledgeDocument.Namespace == knowledgeNamespace
                && (projectId == null ? knowledgeDocument.ProjectId == null : knowledgeDocument.ProjectId == projectId))
            .Select(knowledgeDocument => new
            {
                knowledgeDocument.Content,
                knowledgeDocument.SourcePath,
                knowledgeDocument.Metadata,
                Distance = knowledgeDocument.Embedding.CosineDistance(queryEmbedding)
            })
            .OrderBy(result => result.Distance)
            .Take(topN)
            .ToListAsync(cancellationToken);

        return results
            .Select(result => new KnowledgeChunk(result.Content, result.SourcePath, 1.0 - result.Distance, result.Metadata))
            .ToList();
    }

    public async Task DeleteBySourcePathAsync(
        KnowledgeNamespace knowledgeNamespace,
        Guid? projectId,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        // ponytail: ExecuteDeleteAsync executes directly as DELETE SQL, bypassing the
        // change tracker. SaveChangesAsync after it would be a no-op — omitted intentionally.
        await _context.KnowledgeDocument
            .Where(knowledgeDocument => knowledgeDocument.Namespace == knowledgeNamespace
                && knowledgeDocument.SourcePath == sourcePath
                && (projectId == null ? knowledgeDocument.ProjectId == null : knowledgeDocument.ProjectId == projectId))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
