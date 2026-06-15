# SKILL: context-loading-p05
# Phase: P05 Product Experience Design — Phase 0

## Apply Prior Iteration Learnings

Check: does the workspace contain `feedback/ITERATION_REPORT_P05_i*.md`?
- **YES** → Read the most recent. Apply all HIGH priority improvements silently.
- **NO** → Proceed. This is iteration 1.

## Step 1: Load P04 Design Outputs

Load `manifest.md` and all `requirements/REQ-*.md` files. Focus on `## Design (Added by Pipeline 04)` sections — specifically `### API Contract` and `### Component Interfaces`.

> **PRECEDENCE NOTE:** PROJECT FOUNDATION content is pre-loaded — do NOT reload those files.

> 🚫 **CODEBASE ISOLATION:** Load ONLY `manifest.md` and `requirements/REQ-*.md`. Do NOT read source code files.

Summarise: "I've loaded {N} requirements. Design sections present for {M}/{N}."

## Step 2: Check for Prototype

From ROUTING CONTEXT: `prototype_present`.
- `true` → A validated HTML prototype exists at `prototype/index.html`. Read it. Extract: confirmed user flows, accepted component patterns, user feedback notes. In Phase 1, take confirmed flows as authoritative — do not redesign accepted flows.
- `false` → Full user flow design in Phase 1. No prototype constraints.

## Step 3: Load EMIS UI Kit Baseline

Load skills: `emis-ui-kit-baseline`. This baseline must be applied to ALL component design decisions throughout P05.

## Step 4: Create P05 Review List

Create `feedback/P05_REVIEW_LIST.md`:

```markdown
# Pipeline 05 Review List — {PRODUCT_NAME}
**Started:** {DATE} | **Last Updated:** {DATE}

| REQ-ID | Name | Flows | Wireframe | Components | Accessibility | Written | Flag | Note |
|--------|------|-------|-----------|-----------|--------------|---------|------|------|
| REQ-001 | {name} | ⏳ | | | | | | |
```
