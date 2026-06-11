# SKILL: empty-states
# Phase: P05 Product Experience Design — Phase 10

## Empty States

**Purpose:** Define designs for lists, tables, and views with no data.

### Empty State Categories

**First use (no data ever added):**
- Motivational message: explain the feature and what to do
- Primary action button to create first item
```
┌──────────────────────────────────┐
│  [Icon: mdi:clipboard-outline]   │
│                                  │
│  No {entities} yet               │
│                                  │
│  {Short description of value}    │
│                                  │
│  [Button: Add {entity}]          │
└──────────────────────────────────┘
```

**Filtered empty (data exists but filtered out):**
- Explain that filters are active
- Offer to clear filters
```
│  No {entities} match your filters │
│  [Button: Clear filters]          │
```

**Permission empty (data exists but user can't see it):**
```
│  No {entities} are visible to you │
│  Contact your administrator       │
```

### Empty State Template

```markdown
### Empty States: {FeatureName}

| Scenario | Icon | Heading | Message | Action |
|---------|------|---------|---------|--------|
| First use | {Iconify icon} | "No {entities} yet" | {Value description} | Add {entity} button |
| Filtered | mdi:filter-off | "No results" | "No {entities} match your filters" | Clear filters button |
| No permission | mdi:lock-outline | "Nothing to show" | "Contact your administrator" | None |
```
