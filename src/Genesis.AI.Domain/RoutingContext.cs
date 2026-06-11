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
    bool LawfulBasisConfirmed = false);
