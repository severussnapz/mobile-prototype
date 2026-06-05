using System.Reflection;
using ClosedXML.Excel;
using Genesis.AI.Domain.HazardLog;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Builds the EMIS clinical safety hazard log spreadsheet from parsed hazard
/// records, populating the embedded IF678 hazard log template. One row is written
/// per cause; hazard-level columns are merged across the cause rows of a hazard.
/// </summary>
public sealed class HazardLogExcelBuilder : IHazardLogExcelBuilder
{
    private const string TemplateResourceName = "Genesis.AI.Infrastructure.Resources.HazardLogTemplate.xlsx";
    private const string WorksheetName = "Hazard Log";
    private const int FirstDataRow = 5;

    // Hazard-level columns written on the first cause row and merged across all
    // cause rows belonging to a hazard (1-based column indices, A–Y layout).
    private static readonly int[] HazardLevelColumns =
        [1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 21, 22, 23, 24, 25];

    public byte[] Build(IReadOnlyList<HazardRecord> hazards, string productModule, string dateAdded)
    {
        ArgumentNullException.ThrowIfNull(hazards);

        using var templateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {TemplateResourceName}");

        using var workbook = new XLWorkbook(templateStream);
        var worksheet = workbook.Worksheet(WorksheetName);

        var lastDataRow = WriteHazards(worksheet, hazards, productModule, dateAdded);

        if (lastDataRow >= FirstDataRow)
        {
            ApplyDataStyle(worksheet, FirstDataRow, lastDataRow);
        }

        worksheet.SheetView.FreezeRows(FirstDataRow - 1);

        return Serialise(workbook);
    }

    private static int WriteHazards(
        IXLWorksheet worksheet,
        IReadOnlyList<HazardRecord> hazards,
        string productModule,
        string dateAdded)
    {
        var currentRow = FirstDataRow;
        var hazardCount = 0;

        foreach (var hazard in hazards)
        {
            hazardCount++;
            var firstRow = currentRow;

            foreach (var cause in hazard.Causes)
            {
                if (currentRow == firstRow)
                {
                    WriteHazardLevelCells(worksheet, currentRow, hazardCount, hazard, productModule, dateAdded);
                }

                WriteCauseCells(worksheet, currentRow, cause);
                worksheet.Row(currentRow).Height = 80;
                currentRow++;
            }

            if (hazard.Causes.Count > 1)
            {
                MergeHazardLevelColumns(worksheet, firstRow, currentRow - 1);
            }
        }

        return currentRow - 1;
    }

    private static byte[] Serialise(XLWorkbook workbook)
    {
        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    private static void WriteHazardLevelCells(
        IXLWorksheet worksheet,
        int row,
        int hazardCount,
        HazardRecord hazard,
        string productModule,
        string dateAdded)
    {
        worksheet.Cell(row, 1).Value = hazardCount;
        worksheet.Cell(row, 2).Value = dateAdded;
        worksheet.Cell(row, 3).Value = hazard.SourceRequirement;
        worksheet.Cell(row, 4).Value = productModule;
        worksheet.Cell(row, 5).Value = hazard.HazardReference;
        worksheet.Cell(row, 6).Value = hazard.HazardArea;
        worksheet.Cell(row, 7).Value = hazard.HazardDescription;
        worksheet.Cell(row, 8).Value = hazard.ClinicalImpact;
        worksheet.Cell(row, 10).Value = hazard.ExistingControls;
        worksheet.Cell(row, 11).Value = hazard.InitialSeverity;
        worksheet.Cell(row, 12).Value = hazard.InitialLikelihood;
        worksheet.Cell(row, 13).Value = hazard.InitialRisk;
        worksheet.Cell(row, 21).Value = hazard.ResidualSeverity;
        worksheet.Cell(row, 22).Value = hazard.ResidualLikelihood;
        worksheet.Cell(row, 23).Value = hazard.ResidualRisk;
        worksheet.Cell(row, 24).Value = hazard.Status;
        worksheet.Cell(row, 25).Value = hazard.AdditionalComments;
    }

    private static void WriteCauseCells(IXLWorksheet worksheet, int row, CauseRecord cause)
    {
        worksheet.Cell(row, 9).Value = cause.Description;

        var (hitDescription, hitEvidence) = JoinControls(cause.Controls, "HIT Design");
        var (trainingDescription, trainingEvidence) = JoinControls(cause.Controls, "Training");
        var (businessDescription, businessEvidence) = JoinControls(cause.Controls, "Business Process");
        var (customerDescription, _) = JoinControls(cause.Controls, "Customer");

        worksheet.Cell(row, 14).Value = hitDescription;
        worksheet.Cell(row, 15).Value = hitEvidence;
        worksheet.Cell(row, 16).Value = trainingDescription;
        worksheet.Cell(row, 17).Value = trainingEvidence;
        worksheet.Cell(row, 18).Value = businessDescription;
        worksheet.Cell(row, 19).Value = businessEvidence;
        worksheet.Cell(row, 20).Value = customerDescription;
    }

    private static (string Description, string Evidence) JoinControls(
        IReadOnlyList<ControlRecord> controls,
        string categoryPrefix)
    {
        var matching = controls
            .Where(control => control.Category.Trim().StartsWith(categoryPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var description = string.Join(
            '\n',
            matching.Select(control => control.Description).Where(value => !string.IsNullOrWhiteSpace(value)));

        var evidence = string.Join(
            '\n',
            matching.Select(control => control.Evidence).Where(value => !string.IsNullOrWhiteSpace(value)));

        return (description, evidence);
    }

    private static void MergeHazardLevelColumns(IXLWorksheet worksheet, int firstRow, int lastRow)
    {
        foreach (var column in HazardLevelColumns)
        {
            var range = worksheet.Range(firstRow, column, lastRow, column);
            range.Merge();
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            range.Style.Alignment.WrapText = true;
        }
    }

    private static void ApplyDataStyle(IXLWorksheet worksheet, int firstRow, int lastRow)
    {
        var dataRange = worksheet.Range(firstRow, 1, lastRow, 25);
        dataRange.Style.Alignment.WrapText = true;
        dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
}
