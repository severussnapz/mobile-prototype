namespace Genesis.AI.Domain.Commands.GenerateHazardLog;

/// <summary>
/// Outcome of a hazard log generation request.
/// </summary>
public enum GenerateHazardLogStatus
{
    /// <summary>The hazard log was generated and persisted successfully.</summary>
    Success,

    /// <summary>No project exists with the requested identifier.</summary>
    ProjectNotFound,

    /// <summary>The project has no hazard registry, or it contains no hazards.</summary>
    RegistryNotFound
}
