namespace Genesis.AI.Domain.Interfaces;

public interface IGenesisStructureScaffolder
{
    Task ScaffoldAsync(Guid projectId, string userErn, CancellationToken ct);
}
