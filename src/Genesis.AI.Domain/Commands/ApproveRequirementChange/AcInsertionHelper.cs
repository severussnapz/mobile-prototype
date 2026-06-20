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

        var acSectionIndex = Array.FindIndex(lines,
            line => line.TrimStart().StartsWith(AcSectionHeading, StringComparison.Ordinal));

        if (acSectionIndex < 0)
        {
            throw new InvalidOperationException(
                $"Cannot insert AC text — '## Acceptance Criteria' section not found in REQ file.");
        }

        // Find the last "- [ ]" line after the AC section heading
        // but before the next "##" heading
        var lastAcLineIndex = -1;
        for (var index = acSectionIndex + 1; index < lines.Length; index++)
        {
            var trimmed = lines[index].TrimStart();
            if (trimmed.StartsWith("## ", StringComparison.Ordinal) && index > acSectionIndex)
            {
                break;
            }

            if (trimmed.StartsWith(AcItemPrefix, StringComparison.Ordinal))
            {
                lastAcLineIndex = index;
            }
        }

        if (lastAcLineIndex < 0)
        {
            throw new InvalidOperationException(
                $"Cannot insert AC text — no existing '- [ ]' items found in '## Acceptance Criteria' section.");
        }

        var taggedAcText = $"- [ ] {acText.TrimStart('-', '[', ']', ' ')} " +
                           $"*(Added by {changeId} — {raisingPipeline})*";

        var resultLines = new List<string>(lines);
        resultLines.Insert(lastAcLineIndex + 1, taggedAcText);

        return string.Join('\n', resultLines);
    }
}
