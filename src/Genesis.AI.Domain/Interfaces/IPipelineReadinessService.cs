namespace Genesis.AI.Domain.Interfaces;

public sealed record PipelineReadinessResult(
    bool IsReady,
    IReadOnlyList<string> Blockers);

public interface IPipelineReadinessService
{
    Task<PipelineReadinessResult> GetReadinessAsync(
        Guid projectId,
        IReadOnlyDictionary<string, string> reqContents,
        CancellationToken cancellationToken);
}
