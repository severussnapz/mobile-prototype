---
name: requirements-four-dimensions
description: 'Use this skill when analysing requirements across the four (plus one) dimensions: clinical safety, information governance, security, observability, and frontend/accessibility — includes per-dimension question lists, guardrail mappings, and the IG-003 hard gate.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Requirements Four Dimensions

This skill defines the multi-dimensional analysis framework used during requirements elicitation. Every requirement is analysed across these dimensions to ensure comprehensive coverage of healthcare-specific concerns.

---

## The Five Dimensions

| # | Dimension | Concern | Key Guardrail Prefixes |
|---|-----------|---------|----------------------|
| 1 | Clinical Safety | Patient/practitioner safety, DCB0129/0160 | CLIN-001 to CLIN-010 |
| 2 | Information Governance | UK GDPR, DPA 2018, NHS DSPT, consent | IG-001 to IG-010 |
| 3 | Security | CIS2 auth, encryption, audit trails | AUTH-*, SEC-* |
| 4 | Observability & Performance | OTEL, KPIs, SLOs, alerting | OBS-* |
| 5 | Frontend & Accessibility | EMIS design system, WCAG, i18n | DS-*, A11Y-*, WCS-*, HTTP-* |

Dimension 5 applies **only** to requirements with a UI component. Dimensions 1–4 apply to all requirements.

---

## Dimension 1: Clinical Safety

### Questions to ask

- "Are there any patient safety implications?"
- If YES: "What could go wrong? What's the hazard?"
- "Which clinical safety guardrails apply?" (CLIN-001 to CLIN-010)

### Guardrail mapping

Load skill `emis-x-api-clinical-safety` for full CLIN-001 to CLIN-010 rule definitions. Key rules:

| ID | Name | Severity |
|----|------|----------|
| CLIN-001 | NHS Number Validation (Modulus 11) | Critical |
| CLIN-002 | Patient Data Audit Trail (before return) | Critical |
| CLIN-003 | Medication Dosing BNF Limits | Critical |
| CLIN-004 | Allergy Check Required Before Prescribing | Critical |
| CLIN-006 | Patient Identifier Validation (GUID not int) | High |
| CLIN-007 | Hazard Mitigation Evidence in UI | High |
| CLIN-010 | Error Handling Patient Safety (Result<T>) | High |

### Output per requirement

```markdown
## Dimension 1: Clinical Safety

### Applicable Guardrails
- **CLIN-XXX:** {name and description}

### Hazards Addressed
- **HAZ-XXX:** {description}
  - Severity: {High/Medium/Low}
  - Likelihood: {High/Medium/Low}

### Mitigations
- **MIT-XXX:** {description}
  - Type: {Validation/UI Control/Business Logic/Monitoring}
```

---

## Dimension 2: Information Governance

### Questions to ask

- "What personal or health data is involved?"
- "What is the lawful basis for processing?" (GDPR Article 6)
- "Is this special category data?" (GDPR Article 9)
- "What is the retention period?"
- "Who is the Data Controller and who is the Data Processor for this flow?"
  - Default expectation for healthcare provider deployments: provider organisation (for example GP practice/PCN/provider trust) is controller; solution supplier is processor.
  - Any joint-controller claim requires explicit legal/DPO evidence.

### IG-003 Hard Gate

> 🚫 If the lawful basis **cannot** be confirmed by the user in the interview, you MUST:
>
> 1. Tag it as `[UNVERIFIED — IG-OWNER: {named person} — RESOLUTION DATE: {target date} — GO-LIVE BLOCKER]`
> 2. Add a 🔴 Blocker entry to the parking lot
> 3. Before final output generation: scan all requirements for unresolved IG-003. If any exist, prompt the user to assign an owner and resolution date.

A bare `[UNVERIFIED — confirm before submission]` on IG-003 is **not acceptable** output.

### Guardrail mapping

Load skill `emis-x-api-information-governance` (if available) for full IG rule definitions. Key rules:

| ID | Name | Severity |
|----|------|----------|
| IG-001 | Data Minimisation | Critical |
| IG-003 | Lawful Basis Declaration | Critical |
| IG-004 | Special Category Data Protection (AES-256-GCM) | Critical |
| IG-005 | Data Subject Rights (Articles 15-20) | High |
| IG-007 | Cross-Border Data Transfer Controls | Critical |
| IG-008 | Consent Management | High |
| IG-010 | Retention Policy Enforcement | High |

### Output per requirement

```markdown
## Dimension 2: Information Governance

### Applicable Guardrails
- **IG-XXX:** {name and description}

### GDPR Articles
- **Article 6:** {Lawful basis}
- **Article 9:** {Special category basis — if applicable}

### Controllership Allocation
- **Data Controller:** {care provider organisation}
- **Data Processor:** {solution supplier(s)}
- **Joint Controller:** {No by default; if Yes include legal evidence reference}

### Data Handling Requirements
- **Data Categories:** {personal details, health data, contact info}
- **Data Subjects:** {patients, clinicians, administrators}
- **Retention Period:** {8 years adult, 25+8 paediatric, etc.}
- **Data Minimisation:** {only collect/return: field1, field2, field3}
```

---

## Dimension 3: Security

### Questions to ask

- "Does this require authentication?"
- "What authorisation is needed?" (Roles, scopes, permissions)
- "Does this need encryption?" (In transit, at rest)
- "Will any user-supplied values be embedded in URLs or API query strings?"
  - If YES: `encodeURIComponent()` is mandatory (WSEC-006a)

### Guardrail mapping

Load skill `emis-x-api-security` for full SEC/AUTH rule definitions. Key rules:

| ID | Name |
|----|------|
| AUTH-004 | Authorisation Required (JWT + scope) |
| SEC-001 | TLS Encryption (1.2+) |
| SEC-002 | No Secrets in Code (Key Vault) |

### Output per requirement

```markdown
## Dimension 3: Security

### Applicable Guardrails
- **AUTH-XXX:** {name}
- **SEC-YYY:** {name}

### Security Requirements
- **Authentication:** {CIS2 OAuth2, JWT tokens}
- **Authorisation:** {Required scopes}
- **Encryption in Transit:** {TLS 1.2+}
- **Encryption at Rest:** {AES-256-GCM for special category data}
```

---

## Dimension 4: Observability & Performance

### Questions to ask

- "What KPIs measure success for this requirement?"
- "What OTEL spans should instrument this?" (Format: `{product}.{feature}.{action}`)
- "What is the performance SLO?" (p95 latency, availability, error rate)
- "What alerting conditions are critical?"

### Output per requirement

```markdown
## Dimension 4: Observability & Performance

### Product KPIs
- **KPI 1:** {metric} — Baseline: {X}, Target: {Y}

### Observable Events (OTEL Instrumentation)
- **Span:** `{product}.{feature}.{action}.start`
  - Attributes: {attr1, attr2}

### Performance SLOs
- **Latency p95:** < {X}ms
- **Availability:** {99.9}%
- **Error Rate:** < {0.1}%

### Alerting Conditions
- **Critical:** {condition} → {channel}
```

---

## Dimension 5: Frontend & Accessibility

**Only applies to requirements with a UI component.**

### Questions to ask

- "Does this requirement have any UI input fields?"
  - If YES: every input MUST have `aria-label` or `aria-labelledby` (A11Y-004a)
- "Does this requirement have loading, error, or status states?"
  - If YES: MUST have `role="status"`, `role="alert"`, or `aria-live="polite"` (A11Y-007a)
- "Does this requirement render user-facing text?"
  - YES assumed for all UI — `t()` from react-i18next mandatory (WCS-007a/b)

### EMIS-X Platform Non-Negotiables

These apply to **every** frontend requirement automatically. Do NOT ask whether to include them:

| Mandate | Guardrail | Rule |
|---|---|---|
| pnpm only | WA-005 | pnpm-lock.yaml required |
| @emisgroup/ui-* components only | DS-001 | No native interactive elements |
| Design tokens only | DS-002 | No hardcoded hex/rgb/hsl |
| Iconify icons only | DS-004 | ~icons/ic/outline-* |
| Security headers package | WSEC-013 | @emisgroup/acp-security-headers |
| axios.create() + timeout | HTTP-002a | No raw fetch() |
| react-i18next t() | WCS-007a | All UI text |
| British English | WCS-007b | colour, centre, grey, behaviour |
| jest-axe | A11Y-010 | toHaveNoViolations() in every test |

### Guardrail mapping

Load skills `emis-x-webapp-design-system`, `emis-x-webapp-accessibility`, `emis-x-webapp-coding-standards` for full rule definitions.
