---
name: pipeline-normalisation-contract
description: 'Use this skill when producing output that P09 Normalisation will consume — defines the canonical heading registry (exact strings P09 searches for), the additive update model, DRAFT marker protocol, and the 6 JSON output schemas P09 produces.'
metadata:
  version: 2.0.0
  applyTo:
    - requirements
---

# Requirements P09 Contract

This skill defines the interface contract between pipeline stages (P03–P08) and P09 Normalisation.
P09 performs exact string matching on headings — any variation produces a silent `MISSING` in the extracted JSON.

---

## Canonical Heading Registry

These are the **exact** headings P09 searches for. Same capitalisation, same punctuation, same spacing.
Any deviation produces a silent `MISSING` in the extracted JSON.

### P03 Architecture headings

| Section you write | Exact heading P09 searches for |
|---|---|
| Top-level architecture block | `## Architecture (Added by Pipeline 03)` |
| BDAT subsection | `### BDAT Analysis` |
| ADR list | `### Architecture Decision Records` |
| Failure modes | `### Failure Modes & Resilience` |
| Integration points | `### Integration Points` |
| Traceability updates | `## Traceability` |

### P04 Design headings

| Section you write | Exact heading P09 searches for |
|---|---|
| Top-level design block | `## Design (Added by Pipeline 04)` |
| API contract | `### API Contract (OpenAPI 3.0)` |
| Database schema | `### Database Schema` |
| Component interfaces | `### Component Interfaces` |
| State machines | `### State Machine Design` |
| Cross-requirement orchestration | `### Cross-Requirement Orchestration` |
| Traceability updates | `## Traceability` |

### P05 PxD headings

| Section you write | Exact heading P09 searches for |
|---|---|
| Top-level PxD block | `## PxD (Added by Pipeline 05)` |
| Component specifications | `### Component Specifications` |
| User flows | `### User Flow` |
| Wireframes | `### Wireframes` |
| Accessibility requirements | `### Accessibility Requirements` |
| Traceability updates | `## Traceability` |

### P06 Clinical Safety headings

| Section you write | Exact heading P09 searches for |
|---|---|
| Top-level clinical safety block | `## Clinical Safety (Added by Pipeline 06)` |
| Genesis AI skills applied | `### Genesis AI Skills Applied` |
| Hazard log entries | `### Hazard Log Entries` |
| Mitigations | `### Mitigations` |
| Residual risk | `### Residual Risk Assessment` |
| Traceability updates | `## Traceability` |

### P07 Information Governance headings

| Section you write | Exact heading P09 searches for |
|---|---|
| Top-level IG block | `## Information Governance (Added by Pipeline 07)` |
| Lawful basis | `### Lawful Basis` |
| Data classification | `### Data Classification` |
| Retention and deletion | `### Retention and Deletion` |
| IG controls | `### Information Governance Controls` |
| Traceability updates | `## Traceability` |

### P08 Security headings

| Section you write | Exact heading P09 searches for |
|---|---|
| Top-level security block | `## Security (Added by Pipeline 08)` |
| Threat model | `### Threat Model` |
| Controls | `### Security Controls` |
| OWASP mapping | `### OWASP Mapping` |
| CHECKs | `### Security CHECKs` |
| Traceability updates | `## Traceability` |

---

## Additive Update Model

Each pipeline stage **adds** sections to existing requirement files. They do NOT replace or restructure content written by prior stages.

```
P01 creates:    REQ-001.md (user story, acceptance criteria, 4 dimensions, eval specs)
P03 appends:    ## Architecture (Added by Pipeline 03)
P04 appends:    ## Design (Added by Pipeline 04)
P05 appends:    ## PxD (Added by Pipeline 05)
P06 appends:    ## Clinical Safety (Added by Pipeline 06)
P07 appends:    ## Information Governance (Added by Pipeline 07)
P08 appends:    ## Security (Added by Pipeline 08)
```

Rules:
- Never modify content from a prior stage
- Always use the exact heading from the registry above
- Append at the end of the file, before the `## Change Log` section
- Update the `## Traceability` table (append rows, don't replace)

---

## DRAFT Marker Protocol

When writing content that has not yet been validated by the user or CSO:

```markdown
<!-- DRAFT — pending final validation -->
## Architecture (Added by Pipeline 03)
...content...
```

Remove the DRAFT marker only when:
- The user explicitly confirms the section
- The phase's final validation step is complete

---

## MISSING and VALIDATION_ERROR Handling

When P09 cannot find expected content:

- Missing heading → `"MISSING"` in the JSON field
- Malformed content → `"VALIDATION_ERROR: {reason}"` in the field
- Both are logged to `feedback/P09_REVIEW_LIST.md`

**Resolution:** Before P09 hands off to P10, ALL `MISSING` and `VALIDATION_ERROR` values must be resolved.
If they cannot be resolved from source, P09 halts and reports the gaps.

---

## P09 Output Files (6 JSON)

P09 produces these files in `output/`:

| File | Source Stage | Content |
|------|-------------|---------|
| `API_Contracts.json` | P04 Design | OpenAPI 3.0 specs per requirement |
| `Database_Schemas.json` | P04 Design | DDL, tables, indexes, constraints |
| `Component_Interfaces.json` | P04 Design | C# interfaces, methods, DI registration |
| `UI_Component_Specs.json` | P05 PxD | React components, props, state, accessibility |
| `CS_Guardrails.json` | P06 Clinical Safety | Guardrail rules linked to code components |
| `Traceability_Map.json` | All stages P03–P08 | REQ → HAZ → MIT → Guardrail → CHECK mapping |

---

## P09 Transformation Rules

1. **Exact extraction** — no interpretation, no inference, no generated examples
2. **Guardrails embedded** — P10 Planning and coding agents cannot skip them
3. **Traceability preserved** — every JSON entry links back to source requirement
4. **Validation before output** — required fields present, types correct, cross-refs resolved
