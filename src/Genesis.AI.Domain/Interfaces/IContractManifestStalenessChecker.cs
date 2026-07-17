namespace Genesis.AI.Domain.Interfaces;

public interface IContractManifestStalenessChecker
{
    Task<IReadOnlyList<string>> CheckStalenessAsync(
        Guid projectId,
        string manifestContent,
        CancellationToken cancellationToken);
}