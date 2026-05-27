using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

public enum PipelineStageStatus
{
    [PgName("not_started")]
    NotStarted,

    [PgName("in_progress")]
    InProgress,

    [PgName("complete")]
    Complete,

    [PgName("blocked")]
    Blocked
}
