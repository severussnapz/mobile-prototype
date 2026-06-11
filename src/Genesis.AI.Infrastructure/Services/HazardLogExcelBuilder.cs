using System.Reflection;
using System.Text.RegularExpressions;
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
        [1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 13, 20, 21, 22, 23, 24, 25];

    private static readonly Regex SentenceBoundaryRegex = new(@"(?<=[.])\s+", RegexOptions.Compiled);
    private static readonly Regex CheckReferenceRegex = new(@"CHECK\s*\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex WordRootRegex = new(@"[a-zA-Z]{4,}", RegexOptions.Compiled);

    public byte[] Build(IReadOnlyList<HazardRecord> hazards, string productModule, string dateAdded)
    {
        ArgumentNullException.ThrowIfNull(hazards);

        using var templateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {TemplateResourceName}");

        using var workbook = new XLWorkbook(templateStream);
        var worksheet = workbook.Worksheet(WorksheetName);
        var dataRowStyles = CaptureDataRowStyles(worksheet);

        WriteHazards(worksheet, hazards, productModule, dateAdded, dataRowStyles);

        worksheet.SheetView.FreezeRows(FirstDataRow - 1);

        return Serialise(workbook);
    }

    private static int WriteHazards(
        IXLWorksheet worksheet,
        IReadOnlyList<HazardRecord> hazards,
        string productModule,
        string dateAdded,
        Dictionary<int, IXLStyle> dataRowStyles)
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

                var causeControls = cause.Controls;
                var (hitDescription, hitEvidence) = JoinControls(causeControls, "HIT Design");
                var (trainingDescription, trainingEvidence) = JoinControls(causeControls, "Training");
                var (businessDescription, businessEvidence) = JoinControls(causeControls, "Business Process");
                var customerDescription = BuildCustomerControls(causeControls);

                WriteCauseCells(
                    worksheet,
                    currentRow,
                    firstRow,
                    cause,
                    hazard.ExistingControls,
                    hitEvidence,
                    hitDescription,
                    trainingDescription,
                    trainingEvidence,
                    businessDescription,
                    businessEvidence,
                    customerDescription);

                ApplyDataRowStyle(worksheet, currentRow, dataRowStyles);
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
        worksheet.Cell(row, 6).Value = string.Empty;
        worksheet.Cell(row, 7).Value = hazard.HazardDescription;
        worksheet.Cell(row, 8).Value = hazard.ClinicalImpact;
        worksheet.Cell(row, 11).Value = "[TBD]";
        worksheet.Cell(row, 12).Value = "[TBD]";
        worksheet.Cell(row, 13).Value = "[TBD]";
        worksheet.Cell(row, 21).Value = "[TBD]";
        worksheet.Cell(row, 22).Value = "[TBD]";
        worksheet.Cell(row, 23).Value = "[TBD]";
        worksheet.Cell(row, 24).Value = string.IsNullOrWhiteSpace(hazard.Status) ? null : hazard.Status;
        worksheet.Cell(row, 25).Value = string.IsNullOrWhiteSpace(hazard.AdditionalComments) ? null : hazard.AdditionalComments;
    }

    private static void WriteCauseCells(
        IXLWorksheet worksheet,
        int row,
        int firstHazardRow,
        CauseRecord cause,
        string existingControls,
        string hitEvidence,
        string hitDescription,
        string trainingDescription,
        string trainingEvidence,
        string businessDescription,
        string businessEvidence,
        string customerDescription)
    {
        worksheet.Cell(row, 9).Value = cause.Description;
        worksheet.Cell(row, 10).Value = PickExistingControlsForCause(
            existingControls,
            cause.Description,
            hitEvidence,
            row - firstHazardRow);

        worksheet.Cell(row, 14).Value = hitDescription;
        worksheet.Cell(row, 15).Value = hitEvidence;
        worksheet.Cell(row, 16).Value = trainingDescription;
        worksheet.Cell(row, 17).Value = trainingEvidence;
        worksheet.Cell(row, 18).Value = businessDescription;
        worksheet.Cell(row, 19).Value = businessEvidence;

        // Python parity: column T is written only from the first cause row controls.
        if (row == firstHazardRow)
        {
            worksheet.Cell(row, 20).Value = customerDescription;
        }
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

    private static string BuildCustomerControls(IReadOnlyList<ControlRecord> controls)
    {
        var matching = controls.Where(control =>
            (control.Category ?? string.Empty).Contains("customer", StringComparison.OrdinalIgnoreCase)).ToList();

        if (matching.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            '\n',
            matching.Select(control => control.Description).Where(value => !string.IsNullOrWhiteSpace(value) && value != "-"));
    }

    private static string PickExistingControlsForCause(
        string existingControls,
        string causeText,
        string hitEvidence,
        int causeIndex)
    {
        if (string.IsNullOrWhiteSpace(existingControls))
        {
            return string.Empty;
        }

        var text = existingControls.Trim();
        var fragments = SentenceBoundaryRegex.Split(text)
            .Select(fragment => fragment.Trim())
            .Where(fragment => !string.IsNullOrWhiteSpace(fragment))
            .ToList();

        if (fragments.Count == 0)
        {
            return causeIndex == 0 ? text : string.Empty;
        }

        var checkMatch = TryMatchByChecks(fragments, hitEvidence);
        if (checkMatch is not null)
        {
            return checkMatch;
        }

        var bestFragment = PickBestFragmentByRoots(fragments, causeText);
        if (!string.IsNullOrEmpty(bestFragment))
        {
            return bestFragment;
        }

        return causeIndex == 0 ? text : string.Empty;
    }

    private static string? TryMatchByChecks(List<string> fragments, string hitEvidence)
    {
        var checks = CheckReferenceRegex.Matches(hitEvidence ?? string.Empty)
            .Select(match => match.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (checks.Count == 0)
        {
            return null;
        }

        var checkMatches = fragments
            .Where(fragment => checks.Any(check => fragment.Contains(check, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return checkMatches.Count > 0 ? string.Join(' ', checkMatches) : null;
    }

    private static string PickBestFragmentByRoots(List<string> fragments, string causeText)
    {
        var causeRoots = WordRootRegex.Matches((causeText ?? string.Empty).ToLowerInvariant())
            .Select(match => match.Value[..4])
            .ToHashSet(StringComparer.Ordinal);

        var bestFragment = string.Empty;
        var bestScore = 0;

        foreach (var fragment in fragments)
        {
            var fragmentRoots = WordRootRegex.Matches(fragment.ToLowerInvariant())
                .Select(match => match.Value[..4])
                .ToHashSet(StringComparer.Ordinal);

            var score = causeRoots.Intersect(fragmentRoots).Count();
            if (score > bestScore)
            {
                bestScore = score;
                bestFragment = fragment;
            }
        }

        return bestFragment;
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

    private static Dictionary<int, IXLStyle> CaptureDataRowStyles(IXLWorksheet worksheet)
    {
        return Enumerable.Range(1, 25)
            .ToDictionary(column => column, column => worksheet.Cell(FirstDataRow, column).Style);
    }

    private static void ApplyDataRowStyle(
        IXLWorksheet worksheet,
        int row,
        Dictionary<int, IXLStyle> dataRowStyles)
    {
        for (var column = 1; column <= 25; column++)
        {
            worksheet.Cell(row, column).Style = dataRowStyles[column];
            worksheet.Cell(row, column).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            worksheet.Cell(row, column).Style.Alignment.WrapText = true;
        }
    }
}
