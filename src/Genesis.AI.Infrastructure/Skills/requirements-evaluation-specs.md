---
name: requirements-evaluation-specs
description: 'Use this skill when writing deterministic evaluation function specifications (CHECK patterns) for requirements — defines how to structure pass/fail test criteria that V3 coding agents will transform into executable tests.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Requirements Evaluation Specs

This skill defines how to write **Evaluation Function Specifications** — deterministic pass/fail criteria that V3 coding agents transform into executable tests. These are specifications, not code.

---

## Philosophy

Each requirement needs specifications that an AI coding agent can **verify without ambiguity**:

- Written in structured natural language
- Define binary pass/fail criteria
- Reference specific guardrails (CLIN-001, IG-004, etc.)
- Link to hazards and mitigations
- Provide concrete test scenarios with inputs and expected outputs

---

## CHECK Structure

Every CHECK follows this template:

```markdown
### CHECK {N}: {GUARDRAIL-ID} — {Check Name}

**Trigger:** {When this check applies — e.g., "Any API endpoint receives NHS number as input"}

**Test Scenario 1:** {Description}
- **Setup:** {Preconditions or test data setup}
- **Input:** {Specific input data}
- **Expected Response:** {HTTP status code or UI behaviour}
- **Expected Body:** {JSON structure or error message}
- **Validation:** {Additional assertions}

**Test Scenario 2:** {Description}
- **Input:** {Specific input}
- **Expected Response:** {Expected outcome}

**Applicable Guardrail:** {ID — e.g., CLIN-001}
**Hazard Addressed:** {HAZ-ID} — {Description}
**Mitigation:** {MIT-ID} — {Description}

**Pass Criteria:** {Binary condition — e.g., "Invalid NHS numbers REJECTED, Valid NHS numbers ACCEPTED"}
```

---

## Probing Questions for Evaluation Criteria

During requirements elicitation, ask these follow-ups for **each** requirement:

1. **"Give me a specific example of INVALID input that should be REJECTED"**
   - Get exact input value, expected error message, expected HTTP status

2. **"Give me a specific example of VALID input that should be ACCEPTED"**
   - Get exact input value, expected successful response

3. **"What specific data MUST be in a successful response?"**
   - Exact field names and formats (e.g., "patientId must be GUID, 36 characters")

4. **"What should happen BEFORE the system returns data?"**
   - Audit logging (CLIN-002), validation, authorisation checks

5. **"Are there any timing requirements?"**
   - "Audit log MUST be created BEFORE data returned"
   - "Response time MUST be under 500ms"

6. **"What happens if the user triggers this action a second time?"**
   - Idempotent actions MUST return the same logical result on repeat
   - HTTP 409 Conflict treated as success for logically idempotent operations

---

## Standard Frontend CHECKs

For any requirement with a UI component, include these automatically:

### CHECK A: DS-001 — No Native HTML Interactive Elements

**Trigger:** Any requirement involving a form, button, input, or interactive UI.

- Mount the React component
- Assert `document.querySelector('button, input, select, textarea, table, dialog, fieldset, legend, form')` returns `null`
- All interaction uses `@emisgroup/ui-*` equivalents

**Pass Criteria:** Zero native HTML interactive elements in rendered output.

### CHECK B: DS-002 — No Hardcoded Colour Values

**Trigger:** Any requirement where SCSS or CSS is introduced or modified.

- Read all `.scss` / `.css` files in the component directory
- Assert no match for `/#[0-9a-fA-F]{3,8}|rgb\(|rgba\(|hsl\(|hsla\(/`
- All colours use `var(--token-*)` design tokens

**Pass Criteria:** Zero hardcoded colour literals in style files.

### CHECK C: A11Y-004a — Form Inputs Have Accessible Labels

**Trigger:** Any requirement introducing a form field, text input, search box, or dropdown.

- Render component and run `await axe(container)`
- Assert zero violations with id `label` or `label-content-name-mismatch`
- Each input receives `aria-label` or `aria-labelledby`

**Pass Criteria:** jest-axe `toHaveNoViolations()` passes; no label violations.

### CHECK D: WCS-007a — All User-Visible Text Uses i18n

**Trigger:** Any requirement displaying text to users.

- Static analysis of `.tsx` files
- Assert zero multi-word text between `>` and `<` not using `{t(`
- All strings use `t('key')` from react-i18next
- British English in `locales/en-GB/*.json`

**Pass Criteria:** Zero hardcoded multi-word English strings in JSX.

### CHECK E: A11Y-010 — jest-axe in Every Component Test

**Trigger:** Any requirement producing a React component.

- Check corresponding test file exists
- Assert it contains `import { axe } from 'jest-axe'`
- Assert it contains `toHaveNoViolations()`

**Pass Criteria:** jest-axe assertion present in component test file.

---

## Typical CHECK Count Per Requirement

| Requirement type | Expected CHECKs |
|-----------------|----------------|
| Pure backend API | 4–8 (validation, auth, audit, response format, timing) |
| Frontend-only | 5–10 (DS-001, DS-002, A11Y-004a, WCS-007a, A11Y-010, plus domain) |
| Full-stack | 8–15 (all backend + all frontend) |

---

## Linking CHECKs to V3 Agents

Each CHECK maps to a V3 coding agent via file path:

- `*.cs` / `*.sql` / `{Service}.Api/` → **EMIS-X_API_ENGINEER** transforms CHECK into xUnit + Moq test
- `*.tsx` / `*.ts` / `src/components/` → **EMIS-X_WEBAPP_ENGINEER** transforms CHECK into Jest + jest-axe test

Include this mapping in the traceability table of each requirement.
