using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Populates the PR1625 DPIA Word template tables and appends the generated data
/// mapping section from validated DPIA data.
/// </summary>
internal static class Pr1625DpiaTableWriter
{
    public static void PopulateTemplateTables(List<Table> tables, DpiaData data)
    {
        var now = DateTimeOffset.UtcNow;

        PopulateHeaderTable(tables[0], data, now);
        SafeSet(tables[1], 0, 0, data.Project.Summary);
        SafeSet(tables[2], 0, 0, data.Project.DataFlow);
        PopulateProcessingTable(tables[3], data);
        PopulateLegalBasisTable(tables[5], data);
        PopulateRiskTable(tables[11], data);
    }

    public static void AppendGeneratedMappingSection(Body body, DpiaData data)
    {
        body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

        body.AppendChild(CreateHeading("Generated Data Mapping", "Heading1"));
        body.AppendChild(CreateParagraph($"Document version: {data.DocumentVersion}"));
        body.AppendChild(CreateParagraph($"Lawful purpose: {data.LegalBasis.LawfulPurpose}"));
        body.AppendChild(CreateParagraph($"Privacy notice reference: {data.LegalBasis.PrivacyNoticeReference}"));
        body.AppendChild(CreateParagraph($"IG reviewer: {data.Signoff.IgReviewer} ({data.Signoff.Role})"));
        body.AppendChild(CreateParagraph($"Decision: {data.Signoff.Decision} | Reference: {data.Signoff.Reference}"));

        if (data.SourceMapping.Count == 0)
            return;

        body.AppendChild(CreateHeading("Policy Source Mapping", "Heading2"));

        var mappingTable = new Table(
            new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

        mappingTable.AppendChild(CreateRow("Control ID", "Source document", "Source section"));

        foreach (var mapping in data.SourceMapping)
        {
            mappingTable.AppendChild(CreateRow(mapping.ControlId, mapping.SourceDocument, mapping.SourceSection));
        }

        body.AppendChild(mappingTable);
    }

    private static void PopulateHeaderTable(Table table0, DpiaData data, DateTimeOffset now)
    {
        var project = data.Project;
        SafeSet(table0, 1, 1, $"AUTO-{now:yyyyMMddTHHmmssZ}");
        SafeSet(table0, 2, 1, now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        SafeSet(table0, 4, 1, project.Title);
        SafeSet(table0, 5, 1, project.RequestDate);
        SafeSet(table0, 6, 1, project.ContactName);
        SafeSet(table0, 7, 1, project.Sponsor);
        SafeSet(table0, 8, 1, project.BusinessUnit);
        SafeSet(table0, 9, 1, project.Proposition);
        SafeSet(table0, 10, 1, JoinLines(data.Processing.ThirdParties));
        SafeSet(table0, 11, 1, project.Environment);
        SafeSet(table0, 12, 1, JoinLines(project.Stakeholders));
        SafeSet(table0, 13, 1, project.ChangeType);
    }

    private static void PopulateProcessingTable(Table table3, DpiaData data)
    {
        var processing = data.Processing;
        var dataProfile = data.DataProfile;
        SetQuestionResponse(table3, "1.01", BoolText(processing.PersonalData));
        SetQuestionResponse(table3, "1.02", processing.ThirdParties.Count > 0 ? "Yes" : "No");
        SetQuestionResponse(table3, "1.03", processing.DataController);
        SetQuestionResponse(table3, "1.04", JoinLines(processing.DataSubjects));
        SetQuestionResponse(table3, "1.05", JoinLines(processing.Recipients));
        SetQuestionResponse(table3, "1.06", BoolText(processing.MinorsData));
        SetQuestionResponse(table3, "1.07", JoinLines(dataProfile.DataCategories));
        SetQuestionResponse(table3, "1.08", BoolText(processing.SpecialCategoryData));
        SetQuestionResponse(table3, "1.11", processing.Volume);
        SetQuestionResponse(table3, "1.12", processing.Frequency);
        SetQuestionResponse(table3, "1.13", processing.Role);
        SetQuestionResponse(table3, "1.14", JoinLines(processing.ThirdParties));
    }

    private static void PopulateLegalBasisTable(Table table5, DpiaData data)
    {
        var legalBasis = data.LegalBasis;
        var dataProfile = data.DataProfile;
        SetQuestionResponse(table5, "3.01", legalBasis.Article6);
        SetQuestionResponse(table5, "3.03", string.IsNullOrWhiteSpace(legalBasis.Article9) ? "N/A" : legalBasis.Article9);
        SetQuestionResponse(table5, "3.05", dataProfile.RetentionRule);
        SetQuestionResponse(table5, "3.06", dataProfile.DeletionTrigger);
    }

    private static void PopulateRiskTable(Table table11, DpiaData data)
    {
        var signoff = data.Signoff;
        var rowIndex = 1;
        foreach (var risk in data.RiskAssessment.Risks)
        {
            if (rowIndex >= table11.Elements<TableRow>().Count())
                break;

            SafeSet(table11, rowIndex, 0, risk.RiskId);
            SafeSet(table11, rowIndex, 1, risk.Description);
            SafeSet(
                table11,
                rowIndex,
                2,
                $"Controls: {string.Join(", ", risk.Controls)}{Environment.NewLine}CHECKs: {string.Join(", ", risk.CheckIds)}");
            SafeSet(table11, rowIndex, 3, "Mandatory");
            SafeSet(table11, rowIndex, 4, signoff.Date);
            rowIndex++;
        }
    }

    private static TableRow CreateRow(params string[] values)
    {
        var row = new TableRow();
        foreach (var value in values)
        {
            var cell = new TableCell();
            cell.AppendChild(CreateParagraph(value));
            row.AppendChild(cell);
        }

        return row;
    }

    private static Paragraph CreateHeading(string text, string styleId)
    {
        return new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
            new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Paragraph CreateParagraph(string text)
    {
        var paragraph = new Paragraph();

        var lines = (text ?? string.Empty).Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                paragraph.AppendChild(new Run(new Break()));

            paragraph.AppendChild(new Run(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve }));
        }

        return paragraph;
    }

    private static string BoolText(bool value)
    {
        return value ? "Yes" : "No";
    }

    private static string JoinLines(IReadOnlyList<string> values)
    {
        return values is { Count: > 0 }
            ? string.Join(Environment.NewLine, values.Where(static value => !string.IsNullOrWhiteSpace(value)))
            : string.Empty;
    }

    private static void SetQuestionResponse(Table table, string questionCode, string value)
    {
        foreach (var row in table.Elements<TableRow>())
        {
            var cells = row.Elements<TableCell>().ToList();
            if (cells.Count == 0)
                continue;

            var code = GetCellText(cells[0]).Trim();
            if (!string.Equals(code, questionCode, StringComparison.Ordinal))
                continue;

            if (cells.Count >= 3)
                SetCellText(cells[2], value);
            if (cells.Count >= 4)
                SetCellText(cells[3], value);
            return;
        }
    }

    private static void SafeSet(Table table, int rowIndex, int colIndex, string value)
    {
        var rows = table.Elements<TableRow>().ToList();
        if (rowIndex < 0 || rowIndex >= rows.Count)
            return;

        var cells = rows[rowIndex].Elements<TableCell>().ToList();
        if (colIndex < 0 || colIndex >= cells.Count)
            return;

        SetCellText(cells[colIndex], value);
    }

    private static string GetCellText(TableCell cell)
    {
        return string.Concat(cell.Descendants<Text>().Select(text => text.Text));
    }

    private static void SetCellText(TableCell cell, string value)
    {
        cell.RemoveAllChildren<Paragraph>();
        cell.AppendChild(CreateParagraph(value));
    }
}
