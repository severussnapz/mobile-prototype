# SKILL: visual-design
# Phase: P05 Product Experience Design — Phase 7

## Visual Design Specifications

**Purpose:** Define spacing, typography, and token usage for the feature.

### Token-Based Colour Mapping

Never specify hex colours. Map all colours to design tokens:

```markdown
| Element | Token |
|---------|-------|
| Primary action | var(--color-action-primary) |
| Secondary text | var(--color-text-secondary) |
| Error state | var(--color-feedback-error) |
| Success state | var(--color-feedback-success) |
| Warning state | var(--color-feedback-warning) |
| Background | var(--color-background-primary) |
| Border | var(--color-border-default) |
```

### Spacing Scale

Use 4px base grid. Specify spacing in multiples of 4:

```scss
// Padding/margin values
$spacing-xs: 4px;
$spacing-sm: 8px;
$spacing-md: 16px;
$spacing-lg: 24px;
$spacing-xl: 32px;
$spacing-2xl: 48px;
```

### Typography

- Headings: defined by EMIS design system typography tokens — do not override
- Body text: `var(--font-size-body)` / `var(--font-family-base)`
- Code/monospace: only for technical display (IDs, codes)

### Visual Design Template

```markdown
### Visual Design: {FeatureName}

**Spacing:** {margin/padding values between elements}
**Colour tokens:** {list of tokens used}
**Typography:** {any custom type treatments}
**Iconography:** {Iconify icons used, e.g. mdi:check, mdi:alert}
```
