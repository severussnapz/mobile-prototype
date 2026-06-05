namespace Genesis.AI.Domain.HazardLog;

/// <summary>
/// A possible cause of a hazard, with the controls that mitigate it. Each cause
/// becomes one row in the hazard log spreadsheet (hazard-level columns are merged
/// across the cause rows belonging to the same hazard).
/// </summary>
public sealed record CauseRecord(
    string Description,
    IReadOnlyList<ControlRecord> Controls);
