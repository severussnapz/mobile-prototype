using System.Text.RegularExpressions;
using Genesis.AI.Domain.HazardLog;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Parses <c>requirements/HAZARD-REGISTRY.md</c> into structured hazard records.
/// Ported from the reference Python generator and made tolerant of the
/// Pipeline 06 registry format (cause headings at level four or five, eight-column
/// control tables located dynamically by header name).
/// </summary>
public sealed class HazardRegistryParser : IHazardRegistryParser
{
    private static readonly Regex HazardBlockSplit = new(@"\n(?=## HAZ-DOC-)", RegexOptions.Compiled);
    private static readonly Regex HazardHeading = new(@"##\s+(HAZ-DOC-\d+):\s*(.+)", RegexOptions.Compiled);
    private static readonly Regex RequirementReference = new(@"(REQ-\d+)", RegexOptions.Compiled);
    private static readonly Regex CauseHeading = new(@"(?m)^#{4,5}\s+Cause\s+\d+:\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex CauseBodyCutoff = new(
        @"\n(?:#{1,5} |\*\*Additional Comments|\*\*CSO Approval|### Genesis AI Skills|### Mitigations|### Residual Risk)",
        RegexOptions.Compiled);
    public IReadOnlyList<HazardRecord> Parse(string registryContent)
    {
        if (string.IsNullOrWhiteSpace(registryContent))
        {
            return [];
        }

        var hazards = new List<HazardRecord>();

        // Split into per-hazard blocks at each "## HAZ-DOC-" heading.
        var blocks = HazardBlockSplit.Split(registryContent);

        foreach (var block in blocks)
        {
            if (!block.TrimStart().StartsWith("## HAZ-DOC-", StringComparison.Ordinal))
            {
                continue;
            }

            var hazard = ParseHazardBlock(block);
            if (hazard is not null)
            {
                hazards.Add(hazard);
            }
        }

        return hazards;
    }

    private static HazardRecord? ParseHazardBlock(string block)
    {
        var headingMatch = HazardHeading.Match(block);
        if (!headingMatch.Success)
        {
            return null;
        }

        var hazardReference = headingMatch.Groups[1].Value;
        var (hazardArea, headingDescription) = SplitAreaAndDescription(headingMatch.Groups[2].Value);

        var sourceRequirement = ExtractRequirementReference(block);
        var hazardDescription = ExtractField(block, "Hazard description");
        if (string.IsNullOrWhiteSpace(hazardDescription))
        {
            hazardDescription = headingDescription;
        }

        var clinicalImpact = ExtractField(block, "Potential clinical impact");
        var existingControls = ExtractField(block, "Existing Controls");
        var status = ExtractField(block, "Status");
        var residualDecision = ExtractField(block, "Residual risk decision");

        var (initialSeverity, initialLikelihood, initialRisk) = ExtractRisk(block, "Initial risk");
        var (residualSeverity, residualLikelihood, residualRisk) = ExtractRisk(block, "Residual risk");

        var causes = ParseCauses(block);

        return new HazardRecord(
            hazardReference,
            hazardArea,
            hazardDescription,
            clinicalImpact,
            sourceRequirement,
            existingControls,
            initialSeverity,
            initialLikelihood,
            initialRisk,
            residualSeverity,
            residualLikelihood,
            residualRisk,
            status,
            residualDecision,
            causes);
    }

    private static (string Area, string Description) SplitAreaAndDescription(string headingRemainder)
    {
        var trimmed = headingRemainder.Trim();
        var separatorIndex = trimmed.IndexOf('—');
        if (separatorIndex < 0)
        {
            // Fall back to a plain hyphen separator if no em dash is present.
            separatorIndex = trimmed.IndexOf(" - ", StringComparison.Ordinal);
            if (separatorIndex >= 0)
            {
                return (trimmed[..separatorIndex].Trim(), trimmed[(separatorIndex + 3)..].Trim());
            }

            return (trimmed, string.Empty);
        }

        return (trimmed[..separatorIndex].Trim(), trimmed[(separatorIndex + 1)..].Trim());
    }

    private static string ExtractRequirementReference(string block)
    {
        var fieldValue = ExtractField(block, "Source requirement");
        var match = RequirementReference.Match(fieldValue);
        return match.Success ? match.Value : fieldValue;
    }

    private static string ExtractField(string block, string label)
    {
        var pattern = $@"\*\*{Regex.Escape(label)}:\*\*\s*(.+)";
        var match = Regex.Match(block, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static (string Severity, string Likelihood, string Level) ExtractRisk(string block, string label)
    {
        var fullPattern = $@"\*\*{Regex.Escape(label)}:\*\*\s*(.+?)\s*[×x]\s*(.+?)\s*=\s*\*\*(.+?)\*\*";
        var fullMatch = Regex.Match(block, fullPattern);
        if (fullMatch.Success)
        {
            return (
                fullMatch.Groups[1].Value.Trim(),
                fullMatch.Groups[2].Value.Trim(),
                fullMatch.Groups[3].Value.Trim());
        }

        // Fall back to a bare level when severity/likelihood are not expressed.
        var levelPattern = $@"\*\*{Regex.Escape(label)}:\*\*.+?\*\*(.+?)\*\*";
        var levelMatch = Regex.Match(block, levelPattern);
        return (string.Empty, string.Empty, levelMatch.Success ? levelMatch.Groups[1].Value.Trim() : string.Empty);
    }

    private static List<CauseRecord> ParseCauses(string block)
    {
        var causes = new List<CauseRecord>();
        var causeMatches = CauseHeading.Matches(block);

        for (var index = 0; index < causeMatches.Count; index++)
        {
            var causeMatch = causeMatches[index];
            var bodyStart = causeMatch.Index + causeMatch.Length;
            var bodyEnd = index + 1 < causeMatches.Count ? causeMatches[index + 1].Index : block.Length;
            var causeBody = block[bodyStart..bodyEnd];

            // Stop the cause body at the next major section so we do not consume
            // the Mitigations or sign-off tables that follow the cause list.
            var cutoff = CauseBodyCutoff.Match(causeBody);
            if (cutoff.Success)
            {
                causeBody = causeBody[..cutoff.Index];
            }

            var causeText = causeMatch.Groups[1].Value.Trim();
            var controls = ParseControlTable(causeBody);
            causes.Add(new CauseRecord(causeText, controls));
        }

        if (causes.Count == 0)
        {
            causes.Add(new CauseRecord("[See hazard description]", []));
        }

        return causes;
    }

    private static List<ControlRecord> ParseControlTable(string causeBody)
    {
        var lines = causeBody.Split('\n');
        var tableRows = lines
            .Select(line => line.Trim())
            .Where(line => line.StartsWith('|'))
            .ToList();

        if (tableRows.Count == 0)
        {
            return [];
        }

        var (categoryIndex, descriptionIndex, evidenceIndex) = LocateControlColumns(tableRows[0]);

        var controls = new List<ControlRecord>();
        foreach (var row in tableRows)
        {
            if (row.StartsWith("|---", StringComparison.Ordinal)
                || row.StartsWith("| ---", StringComparison.Ordinal)
                || row.Contains("Control ID", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var cells = SplitTableRow(row);
            if (cells.Count <= descriptionIndex)
            {
                continue;
            }

            var category = ValueAt(cells, categoryIndex);
            var description = ValueAt(cells, descriptionIndex);
            var evidence = ValueAt(cells, evidenceIndex);

            if (string.IsNullOrWhiteSpace(category) && string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            controls.Add(new ControlRecord(category, description, evidence));
        }

        return controls;
    }

    private static (int Category, int Description, int Evidence) LocateControlColumns(string headerRow)
    {
        var headers = SplitTableRow(headerRow);

        var categoryIndex = FindHeaderIndex(headers, "Category", fallback: 1);
        var descriptionIndex = FindHeaderIndex(headers, "Description", fallback: 2);
        var evidenceIndex = FindHeaderIndex(headers, "Evidence", fallback: 4);

        return (categoryIndex, descriptionIndex, evidenceIndex);
    }

    private static int FindHeaderIndex(List<string> headers, string name, int fallback)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            if (headers[index].Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return fallback;
    }

    private static List<string> SplitTableRow(string row)
    {
        return row.Trim('|')
            .Split('|')
            .Select(cell => cell.Trim())
            .ToList();
    }

    private static string ValueAt(List<string> cells, int index)
    {
        if (index < 0 || index >= cells.Count)
        {
            return string.Empty;
        }

        var value = cells[index];
        return value is "—" or "-" ? string.Empty : value;
    }
}
