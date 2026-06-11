# SKILL: gap-fill-verification
# Phase: P03 Architecture — Phase 12

## Gap-Fill Verification Pass

**Purpose:** Phase 12 is a verification and gap-fill pass only. BDAT Analysis sections were written to each file immediately after Phase 2 confirmation. Phase 12 confirms all 12 required sub-sections are present and adds any that are missing.

### Required Sub-Sections (All 12 Mandatory)

For every requirement file, verify these headings exist under `## Architecture (Added by Pipeline 03)`:

1. `### BDAT Analysis`
2. `### Architecture Decision Records`
3. `### Platform Boundaries`
4. `### Service Classification`
5. `### Failure Modes & Resilience`
6. `### Integration Points`
7. `### AWS Well-Architected`
8. `### EMIS Principles`
9. `### Operations`
10. `### Performance & Cost`
11. `### Security`
12. `### Diagrams`

### Uniform Depth Rule

Every requirement file MUST contain ALL 12 sub-sections. Do NOT abbreviate or omit sub-sections for earlier files, simpler requirements, or requirements with no external integrations.

If a sub-section is not applicable, write a one-line note rather than omitting the heading:
- Example: `No external integrations — this requirement is fully internal to {ServiceName}.`
- Example: `No diagrams — data flow is simple read-only GET; sequence is covered by the component diagram.`

### Before Moving to Next File

After gap-fill writing each file, verify all 12 headings are present before proceeding to the next requirement file.
