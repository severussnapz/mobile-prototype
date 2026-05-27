using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

public enum MessageRole
{
    [PgName("user")]
    User,

    [PgName("assistant")]
    Assistant,

    [PgName("system")]
    System
}
