namespace Genesis.AI.Domain.HazardLog;

/// <summary>
/// Builds a hazard log spreadsheet (.xlsx) from parsed hazard records, populating
/// the EMIS clinical safety hazard log template.
/// </summary>
public interface IHazardLogExcelBuilder
{
    /// <summary>
    /// Produces the hazard log workbook as a byte array.
    /// </summary>
    /// <param name="hazards">The hazards to render, one block of cause rows each.</param>
    /// <param name="productModule">Product module / capability (the project name).</param>
    /// <param name="dateAdded">The date the hazards were added, formatted for display.</param>
    byte[] Build(IReadOnlyList<HazardRecord> hazards, string productModule, string dateAdded);
}
