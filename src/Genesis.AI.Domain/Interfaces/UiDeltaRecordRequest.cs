namespace Genesis.AI.Domain.Interfaces;

public sealed record UiDeltaRecordRequest(
    Guid ProjectId,
    Guid StageId,
    string? RequirementId,
    string TargetId,
    string FilePath,
    string OperationType,
    string SourceType,
    string? UserRequest,
    string BeforeSummary,
    string AfterSummary,
    string CreatedBy,
    Guid? ConversationId = null,
    Guid? MessageId = null);
