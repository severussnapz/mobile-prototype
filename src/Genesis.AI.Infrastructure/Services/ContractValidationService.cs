using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class ContractValidationService : IContractValidationService
{
    public ContractValidationResult ValidatePipeline01(string reqContent)
    {
        var violations = new List<string>();

        CheckSection(reqContent, "## User Story", violations,
            "Missing required section: '## User Story'");

        // Accept both canonical '## Acceptance Criteria' and legacy bold format
        var hasAcHeading = reqContent.Contains("## Acceptance Criteria",
            StringComparison.OrdinalIgnoreCase);
        var hasLegacyAcHeading = reqContent.Contains("**Acceptance Criteria",
            StringComparison.OrdinalIgnoreCase);
        if (!hasAcHeading && !hasLegacyAcHeading)
        {
            violations.Add("Missing required section: '## Acceptance Criteria'");
        }

        CheckAcItems(reqContent, violations);

        return new ContractValidationResult(violations.Count == 0, violations);
    }

    public ContractValidationResult ValidatePipeline03(string reqContent)
    {
        var violations = new List<string>();

        CheckSection(reqContent, "## Architecture (Added by Pipeline 03)", violations,
            "Missing required section: 'Architecture (Added by Pipeline 03)'");
        CheckSection(reqContent, "### BDAT Analysis", violations,
            "Missing required sub-section: '### BDAT Analysis'");
        CheckSection(reqContent, "### Failure Modes", violations,
            "Missing required sub-section: '### Failure Modes'");
        CheckSection(reqContent, "### Integration Points", violations,
            "Missing required sub-section: '### Integration Points'");

        return new ContractValidationResult(violations.Count == 0, violations);
    }

    public ContractValidationResult ValidatePipeline04(string reqContent)
    {
        var violations = new List<string>();

        CheckSection(reqContent, "## Solution Design (Added by Pipeline 04)", violations,
            "Missing required section: 'Solution Design (Added by Pipeline 04)'");
        CheckSection(reqContent, "### API Contract", violations,
            "Missing required sub-section: '### API Contract'");
        CheckSection(reqContent, "### Database Schema", violations,
            "Missing required sub-section: '### Database Schema'");

        return new ContractValidationResult(violations.Count == 0, violations);
    }

    public ContractValidationResult ValidatePipeline05(string reqContent)
    {
        var violations = new List<string>();

        CheckSection(reqContent, "## PxD (Added by Pipeline 05)", violations,
            "Missing required section: 'PxD (Added by Pipeline 05)'");
        CheckSection(reqContent, "### User Flow", violations,
            "Missing required sub-section: '### User Flow'");
        CheckSection(reqContent, "### Component Specifications", violations,
            "Missing required sub-section: '### Component Specifications'");
        CheckSection(reqContent, "### Accessibility Requirements", violations,
            "Missing required sub-section: '### Accessibility Requirements'");

        return new ContractValidationResult(violations.Count == 0, violations);
    }

    public ContractValidationResult ValidatePipeline06(string reqContent)
    {
        var violations = new List<string>();

        var hasClinicalSafetySection =
            reqContent.Contains("## Clinical Safety", StringComparison.OrdinalIgnoreCase);
        var hasGuardrails =
            reqContent.Contains("### Applicable Guardrails", StringComparison.OrdinalIgnoreCase);

        if (!hasClinicalSafetySection || !hasGuardrails)
        {
            violations.Add("Missing required section: '## Clinical Safety' with guardrails");
        }

        CheckSection(reqContent, "### Mitigations", violations,
            "Missing required sub-section: '### Mitigations' — Pipeline 06 must complete mitigations");

        return new ContractValidationResult(violations.Count == 0, violations);
    }

    public ContractValidationResult ValidatePipeline07(string reqContent)
    {
        var violations = new List<string>();

        CheckSection(reqContent, "### Lawful Basis", violations,
            "Missing required sub-section: '### Lawful Basis' — Pipeline 07 must declare lawful basis");
        CheckSection(reqContent, "### Data Handling", violations,
            "Missing required sub-section: '### Data Handling' — Pipeline 07 must document data handling");

        return new ContractValidationResult(violations.Count == 0, violations);
    }

    public ContractValidationResult ValidatePipeline08(string reqContent)
    {
        var violations = new List<string>();

        CheckSection(reqContent, "## Security", violations,
            "Missing required section: '## Security'");
        CheckSection(reqContent, "### Security Requirements", violations,
            "Missing required sub-section: '### Security Requirements' — Pipeline 08 must complete security requirements");

        return new ContractValidationResult(violations.Count == 0, violations);
    }

    private static void CheckSection(
        string content,
        string heading,
        List<string> violations,
        string message)
    {
        if (!content.Contains(heading, StringComparison.OrdinalIgnoreCase))
        {
            violations.Add(message);
        }
    }

    private static void CheckAcItems(string content, List<string> violations)
    {
        // Accept - [ ] items anywhere under either
        // '## Acceptance Criteria' or '**Acceptance Criteria' headings
        var hasAcSection =
            content.Contains("## Acceptance Criteria", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("**Acceptance Criteria", StringComparison.OrdinalIgnoreCase);

        var hasAcItem = content.Contains("- [ ]", StringComparison.Ordinal);

        if (!hasAcSection || !hasAcItem)
        {
            violations.Add(
                "Missing required content: 'Acceptance Criteria' section must contain at least one '- [ ]' item");
        }
    }

    private static void CheckChecks(string content, List<string> violations)
    {
        if (!content.Contains("### CHECK", StringComparison.OrdinalIgnoreCase) &&
            !content.Contains("**Pass Criteria:**", StringComparison.OrdinalIgnoreCase))
        {
            violations.Add(
                "Missing required content: 'Evaluation Function Specification' must contain at least one CHECK with Pass Criteria");
        }
    }
}
