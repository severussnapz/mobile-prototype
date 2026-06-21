namespace Genesis.AI.Domain.Interfaces;

public interface IPipelineReadinessService
{
    Task<PipelineReadinessResult> GetReadinessAsync(
        Guid projectId,
        IReadOnlyDictionary<string, string> reqContents,
        CancellationToken cancellationToken);
}
