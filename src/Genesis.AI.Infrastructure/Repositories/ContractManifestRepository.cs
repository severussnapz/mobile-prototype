using Genesis.AI.Domain.AggregatesModel.ContractManifestAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public class ContractManifestRepository : IContractManifestRepository
{
    private readonly GenesisAiDbContext _context;

    public ContractManifestRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ContractManifest?> GetLatestForProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.ContractManifests
            .Include(contractManifest => contractManifest.Pins)
            .AsNoTracking()
            .Where(contractManifest => contractManifest.ProjectId == projectId)
            .OrderByDescending(contractManifest => contractManifest.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }
}