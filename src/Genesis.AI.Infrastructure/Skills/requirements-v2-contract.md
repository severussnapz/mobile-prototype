---
name: requirements-v2-contract
description: 'Use this skill when producing output that the V2 Normalisation agent will consume — defines the canonical heading registry (exact strings V2 searches for), the additive update model, DRAFT marker protocol, and the 6 JSON output schemas V2 produces.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Requirements V2 Contract

This skill defines the interface contract between pipeline agents (V1a–V1e) and the V2 Normalisation agent. V2 performs exact string matching on headings — any variation breaks extraction.

---

## Canonical Heading Registry

These are the **exact** headings V2 searches for. Same capitalisation, same punctuation, same spacing. Any deviation produces a silent `MISSING` in the extracted JSON.

### V1b Architecture headings

| Section you write | Exact heading V2 searches for |
|---|---|
| Top-level architecture block | `## Architecture (Added by V1b)` |
| BDAT subsection | `### BDAT Analysis` |
| ADR list | `### Architecture Decision Records` |
| Failure modes | `### Failure Modes & Resilience` |
| Integration points | `### Integration Points` |
| Traceability updates | `## Traceability` |

### V1c Design headings

| Section you write | Exact heading V2 searches for |
|---|---|
| Top-level design block | `## Design (Added by V1c)` |
| API contract | `### API Contract (OpenAPI 3.0)` |
| Database schema | `### Database Schema` |
| Component interfaces | `### Component Interfaces` |
| State machines | `### State Machine Design` |
| Cross-requirement orchestration | `### Cross-Requirement Orchestration` |
| Traceability updates | `## Traceability` |

### V1d PxD headings

| Section you write | Exact heading V2 searches for |
|---|---|
| Top-level PxD block | `## PxD (Added by V1d)` |
| Component specifications | `### Component Specifications` |
| User flows | `### User Flow` |
| Wireframes | `### Wireframes` |
| Accessibility requirements | `### Accessibility Requirements` |
| Traceability updates | `## Traceability` |

### V1e Clinical Safety headings

| Section you write | Exact heading V2 searches for |
|---|---|
| Top-level clinical safety block | `## Clinical Safety (Added by V1e)` |
| Genesis AI skills applied | `### Genesis AI Skills Applied` |
| Hazard log entries | `### Hazard Log Entries` |
| Mitigations | `### Mitigations` |
| Residual risk | `### Residual Risk Assessment` |
| Traceability updates | `## Traceability` |

---

## Additive Update Model

Each V1 agent **adds** sections to existing requirement files. They do NOT replace or restructure content written by prior agents.

```
V1a creates:    REQ-001.md (user story, acceptance criteria, 4 dimensions, eval specs)
V1b appends:    ## Architecture (Added by V1b)
V1c appends:    ## Design (Added by V1c)
V1d appends:    ## PxD (Added by V1d)
V1e appends:    ## Clinical Safety (Added by V1e)
```

Rules:
- Never modify content from a prior agent
- Always use the exact heading from the registry above
- Append at the end of the file, before the `## Change Log` section
- Update the `## Traceability` table (append rows, don't replace)

---

## DRAFT Marker Protocol

When writing content that has not yet been validated by the user or CSO:

```markdown
<!-- DRAFT — pending final validation -->
## Architecture (Added by V1b)
...content...
```

Remove the DRAFT marker only when:
- The user explicitly confirms the section
- The phase's final validation step is complete

---

## MISSING and VALIDATION_ERROR Handling

When V2 cannot find expected content:

- Missing heading → `"MISSING"` in the JSON field
- Malformed content → `"VALIDATION_ERROR: {reason}"` in the field
- Both are logged to `_session/parking_lot.md`

**Resolution:** Before V2 hands off to V1f, ALL `MISSING` and `VALIDATION_ERROR` values must be resolved. If they cannot be resolved from source, V2 halts and reports the gaps.

---

## V2 Output Files (6 JSON)

V2 produces these files in `output/`:

| File | Source Agent | Content |
|------|-------------|---------|
| `API_Contracts.json` | V1c Design | OpenAPI 3.0 specs per requirement |
| `Database_Schemas.json` | V1c Design | DDL, tables, indexes, constraints |
| `Component_Interfaces.json` | V1c Design | C# interfaces, methods, DI registration |
| `UI_Component_Specs.json` | V1d PxD | React components, props, state, accessibility |
| `CS_Guardrails.json` | V1e Clinical Safety | Guardrail rules linked to code components |
| `Traceability_Map.json` | All V1 agents | REQ → HAZ → MIT → Guardrail → CHECK mapping |

---

## V2 Transformation Rules

1. **Exact extraction** — no interpretation, no inference, no generated examples
2. **Guardrails embedded** — V3 coding agent cannot skip them
3. **Traceability preserved** — every JSON entry links back to source requirement
4. **Validation before output** — required fields present, types correct, cross-refs resolved
