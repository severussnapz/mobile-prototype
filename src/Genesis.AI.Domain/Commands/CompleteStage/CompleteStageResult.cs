namespace Genesis.AI.Domain.Commands.CompleteStage;

public record CompleteStageResult(
    bool Found,
    bool AlreadyComplete,
    string? ValidationError,
    Guid? StageId = null,
    string? StageType = null,
    string? Status = null);
