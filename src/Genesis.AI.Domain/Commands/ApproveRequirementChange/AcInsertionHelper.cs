namespace Genesis.AI.Domain.Commands.ApproveRequirementChange;

public static class AcInsertionHelper
{
    private const string AcSectionHeading = "## Acceptance Criteria";
    private const string AcItemPrefix = "- [ ]";

    public static string InsertAcText(
        string reqContent,
        string acText,
        string changeId,
        string raisingPipeline)
    {
        var lines = reqContent.Split('\n');

        // Accept both canonical "## Acceptance Criteria" and legacy bold "**Acceptance Criteria" headings
        var acSectionIndex = Array.FindIndex(lines,
            line => line.TrimStart().StartsWith(AcSectionHeading, StringComparison.OrdinalIgnoreCase) ||
                    line.TrimStart().StartsWith("**Acceptance Criteria", StringComparison.OrdinalIgnoreCase));

        if (acSectionIndex < 0)
        {
            throw new InvalidOperationException(
                $"Cannot insert AC text — no Acceptance Criteria section found in REQ file.");
        }

        // Find the last "- [ ]" item anywhere in the file after the first AC section
        // REQ files may have multiple AC groups; insert after the last one
        var lastAcLineIndex = -1;
        for (var index = acSectionIndex; index < lines.Length; index++)
        {
            var trimmed = lines[index].TrimStart();
            if (trimmed.StartsWith(AcItemPrefix, StringComparison.Ordinal))
            {
                lastAcLineIndex = index;
            }
            // Stop at the Change Log or Traceability section
            if (index > acSectionIndex &&
                (trimmed.StartsWith("## Change Log", StringComparison.OrdinalIgnoreCase) ||
                 trimmed.StartsWith("## Traceability", StringComparison.OrdinalIgnoreCase) ||
                 trimmed.StartsWith("## Dimension", StringComparison.OrdinalIgnoreCase) ||
                 trimmed.StartsWith("## Architecture", StringComparison.OrdinalIgnoreCase) ||
                 trimmed.StartsWith("## PxD", StringComparison.OrdinalIgnoreCase) ||
                 trimmed.StartsWith("## Design", StringComparison.OrdinalIgnoreCase) ||
                 trimmed.StartsWith("## Clinical Safety", StringComparison.OrdinalIgnoreCase)))
            {
                break;
            }
        }

        if (lastAcLineIndex < 0)
        {
            throw new InvalidOperationException(
                $"Cannot insert AC text — no existing '- [ ]' items found in Acceptance Criteria section.");
        }

        var taggedAcText = $"- [ ] {acText.TrimStart('-', '[', ']', ' ')} " +
                           $"*(Added by {changeId} — {raisingPipeline})*";

        var resultLines = new List<string>(lines);
        resultLines.Insert(lastAcLineIndex + 1, taggedAcText);

        return string.Join('\n', resultLines);
    }
}
