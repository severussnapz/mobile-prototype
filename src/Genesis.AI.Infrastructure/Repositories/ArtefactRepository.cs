using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public class ArtefactRepository : IArtefactRepository
{
    private readonly GenesisAiDbContext _context;

    public ArtefactRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(Artefact artefact, CancellationToken cancellationToken)
    {
        await _context.Artefacts.AddAsync(artefact, cancellationToken);
    }

    public async Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tracked = await _context.Artefacts.FirstOrDefaultAsync(artefact => artefact.Id == id, cancellationToken);
        if (tracked is null)
        {
            return;
        }

        _context.Artefacts.Remove(tracked);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Artefact?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Artefacts
            .FirstOrDefaultAsync(artefact => artefact.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Artefact>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.Artefacts
            .AsNoTracking()
            .Where(artefact => artefact.ProjectId == projectId && artefact.IsPublished)
            .OrderByDescending(artefact => artefact.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextVersionAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var maxVersion = await _context.Artefacts
            .Where(artefact => artefact.ProjectId == projectId && artefact.IsPublished)
            .MaxAsync(artefact => (int?)artefact.Version, cancellationToken);

        return (maxVersion ?? 0) + 1;
    }

    public async Task<int> GetNextVersionForFileAsync(Guid projectId, string filePath, CancellationToken cancellationToken)
    {
        var maxVersion = await _context.Artefacts
            .Where(artefact => artefact.ProjectId == projectId && artefact.FilePath == filePath)
            .MaxAsync(artefact => (int?)artefact.Version, cancellationToken);

        return (maxVersion ?? 0) + 1;
    }

    public async Task DeletePreviousVersionsAsync(Guid projectId, string filePath, int currentVersion, CancellationToken cancellationToken)
    {
        var deleteQuery = _context.Artefacts
            .Where(artefact => artefact.ProjectId == projectId && artefact.FilePath == filePath && artefact.Version < currentVersion);

        try
        {
            await deleteQuery.ExecuteDeleteAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Fallback for providers (for example EF InMemory in integration tests)
            // that do not support ExecuteDelete/ExecuteDeleteAsync.
            var previousVersions = await deleteQuery.ToListAsync(cancellationToken);
            if (previousVersions.Count == 0)
            {
                return;
            }

            _context.Artefacts.RemoveRange(previousVersions);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Artefact>> GetProjectArtefactManifestAsync(Guid projectId, CancellationToken cancellationToken)
    {
        // Get the latest version of each file path (deduplicated).
        // Uses a subquery to find max version per file_path, then joins back —
        // the GroupBy + First() pattern fails EF Core query compilation.
        var latestVersions = _context.Artefacts
            .Where(artefact => artefact.ProjectId == projectId && artefact.IsPublished)
            .GroupBy(artefact => artefact.FilePath)
            .Select(group => new { FilePath = group.Key, MaxVersion = group.Max(artefact => artefact.Version) });

        return await _context.Artefacts
            .AsNoTracking()
            .Where(artefact => artefact.ProjectId == projectId && artefact.IsPublished)
            .Join(
                latestVersions,
                artefact => new { artefact.FilePath, Version = artefact.Version },
                latest => new { latest.FilePath, Version = latest.MaxVersion },
                (artefact, _) => artefact)
            .OrderBy(artefact => artefact.FilePath)
            .ToListAsync(cancellationToken);
    }

    public async Task<Artefact?> GetByProjectAndFilePathAsync(Guid projectId, string filePath, CancellationToken cancellationToken)
    {
        return await _context.Artefacts
            .AsNoTracking()
            .Where(artefact => artefact.ProjectId == projectId && artefact.FilePath == filePath && artefact.IsPublished)
            .OrderByDescending(artefact => artefact.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Artefact?> GetLatestDraftByProjectAndFilePathAsync(Guid projectId, string filePath, CancellationToken cancellationToken)
    {
        return await _context.Artefacts
            .AsNoTracking()
            .Where(artefact => artefact.ProjectId == projectId && artefact.FilePath == filePath && !artefact.IsPublished)
            .OrderByDescending(artefact => artefact.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLatestArtefactTimestampAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.Artefacts
            .Where(artefact => artefact.ProjectId == projectId && artefact.IsPublished)
            .MaxAsync(artefact => (DateTimeOffset?)artefact.CreatedAt, cancellationToken);
    }

    public async Task<bool> HasRequirementArtefactAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.Artefacts
            .AnyAsync(
                artefact => artefact.ProjectId == projectId
                    && artefact.IsPublished
                    && artefact.FilePath.StartsWith("requirements/REQ-")
                    && artefact.FilePath.EndsWith(".md"),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Artefact>> GetVersionsByFilePathAsync(Guid projectId, string filePath, CancellationToken cancellationToken)
    {
        return await _context.Artefacts
            .AsNoTracking()
            .Where(artefact => artefact.ProjectId == projectId && artefact.FilePath == filePath && artefact.IsPublished)
            .OrderByDescending(artefact => artefact.Version)
            .ToListAsync(cancellationToken);
    }
    public async Task<Artefact?> GetPreviousVersionAsync(
        Guid projectId,
        string filePath,
        CancellationToken cancellationToken)
    {
        return await _context.Artefacts
            .Where(artefact => artefact.ProjectId == projectId &&
                               artefact.FilePath == filePath)
            .OrderByDescending(artefact => artefact.Version)
            .Skip(1)
            .FirstOrDefaultAsync(cancellationToken);
    }


}
