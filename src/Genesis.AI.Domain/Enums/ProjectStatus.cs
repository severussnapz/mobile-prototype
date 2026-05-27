using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

public enum ProjectStatus
{
    [PgName("discovery")]
    Discovery,

    [PgName("in_progress")]
    InProgress,

    [PgName("complete")]
    Complete,

    [PgName("archived")]
    Archived
}
