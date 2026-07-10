using Genesis.AI.Domain.AggregatesModel.ContractManifestAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IContractManifestRepository
{
    Task<ContractManifest?> GetLatestForProjectAsync(Guid projectId, CancellationToken cancellationToken);
}