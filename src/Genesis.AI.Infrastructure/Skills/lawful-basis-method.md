# SKILL: lawful-basis-method
# Phase: P07 Information Governance — Phase 1

## Lawful Basis Determination

**Purpose:** Determine the appropriate UK GDPR lawful basis for each data processing activity.

### Skip Rule

If ROUTING CONTEXT `lawful_basis_confirmed: true` → skip Phase 1 Step 1 and go to data flow mapping.

### UK GDPR Article 6 Lawful Bases

| Basis | Ref | When to use |
|-------|-----|-------------|
| Consent | Art 6(1)(a) | Only if data subject can meaningfully refuse. NOT appropriate for NHS clinical systems (power imbalance). |
| Contract | Art 6(1)(b) | Processing necessary to perform a contract with the data subject |
| Legal obligation | Art 6(1)(c) | Required by law (CQC, NHS obligations) |
| Vital interests | Art 6(1)(d) | Emergency / life-or-death situations only |
| Public task | Art 6(1)(e) | **Primary basis for NHS systems** — processing in the public interest |
| Legitimate interests | Art 6(1)(f) | Not appropriate for public authorities in relation to their public functions |

### NHS Guidance

For NHS clinical systems: **Public task (Art 6(1)(e))** is the standard lawful basis. Consent is inappropriate for core clinical records.

### Controller/Processor Baseline

For healthcare provider deployments, treat the care provider organisation (for example GP practice, PCN, or provider trust) as Data Controller and the solution supplier as Data Processor, unless explicit legal/DPO evidence confirms a different arrangement.

Any joint-controller declaration must include a legal evidence reference.

### Special Category Data (Article 9)

If `data_class = special_category`, also determine Article 9 condition:

| Condition | When |
|-----------|------|
| Art 9(2)(c) | Vital interests (unable to consent) |
| Art 9(2)(h) | **Health/social care provision** — primary basis for NHS systems |
| Art 9(2)(i) | Public health |

### Lawful Basis Output

```markdown
### Lawful Basis

**Article 6 basis:** {e.g. Public task — Art 6(1)(e)}
**Article 9 basis (if applicable):** {e.g. Health/social care — Art 9(2)(h)}
**Data Controller:** {care provider organisation}
**Data Processor:** {solution supplier}
**Joint Controller:** {No by default; if Yes include legal evidence reference}
**Rationale:** {Why this basis applies to this specific processing}
**Confirmed by:** {DPIA reference / DPO / CSO}
```
