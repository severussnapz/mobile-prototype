using Genesis.AI.Infrastructure.Services;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class ContractValidationServiceTests
{
    private static readonly string FullyPopulatedReq = """
        # REQ-001: Title

        **Priority:** Must Have

        ## User Story

        As a user I need something so that I can do things.

        ## Acceptance Criteria

        - [ ] First AC item. *(Must Have)*
        - [ ] Second AC item. *(Must Have)*

        ## Clinical Safety

        ### Applicable Guardrails
        - **CLIN-001:** Some guardrail

        ## Information Governance

        ### Applicable Guardrails
        - **IG-001:** Some guardrail

        ## Security

        ### Applicable Guardrails
        - **SEC-001:** Some guardrail

        ## Observability

        ### KPIs
        - Some KPI

        ## Evaluation Function Specification

        ### CHECK 1: CLIN-001 — Some Check
        **Pass Criteria:** Something passes.

        ## Traceability

        | Requirement | Hazard | Guardrail | Check |
        |-------------|--------|-----------|-------|
        | REQ-001 | — | CLIN-001 | CHECK 1 |

        ## Change Log

        | Version | Date | Pipeline | Summary |
        |---------|------|----------|---------|
        | 1.0 | 2026-06-20 | Pipeline 01 | Initial creation |
        """;

    [Fact]
    public void ValidatePipeline01_WhenAllSectionsPresent_ReturnsValid()
    {
        var service = new ContractValidationService();
        var result = service.ValidatePipeline01(FullyPopulatedReq);
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void ValidatePipeline01_WhenMissingUserStory_ReturnsViolation()
    {
        var content = FullyPopulatedReq.Replace("## User Story", "## Something Else");
        var service = new ContractValidationService();
        var result = service.ValidatePipeline01(content);
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("User Story"));
    }

    [Fact]
    public void ValidatePipeline01_WhenNoAcItems_ReturnsViolation()
    {
        var content = """
            # REQ-001: Title

            ## User Story

            As a user I need something.

            ## Acceptance Criteria

            ## Clinical Safety

            ### Applicable Guardrails
            - **CLIN-001:** Some guardrail

            ## Information Governance

            ### Applicable Guardrails
            - **IG-001:** Some guardrail

            ## Security

            ### Applicable Guardrails
            - **SEC-001:** Some guardrail

            ## Observability

            ### KPIs
            - Some KPI

            ## Evaluation Function Specification

            ### CHECK 1: CLIN-001 — Some Check
            **Pass Criteria:** Something passes.

            ## Traceability

            | Requirement | Check |
            |-------------|-------|
            | REQ-001 | CHECK 1 |

            ## Change Log

            | Version | Date | Pipeline | Summary |
            |---------|------|----------|---------|
            | 1.0 | 2026-06-20 | Pipeline 01 | Initial creation |
            """;
        var service = new ContractValidationService();
        var result = service.ValidatePipeline01(content);
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("Acceptance Criteria"));
    }

    [Fact]
    public void ValidatePipeline01_WhenMissingEvaluationSpec_ReturnsViolation()
    {
        var content = FullyPopulatedReq.Replace("## Evaluation Function Specification", "## Something");
        var service = new ContractValidationService();
        var result = service.ValidatePipeline01(content);
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("Evaluation Function Specification"));
    }

    [Fact]
    public void ValidatePipeline01_WhenMissingChangeLog_ReturnsViolation()
    {
        var content = FullyPopulatedReq.Replace("## Change Log", "## Something Else");
        var service = new ContractValidationService();
        var result = service.ValidatePipeline01(content);
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("Change Log"));
    }

    [Fact]
    public void ValidatePipeline03_WhenArchitectureSectionPresent_ReturnsValid()
    {
        var content = FullyPopulatedReq + """

        ## Architecture (Added by Pipeline 03)

        ### BDAT Analysis
        Business context here.

        ### Failure Modes
        1. Some failure mode.

        ### Integration Points
        | Component | Purpose |
        |-----------|---------|
        | SomeService | Does things |
        """;

        var service = new ContractValidationService();
        var result = service.ValidatePipeline03(content);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidatePipeline03_WhenMissingArchitectureSection_ReturnsViolation()
    {
        var service = new ContractValidationService();
        var result = service.ValidatePipeline03(FullyPopulatedReq);
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations,
            v => v.Contains("Architecture (Added by Pipeline 03)"));
    }

    [Fact]
    public void ValidatePipeline05_WhenPxdSectionPresent_ReturnsValid()
    {
        var content = FullyPopulatedReq + """

        ## PxD (Added by Pipeline 05)

        ### User Flow
        1. User does something.

        ### Component Specifications
        Some component specs.

        ### Accessibility Requirements
        WCAG 2.1 AA compliance required.
        """;

        var service = new ContractValidationService();
        var result = service.ValidatePipeline05(content);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidatePipeline05_WhenMissingPxdSection_ReturnsViolation()
    {
        var service = new ContractValidationService();
        var result = service.ValidatePipeline05(FullyPopulatedReq);
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations,
            v => v.Contains("PxD (Added by Pipeline 05)"));
    }
}
