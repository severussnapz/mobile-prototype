# SKILL: micro-interactions
# Phase: P05 Product Experience Design — Phase 8

## Micro-Interactions

**Purpose:** Define small animations and state transitions that communicate system status.

### Required Micro-Interactions

**Button loading state:**
- When `isLoading={true}`: show inline spinner, disable button, prevent double-submit
- Text changes: "Save" → "Saving..." during load, "Save" restored on complete

**Form field validation:**
- Inline validation: show error message below field on blur (not on every keystroke)
- Valid state: no visible indicator (avoid "green tick" clutter)
- Invalid state: red border + error text below

**List item actions (delete/archive):**
- On confirm: item animates out (fade or slide) before list reflows
- Avoids jarring jump as items are removed

**Toast / notification:**
- Success notifications: auto-dismiss after 5 seconds
- Error notifications: persist until manually dismissed
- Position: top-right, stacked if multiple

### Micro-Interaction Template

```markdown
### Micro-Interactions: {FeatureName}

| Trigger | Animation | Duration | Notes |
|---------|-----------|---------|-------|
| Form submit | Button spinner | Until API response | Disable inputs |
| Delete item | Fade out + collapse | 200ms | Smooth list reflow |
| Success save | Toast auto-dismiss | 5 seconds | |
| Validation error | Field border red | Instant | Show on blur |
```
