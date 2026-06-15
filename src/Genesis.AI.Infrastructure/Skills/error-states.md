# SKILL: error-states
# Phase: P05 Product Experience Design — Phase 9

## Error States

**Purpose:** Define specific error state designs for every user-facing operation.

### Error State Categories

**API error (network/server):**
```
<Banner variant="error">
  <strong>Unable to {operation}.</strong>
  {Specific reason from API, or "Please try again."}
  [Try again] button
</Banner>
```

**Validation error (form):**
- Inline per field: red border + error text below field
- If multiple fields: summary Banner at top listing all errors
- Scroll to first error on submit

**Not found:**
```
<Banner variant="error">
  {Entity} not found. It may have been deleted or you may not have permission to view it.
</Banner>
```

**Permission denied:**
```
<Banner variant="warning">
  You do not have permission to {action}.
  Contact your administrator if you believe this is an error.
</Banner>
```

**Empty result (not an error, but handled here):**
→ See `empty-states` skill.

### Error State Template

```markdown
### Error States: {FeatureName}

| Scenario | Component | Message | Recovery |
|---------|-----------|---------|---------|
| API 500 | Banner error | "Unable to {action}. Please try again." | Retry button |
| API 422 | Inline field + Banner | Field-specific messages from API | Fix and resubmit |
| API 404 | Banner error | "{Entity} not found." | Back button |
| API 403 | Banner warning | "You do not have permission." | Contact admin link |
```
