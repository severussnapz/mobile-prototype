# SKILL: responsive-design
# Phase: P05 Product Experience Design — Phase 6

## Responsive Design

**Purpose:** Define layout behaviour at different viewport sizes.

### EMIS-X Breakpoints

| Breakpoint | Width | Target |
|-----------|-------|--------|
| Mobile | < 768px | Not supported for clinical apps (desktop only for NHS) |
| Tablet | 768–1024px | Acceptable — test at 768px minimum |
| Desktop | > 1024px | Primary target |
| Wide | > 1440px | Ensure content doesn't stretch infinitely |

### Responsive Patterns

**Tables on tablet:** stack to card view or allow horizontal scroll with sticky first column.

**Multi-column forms:** collapse to single-column at ≤ 768px.

**Navigation:** sidebar collapses to hamburger at ≤ 768px (handled by host app — do not implement).

**Content max-width:** cap at `1200px` and centre with `margin: 0 auto` to prevent infinite stretch on wide screens.

### Responsive Design Template

```markdown
### Responsive Design: {FeatureName}

| Component | Desktop (>1024px) | Tablet (768-1024px) |
|-----------|------------------|---------------------|
| {Table} | Full columns | Scroll horizontally |
| {Form} | 2 columns | 1 column |
| {Cards} | Grid 3 per row | Grid 2 per row |
```
