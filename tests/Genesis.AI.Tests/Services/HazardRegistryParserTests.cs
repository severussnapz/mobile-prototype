using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public class HazardRegistryParserTests
{
    private readonly HazardRegistryParser _parser = new();

    private const string SampleRegistry = """
        # Hazard Registry — ACME Patient Portal

        ## HAZ-DOC-001: Patient Identification — Wrong patient record displayed
        **Source requirement:** REQ-001-patient-lookup
        **Status:** Active

        ### HAZ-DOC-001: Wrong patient record displayed
        **Hazard description:** A clinician is shown the record of a patient other than the one they intended to open.
        **Potential clinical impact:** Clinical decisions made against the wrong record could cause serious harm.
        **Initial risk:** Major × Possible = **High**
        **Residual risk:** Major × Unlikely = **Low**
        **Residual risk decision:** Acceptable with controls
        **Existing Controls:** Patient banner displays NHS number on every screen.

        #### Cause 1: Ambiguous search returns multiple matches
        | Control ID | Category | Description | CLIN Rule | Evidence ID | Status Proof | Additional Comments | Go/Launch Gate |
        | --- | --- | --- | --- | --- | --- | --- | --- |
        | C-001 | HIT Design | Force NHS number confirmation before opening | CLIN-002 | EV-101 | Done | — | Yes |
        | C-002 | Training | Train staff on identity verification | CLIN-003 | EV-102 | Done | — | No |

        #### Cause 2: Stale cache shows previous patient
        | Control ID | Category | Description | CLIN Rule | Evidence ID | Status Proof | Additional Comments | Go/Launch Gate |
        | --- | --- | --- | --- | --- | --- | --- | --- |
        | C-003 | Business Process | Clear context on patient switch | CLIN-004 | EV-103 | Done | — | Yes |

        ### Genesis AI Skills Applied
        - WCLIN-001

        ## HAZ-DOC-002: Medication — Allergy alert suppressed
        **Source requirement:** REQ-014-prescribing
        **Status:** Active

        ### HAZ-DOC-002: Allergy alert suppressed
        **Hazard description:** An allergy alert is not displayed when prescribing a contraindicated medicine.
        **Potential clinical impact:** Patient may receive a medicine to which they have a known allergy.
        **Initial risk:** Catastrophic × Possible = **High**
        **Residual risk:** Catastrophic × Rare = **Moderate**
        **Residual risk decision:** Acceptable
        **Existing Controls:** Hard-stop alert on contraindication.

        #### Cause 1: Alert engine times out
        | Control ID | Category | Description | CLIN Rule | Evidence ID | Status Proof | Additional Comments | Go/Launch Gate |
        | --- | --- | --- | --- | --- | --- | --- | --- |
        | C-010 | HIT Design | Fail safe to blocking state on timeout | CLIN-005 | EV-201 | Done | — | Yes |
        """;

    [Fact]
    public void Parse_EmptyContent_ReturnsEmptyList()
    {
        var result = _parser.Parse(string.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_WhitespaceContent_ReturnsEmptyList()
    {
        var result = _parser.Parse("   \n  \n");

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_TwoHazards_ReturnsBothHazards()
    {
        var result = _parser.Parse(SampleRegistry);

        Assert.Equal(2, result.Count);
        Assert.Equal("HAZ-DOC-001", result[0].HazardReference);
        Assert.Equal("HAZ-DOC-002", result[1].HazardReference);
    }

    [Fact]
    public void Parse_EmDashHeading_SplitsAreaAndDescription()
    {
        var result = _parser.Parse(SampleRegistry);

        Assert.Equal("Patient Identification", result[0].HazardArea);
    }

    [Fact]
    public void Parse_SourceRequirement_ExtractsRequirementReference()
    {
        var result = _parser.Parse(SampleRegistry);

        Assert.Equal("REQ-001", result[0].SourceRequirement);
    }

    [Fact]
    public void Parse_InitialRisk_ExtractsSeverityLikelihoodAndLevel()
    {
        var result = _parser.Parse(SampleRegistry);

        var hazard = result[0];
        Assert.Equal("Major", hazard.InitialSeverity);
        Assert.Equal("Possible", hazard.InitialLikelihood);
        Assert.Equal("High", hazard.InitialRisk);
    }

    [Fact]
    public void Parse_ResidualRisk_ExtractsSeverityLikelihoodAndLevel()
    {
        var result = _parser.Parse(SampleRegistry);

        var hazard = result[0];
        Assert.Equal("Major", hazard.ResidualSeverity);
        Assert.Equal("Unlikely", hazard.ResidualLikelihood);
        Assert.Equal("Low", hazard.ResidualRisk);
    }

    [Fact]
    public void Parse_MultipleCauses_ReturnsAllCauses()
    {
        var result = _parser.Parse(SampleRegistry);

        Assert.Equal(2, result[0].Causes.Count);
        Assert.Equal("Ambiguous search returns multiple matches", result[0].Causes[0].Description);
        Assert.Equal("Stale cache shows previous patient", result[0].Causes[1].Description);
    }

    [Fact]
    public void Parse_ControlTable_ExtractsCategoryDescriptionAndEvidenceByHeaderName()
    {
        var result = _parser.Parse(SampleRegistry);

        var firstCauseControls = result[0].Causes[0].Controls;
        Assert.Equal(2, firstCauseControls.Count);

        var hitControl = firstCauseControls[0];
        Assert.Equal("HIT Design", hitControl.Category);
        Assert.Equal("Force NHS number confirmation before opening", hitControl.Description);
        Assert.Equal("EV-101", hitControl.Evidence);
    }

    [Fact]
    public void Parse_LevelFiveCauseHeading_IsToleratedAsCause()
    {
        const string registry = """
            ## HAZ-DOC-003: Logging — PII written to logs
            **Source requirement:** REQ-020-logging
            **Hazard description:** Patient identifiers leak into application logs.
            **Initial risk:** Moderate × Possible = **Moderate**
            **Residual risk:** Moderate × Rare = **Low**

            ##### Cause 1: Unredacted exception message
            | Control ID | Category | Description | CLIN Rule | Evidence ID | Status Proof | Additional Comments | Go/Launch Gate |
            | --- | --- | --- | --- | --- | --- | --- | --- |
            | C-030 | HIT Design | Redact PII before logging | CLIN-006 | EV-301 | Done | — | Yes |
            """;

        var result = _parser.Parse(registry);

        Assert.Single(result);
        Assert.Single(result[0].Causes);
        Assert.Equal("Unredacted exception message", result[0].Causes[0].Description);
    }

    [Fact]
    public void Parse_HazardWithoutCauseTable_ProducesPlaceholderCause()
    {
        const string registry = """
            ## HAZ-DOC-004: Performance — Slow record load
            **Source requirement:** REQ-040-performance
            **Hazard description:** Record takes too long to load.
            **Initial risk:** Minor × Possible = **Low**
            **Residual risk:** Minor × Rare = **Low**
            """;

        var result = _parser.Parse(registry);

        Assert.Single(result);
        Assert.Single(result[0].Causes);
        Assert.Equal("[See hazard description]", result[0].Causes[0].Description);
    }
}
