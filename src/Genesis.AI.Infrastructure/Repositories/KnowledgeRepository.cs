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
            await _context.KnowledgeDocuments
                .Where(k => k.Namespace == knowledgeNamespace
                    && k.SourcePath == sourcePath
                    && (projectId == null ? k.ProjectId == null : k.ProjectId == projectId))
                .ExecuteDeleteAsync(cancellationToken);

            // Insert new chunks
            _context.KnowledgeDocuments.AddRange(documents);
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
        var results = await _context.KnowledgeDocuments
            .Where(k => k.Namespace == knowledgeNamespace
                && (projectId == null ? k.ProjectId == null : k.ProjectId == projectId))
            .Select(k => new
            {
                k.Content,
                k.SourcePath,
                k.Metadata,
                Distance = k.Embedding.CosineDistance(queryEmbedding)
            })
            .OrderBy(x => x.Distance)
            .Take(topN)
            .ToListAsync(cancellationToken);

        return results
            .Select(x => new KnowledgeChunk(x.Content, x.SourcePath, 1.0 - x.Distance, x.Metadata))
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
        await _context.KnowledgeDocuments
            .Where(k => k.Namespace == knowledgeNamespace
                && k.SourcePath == sourcePath
                && (projectId == null ? k.ProjectId == null : k.ProjectId == projectId))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
