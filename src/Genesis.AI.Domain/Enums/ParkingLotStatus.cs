using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

public enum ParkingLotStatus
{
    [PgName("open")]
    Open,

    [PgName("resolved")]
    Resolved,

    [PgName("deferred")]
    Deferred
}
