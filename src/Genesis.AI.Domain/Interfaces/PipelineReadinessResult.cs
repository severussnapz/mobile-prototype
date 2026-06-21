namespace Genesis.AI.Domain.Interfaces;

public sealed record PipelineReadinessResult(
    bool IsReady,
    IReadOnlyList<string> Blockers);
