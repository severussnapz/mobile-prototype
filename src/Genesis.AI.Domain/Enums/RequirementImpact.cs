using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

public enum RequirementImpact
{
    [PgName("cosmetic")]
    Cosmetic,

    [PgName("substantive")]
    Substantive
}
