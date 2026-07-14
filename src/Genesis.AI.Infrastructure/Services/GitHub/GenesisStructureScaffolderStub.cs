using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services.GitHub;

internal sealed class GenesisStructureScaffolderStub : IGenesisStructureScaffolder
{
    public Task ScaffoldAsync(Guid projectId, string triggeredBy, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
