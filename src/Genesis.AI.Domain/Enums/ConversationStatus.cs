using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

public enum ConversationStatus
{
    [PgName("active")]
    Active,

    [PgName("paused")]
    Paused,

    [PgName("completed")]
    Completed
}
