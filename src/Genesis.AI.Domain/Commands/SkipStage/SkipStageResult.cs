namespace Genesis.AI.Domain.Commands.SkipStage;

public record SkipStageResult(
    bool Found,
    string? ValidationError,
    Guid? StageId = null,
    string? StageType = null,
    string? Status = null);
