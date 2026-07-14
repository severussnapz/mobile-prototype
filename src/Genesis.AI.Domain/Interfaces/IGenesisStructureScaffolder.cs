namespace Genesis.AI.Domain.Interfaces;

public interface IGenesisStructureScaffolder
{
    Task<ScaffoldResult> ScaffoldAsync(Guid projectId, string triggeredBy, CancellationToken ct);
}
