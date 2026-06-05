using ClosedXML.Excel;
using Genesis.AI.Domain.HazardLog;
using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public class HazardLogExcelBuilderTests
{
    private const string WorksheetName = "Hazard Log";
    private const int FirstDataRow = 5;

    private readonly HazardLogExcelBuilder _builder = new();

    private static HazardRecord CreateHazard(IReadOnlyList<CauseRecord> causes)
    {
        return new HazardRecord(
            "HAZ-DOC-001",
            "Patient Identification",
            "Wrong patient record displayed",
            "Clinical decisions made against the wrong record",
            "REQ-001",
            "Patient banner displays NHS number",
            "Major",
            "Possible",
            "High",
            "Major",
            "Unlikely",
            "Low",
            "Active",
            "Acceptable with controls",
            causes);
    }

    private static IXLWorksheet LoadWorksheet(byte[] content)
    {
        using var stream = new MemoryStream(content);
        var workbook = new XLWorkbook(stream);
        return workbook.Worksheet(WorksheetName);
    }

    [Fact]
    public void Build_SingleHazardSingleCause_WritesHazardLevelCells()
    {
        var control = new ControlRecord("HIT Design", "Force NHS number confirmation", "EV-101");
        var cause = new CauseRecord("Ambiguous search", [control]);
        var hazards = new List<HazardRecord> { CreateHazard([cause]) };

        var content = _builder.Build(hazards, "ACME Portal", "01/02/2025");

        var worksheet = LoadWorksheet(content);
        Assert.Equal(1, worksheet.Cell(FirstDataRow, 1).GetValue<int>());
        Assert.Equal("01/02/2025", worksheet.Cell(FirstDataRow, 2).GetString());
        Assert.Equal("REQ-001", worksheet.Cell(FirstDataRow, 3).GetString());
        Assert.Equal("ACME Portal", worksheet.Cell(FirstDataRow, 4).GetString());
        Assert.Equal("HAZ-DOC-001", worksheet.Cell(FirstDataRow, 5).GetString());
        Assert.Equal("Patient Identification", worksheet.Cell(FirstDataRow, 6).GetString());
    }

    [Fact]
    public void Build_SingleHazardSingleCause_WritesRiskColumns()
    {
        var cause = new CauseRecord("Ambiguous search", []);
        var hazards = new List<HazardRecord> { CreateHazard([cause]) };

        var content = _builder.Build(hazards, "ACME Portal", "01/02/2025");

        var worksheet = LoadWorksheet(content);
        Assert.Equal("Major", worksheet.Cell(FirstDataRow, 11).GetString());
        Assert.Equal("Possible", worksheet.Cell(FirstDataRow, 12).GetString());
        Assert.Equal("High", worksheet.Cell(FirstDataRow, 13).GetString());
        Assert.Equal("Major", worksheet.Cell(FirstDataRow, 21).GetString());
        Assert.Equal("Unlikely", worksheet.Cell(FirstDataRow, 22).GetString());
        Assert.Equal("Low", worksheet.Cell(FirstDataRow, 23).GetString());
    }

    [Fact]
    public void Build_ControlsByCategory_WritesIntoCorrectColumns()
    {
        var controls = new List<ControlRecord>
        {
            new("HIT Design", "Fail safe to blocking state", "EV-101"),
            new("Training", "Train staff on verification", "EV-102"),
        };
        var cause = new CauseRecord("Engine timeout", controls);
        var hazards = new List<HazardRecord> { CreateHazard([cause]) };

        var content = _builder.Build(hazards, "ACME Portal", "01/02/2025");

        var worksheet = LoadWorksheet(content);
        Assert.Equal("Engine timeout", worksheet.Cell(FirstDataRow, 9).GetString());
        Assert.Equal("Fail safe to blocking state", worksheet.Cell(FirstDataRow, 14).GetString());
        Assert.Equal("EV-101", worksheet.Cell(FirstDataRow, 15).GetString());
        Assert.Equal("Train staff on verification", worksheet.Cell(FirstDataRow, 16).GetString());
        Assert.Equal("EV-102", worksheet.Cell(FirstDataRow, 17).GetString());
    }

    [Fact]
    public void Build_HazardWithTwoCauses_WritesOneRowPerCause()
    {
        var causeOne = new CauseRecord("First cause", []);
        var causeTwo = new CauseRecord("Second cause", []);
        var hazards = new List<HazardRecord> { CreateHazard([causeOne, causeTwo]) };

        var content = _builder.Build(hazards, "ACME Portal", "01/02/2025");

        var worksheet = LoadWorksheet(content);
        Assert.Equal("First cause", worksheet.Cell(FirstDataRow, 9).GetString());
        Assert.Equal("Second cause", worksheet.Cell(FirstDataRow + 1, 9).GetString());
    }

    [Fact]
    public void Build_TwoHazards_IncrementsCountColumn()
    {
        var firstHazard = CreateHazard([new CauseRecord("Cause A", [])]);
        var secondHazard = new HazardRecord(
            "HAZ-DOC-002", "Medication", "Allergy alert suppressed", "Patient harm",
            "REQ-014", "Hard-stop alert", "Catastrophic", "Possible", "High",
            "Catastrophic", "Rare", "Moderate", "Active", "Acceptable",
            [new CauseRecord("Cause B", [])]);
        var hazards = new List<HazardRecord> { firstHazard, secondHazard };

        var content = _builder.Build(hazards, "ACME Portal", "01/02/2025");

        var worksheet = LoadWorksheet(content);
        Assert.Equal(1, worksheet.Cell(FirstDataRow, 1).GetValue<int>());
        Assert.Equal(2, worksheet.Cell(FirstDataRow + 1, 1).GetValue<int>());
        Assert.Equal("HAZ-DOC-002", worksheet.Cell(FirstDataRow + 1, 5).GetString());
    }

    [Fact]
    public void Build_EmptyHazardList_ProducesWorkbookWithNoDataRows()
    {
        var content = _builder.Build([], "ACME Portal", "01/02/2025");

        var worksheet = LoadWorksheet(content);
        Assert.True(worksheet.Cell(FirstDataRow, 1).IsEmpty());
    }
}
