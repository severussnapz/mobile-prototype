# SKILL: context-loading-p07
# Phase: P07 Information Governance — Phase 0

## Apply Prior Iteration Learnings

Check: does the workspace contain `feedback/ITERATION_REPORT_P07_i*.md`?
- **YES** → Read the most recent. Apply all HIGH priority improvements silently.
- **NO** → Proceed. This is iteration 1.

## Step 1: Load P06 Clinical Safety Outputs

Load `manifest.md` and all `requirements/REQ-*.md` files. Focus on `## Clinical Safety (Added by Pipeline 06)` sections — specifically hazard registers and residual risk levels.

> **PRECEDENCE NOTE:** PROJECT FOUNDATION content is pre-loaded — do NOT reload those files.

Summarise: "I've loaded {N} requirements. Clinical safety sections present for {M}/{N}."

## Step 2: DPIA Reference Check

From ROUTING CONTEXT: `dpia_reference_existing`.
- `true` → An existing DPIA exists for this system. Load its reference from Project Foundation. Phase 1 will delta-assess: only new data flows / personal data types introduced by this product.
- `false` → Full DPIA design needed in Phase 1.

## Step 3: Lawful Basis Confirmation

From ROUTING CONTEXT: `lawful_basis_confirmed`.
- `true` → Lawful basis already confirmed in DPIA reference. Skip Phase 1 Step 1 (basis selection). Go straight to data flow mapping.
- `false` → Phase 1 must start with lawful basis determination.

## Step 4: Data Classification Check

From ROUTING CONTEXT: `data_class`.
- If `special_category` → NHS Number, clinical records, health data → apply UK GDPR Article 9 enhanced rules throughout.
- If `personal` → Standard UK GDPR Article 6 rules.
- If `anonymous` → IG obligations minimal — document anonymisation basis.

## Step 5: Create P07 Review List

Create `feedback/P07_REVIEW_LIST.md`:

```markdown
# Pipeline 07 IG Review List — {PRODUCT_NAME}
**Started:** {DATE} | **Scope:** {FULL | DELTA}

| REQ-ID | Name | Lawful Basis | Data Class | Retention | Controls | DPIA Entry | Written | Flag |
|--------|------|------------|-----------|---------|---------|-----------|---------|------|
| REQ-001 | {name} | ⏳ | | | | | | |
```
