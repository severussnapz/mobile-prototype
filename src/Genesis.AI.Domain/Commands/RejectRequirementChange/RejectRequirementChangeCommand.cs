namespace Genesis.AI.Domain.Commands.RejectRequirementChange;

public sealed record RejectRequirementChangeCommand(
    Guid ChangeId,
    string RejectedBy);
