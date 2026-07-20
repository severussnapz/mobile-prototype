using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public interface IContractManifestContextBuilder
{
    Task<string> BuildContractManifestContextAsync(Guid projectId, StageType stageType, CancellationToken cancellationToken);
}
