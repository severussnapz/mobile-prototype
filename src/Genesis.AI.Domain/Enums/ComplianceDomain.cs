using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

public enum ComplianceDomain
{
    [PgName("clinical_uk")]
    ClinicalUk,

    [PgName("generic")]
    Generic,

    [PgName("finance")]
    Finance
}
