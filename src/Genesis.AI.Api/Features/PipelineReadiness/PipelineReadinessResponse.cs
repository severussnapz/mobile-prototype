namespace Genesis.AI.Api.Features.PipelineReadiness;

public sealed record PipelineReadinessResponse(
    bool IsReady,
    IReadOnlyList<string> Blockers);
