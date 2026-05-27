using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

public enum ParkingLotPriority
{
    [PgName("critical")]
    Critical,

    [PgName("high")]
    High,

    [PgName("medium")]
    Medium
}
