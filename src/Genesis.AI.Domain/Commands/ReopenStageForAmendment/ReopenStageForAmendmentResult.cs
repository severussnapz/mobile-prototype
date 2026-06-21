namespace Genesis.AI.Domain.Commands.ReopenStageForAmendment;

public sealed record ReopenStageForAmendmentResult(
    bool IsSuccess,
    Guid? ConversationId = null,
    string? ErrorMessage = null);
