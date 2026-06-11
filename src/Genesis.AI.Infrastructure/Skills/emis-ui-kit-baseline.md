# SKILL: emis-ui-kit-baseline
# Phase: P05 Product Experience Design — Phase 0

## EMIS UI Kit Mandatory Baseline

These rules apply to EVERY component specification produced by P05. There are no exceptions.

### Non-Negotiable Component Mappings

| Prohibited pattern | Required EMIS component |
|-------------------|------------------------|
| `<button>` | `<Button>` from `@emisgroup/ui-button` |
| `<input type="text">` | `<Input>` from `@emisgroup/ui-input` |
| `<select>` / `<Listbox>` | `<Combobox>` from `@emisgroup/ui-combobox` |
| `<table>` / `<DataTable>` | `<Table>` from `@emisgroup/ui-table` |
| `<dialog>` / `<Modal>` | `<Dialog>` from `@emisgroup/ui-dialog` |
| Custom spinner | `<ProgressSpinner>` from `@emisgroup/ui-progress-indicator` |
| Custom accordion | `<Accordion>` from `@emisgroup/ui-accordion` |
| Custom badge | `<Badge>` from `@emisgroup/ui-badge` |
| Custom tabs | `<Tabs>` from `@emisgroup/ui-tabs` |
| Custom tag/chip | `<Tag>` from `@emisgroup/ui-tag` |
| Hardcoded banner | `<Banner>` from `@emisgroup/ui-banner` |

### Token Rules

- **All colours** MUST use `var(--token-name)`. Never hex (`#RRGGBB`) or RGB values.
- **All text** MUST use `t('Namespace.Key')` translation keys. No hardcoded strings.

### Component Structure Rules

- Every component MUST have `ComponentName.displayName = 'ComponentName'`
- Props interfaces MUST be `interface {ComponentName}Props { ... }` — named exports
- SCSS classes MUST be camelCase in `.module.scss` files

### Accessibility Mandate

Every interactive element MUST have:
- `aria-label` or `aria-labelledby`
- Keyboard navigability
- Minimum touch target: 44×44 px
- Colour contrast ratio: ≥ 4.5:1 for normal text, ≥ 3:1 for large text
