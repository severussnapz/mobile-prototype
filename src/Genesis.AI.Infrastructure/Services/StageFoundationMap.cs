using System.Collections.Frozen;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Maps each P3-P8 stage to its Category A (stable foundation) artefact path prefixes.
/// Category A artefacts are read-only during a requirement turn and are safe to cache
/// in the Bedrock prompt prefix.
///
/// Path prefix matching rules:
/// - An entry ending with "/" matches any artefact whose path starts with that prefix
///   (e.g. "requirements/" matches requirements/REQ-001.md, requirements/REQ-002.md, etc.).
/// - An entry not ending with "/" is an exact file path match.
///
/// Rules for inclusion in a stage's foundation set:
/// - The artefact must be produced by an EARLIER stage (upstream dependency).
/// - The artefact must not mutate during the current stage run.
/// - Live tracking artefacts (manifest watermark fields, feedback/REVIEW_LIST files,
///   feedback/VALUE_CHAIN.md) must NOT be included here — they are Category C and
///   must be fetched fresh every turn via get_artefact.
///
/// Only in scope for this plan: P3-P8 (Architecture through Security).
/// P1, P2, P9, P10 are excluded — they are cheap or separately optimised.
/// </summary>
public static class StageFoundationMap
{
    /// <summary>
    /// Returns the Category A artefact path prefixes for the given stage, or an empty array
    /// when the stage is out of scope (P1, P2, P9, P10) or has no foundation artefacts yet.
    /// </summary>
    public static IReadOnlyList<string> GetFoundationPrefixes(StageType stageType) =>
        FoundationPrefixes.TryGetValue(stageType, out var prefixes) ? prefixes : [];

    /// <summary>
    /// Returns true when the given artefact file path matches any of the foundation prefixes
    /// for the given stage.
    /// </summary>
    public static bool IsFoundationArtefact(StageType stageType, string filePath)
    {
        var prefixes = GetFoundationPrefixes(stageType);
        return prefixes.Any(prefix => prefix.EndsWith('/')
            ? filePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            : filePath.Equals(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly FrozenDictionary<StageType, string[]> FoundationPrefixes =
        new Dictionary<StageType, string[]>
        {
            // P03 Architecture — upstream: all requirements artefacts
            // manifest.md is excluded (mutable watermark fields — Category C)
            [StageType.Architecture] =
            [
                "requirements/",
            ],

            // P04 Design — upstream: requirements + architecture output
            [StageType.Design] =
            [
                "requirements/",
                "architecture/",
            ],

            // P05 PxD — upstream: requirements + architecture + design
            [StageType.Pxd] =
            [
                "requirements/",
                "architecture/",
                "design/",
            ],

            // P06 Clinical Safety — upstream: requirements + architecture + design + pxd
            [StageType.ClinicalSafety] =
            [
                "requirements/",
                "architecture/",
                "design/",
                "pxd/",
            ],

            // P07 Information Governance — same upstream as Clinical Safety + clinical output
            [StageType.InformationGovernance] =
            [
                "requirements/",
                "architecture/",
                "design/",
                "pxd/",
                "clinical_safety/",
            ],

            // P08 Security — upstream: all previous stage outputs
            [StageType.Security] =
            [
                "requirements/",
                "architecture/",
                "design/",
                "pxd/",
                "clinical_safety/",
                "information_governance/",
            ],
        }.ToFrozenDictionary();
}
