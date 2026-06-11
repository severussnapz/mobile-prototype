# SKILL: interaction-patterns
# Phase: P05 Product Experience Design — Phase 4

## Interaction Patterns

**Purpose:** Define micro-interactions, animations, and user feedback patterns.

### Standard Interaction Patterns (Apply to All)

**Form submission:**
1. User clicks Submit → Button shows loading state (`isLoading={true}`)
2. API call in progress → `<ProgressSpinner>` visible, form inputs disabled
3. Success → Success Banner displays, form resets or navigates
4. Error → Error Banner displays with specific message from API

**Data loading:**
1. Component mounts → `<ProgressSpinner>` displayed immediately
2. Data arrives → Content renders
3. Error → `<Banner variant="error">` with retry option

**Optimistic updates (when appropriate):**
- Suitable for low-risk actions (toggle, reorder)
- Always show loading indicator
- Revert immediately on error with Banner notification

**Confirmation dialogs:**
- Destructive actions (delete, irreversible changes) MUST use `<Dialog>` from `@emisgroup/ui-dialog`
- Dialog title: "Confirm {action}"
- Primary button: `variant="danger"` — "Delete" / "Confirm"
- Cancel button: `variant="mono"` — "Cancel"

### Interaction Pattern Template

```markdown
### Interaction Patterns for {FeatureName}

| Action | Trigger | Loading state | Success | Error |
|--------|---------|-------------|---------|-------|
| {Create} | Submit button | Button spinner + disabled inputs | Success Banner | Error Banner from API |
| {Delete} | Delete button | Confirm Dialog → loading | Remove from list | Error Banner |
| {Load data} | Mount | ProgressSpinner | Render content | Error Banner + retry |
```
