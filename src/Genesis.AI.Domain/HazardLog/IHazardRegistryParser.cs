namespace Genesis.AI.Domain.HazardLog;

/// <summary>
/// Parses the hazard registry markdown (<c>requirements/HAZARD-REGISTRY.md</c>)
/// into structured hazard records for hazard log generation.
/// </summary>
public interface IHazardRegistryParser
{
    /// <summary>
    /// Parses the registry markdown content into an ordered list of hazards.
    /// Returns an empty list when no hazard blocks are present.
    /// </summary>
    IReadOnlyList<HazardRecord> Parse(string registryContent);
}
