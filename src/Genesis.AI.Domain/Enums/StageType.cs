using NpgsqlTypes;

namespace Genesis.AI.Domain.Enums;

public enum StageType
{
    [PgName("requirements_discovery")]
    RequirementsDiscovery,

    [PgName("prototype")]
    Prototype,

    [PgName("architecture")]
    Architecture,

    [PgName("design")]
    Design,

    [PgName("pxd")]
    Pxd,

    [PgName("clinical_safety")]
    ClinicalSafety,

    [PgName("information_governance")]
    InformationGovernance,

    [PgName("security")]
    Security,

    [PgName("normalisation")]
    Normalisation,

    [PgName("planning")]
    Planning
}
