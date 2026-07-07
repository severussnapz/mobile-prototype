using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services.GitHub;

internal sealed class GenesisStructureScaffolderStub : IGenesisStructureScaffolder
{
    public Task ScaffoldAsync(Guid projectId, string userErn, CancellationToken ct)
        => Task.CompletedTask;
}
