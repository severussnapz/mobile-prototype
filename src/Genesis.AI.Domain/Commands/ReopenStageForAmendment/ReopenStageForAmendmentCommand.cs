namespace Genesis.AI.Domain.Commands.ReopenStageForAmendment;

public sealed record ReopenStageForAmendmentCommand(
    Guid StageId,
    string ReqId);
