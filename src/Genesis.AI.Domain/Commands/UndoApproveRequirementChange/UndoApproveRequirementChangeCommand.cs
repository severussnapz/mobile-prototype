namespace Genesis.AI.Domain.Commands.UndoApproveRequirementChange;

public sealed record UndoApproveRequirementChangeCommand(
    Guid ChangeId,
    string UndoneBy,
    string? UndoRationale);
