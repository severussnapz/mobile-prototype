namespace Genesis.AI.Domain.HazardLog;

/// <summary>
/// A single control mitigating a cause, parsed from a control table row in the
/// hazard registry. Categories map to the hazard log control columns
/// (HIT Design, Training, Business Process, Customer Controls).
/// </summary>
public sealed record ControlRecord(
    string Category,
    string Description,
    string Evidence);
