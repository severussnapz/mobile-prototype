namespace Genesis.AI.Domain.Commands.RejectRequirementChange;

public sealed record RejectRequirementChangeCommand(
    Guid ProjectId,
    Guid ChangeId,
    string RejectedBy);
