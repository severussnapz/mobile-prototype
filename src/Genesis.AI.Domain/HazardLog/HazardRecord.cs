namespace Genesis.AI.Domain.HazardLog;

/// <summary>
/// A hazard parsed from a <c>## HAZ-DOC-NNN</c> block in the hazard registry
/// (<c>requirements/HAZARD-REGISTRY.md</c>). Aggregates the hazard-level metadata
/// plus its possible causes and controls.
/// </summary>
public sealed record HazardRecord(
    string HazardReference,
    string HazardArea,
    string HazardDescription,
    string ClinicalImpact,
    string SourceRequirement,
    string ExistingControls,
    string InitialSeverity,
    string InitialLikelihood,
    string InitialRisk,
    string ResidualSeverity,
    string ResidualLikelihood,
    string ResidualRisk,
    string Status,
    string AdditionalComments,
    IReadOnlyList<CauseRecord> Causes);
