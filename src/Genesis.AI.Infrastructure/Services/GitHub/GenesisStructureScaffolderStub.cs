using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services.GitHub;

internal sealed class GenesisStructureScaffolderStub : IGenesisStructureScaffolder
{
    public Task<ScaffoldResult> ScaffoldAsync(Guid projectId, string triggeredBy, CancellationToken ct)
    {
        return Task.FromResult(ScaffoldResult.Success());
    }
}
