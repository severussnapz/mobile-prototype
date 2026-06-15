using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain;

/// <summary>
/// Lightweight routing context resolved at the start of each streaming turn.
/// Carries the stage type, current phase, whether this is the first user
/// message, and artefact-derived signals used by phase-specific skills to
/// select fast-track paths or pre-fill context.
/// </summary>
public sealed record RoutingContext(
    StageType StageType,
    int CurrentPhase,
    bool IsFirstMessage,
    bool SwaggerPresent = false,
    bool PrototypePresent = false,
    bool HazardRegistryExisting = false,
    int HazIdWatermark = 0,
    bool SecurityFramingPresent = false,
    bool DpiaReferenceExisting = false,
    bool NhsRetentionApplicable = false,
    bool LawfulBasisConfirmed = false)
{
    public static RoutingContext Create(
        StageType stageType,
        int currentPhase,
        bool isFirstMessage,
        bool swaggerPresent,
        bool prototypePresent,
        bool hazardRegistryExisting,
        int hazIdWatermark,
        bool securityFramingPresent,
        bool dpiaReferenceExisting,
        bool nhsRetentionApplicable,
        bool lawfulBasisConfirmed)
    {
        return new RoutingContext(
            StageType: stageType,
            CurrentPhase: currentPhase,
            IsFirstMessage: isFirstMessage,
            SwaggerPresent: swaggerPresent,
            PrototypePresent: prototypePresent,
            HazardRegistryExisting: hazardRegistryExisting,
            HazIdWatermark: hazIdWatermark,
            SecurityFramingPresent: securityFramingPresent,
            DpiaReferenceExisting: dpiaReferenceExisting,
            NhsRetentionApplicable: nhsRetentionApplicable,
            LawfulBasisConfirmed: lawfulBasisConfirmed);
    }
}
