# SKILL: design-system-integration
# Phase: P05 Product Experience Design — Phase 11

## Design System Integration

**Purpose:** Final validation that all component specifications conform to the EMIS design system.

### Integration Checklist

Before writing to REQ files, verify:

- [ ] All interactive elements use `@emisgroup/ui-*` components (see `emis-ui-kit-baseline`)
- [ ] All colours use `var(--token-name)` — no hex/rgb values anywhere
- [ ] All user-facing text uses `t('Namespace.Key')` translation keys
- [ ] All components have `ComponentName.displayName = 'ComponentName'`
- [ ] All components have a corresponding axe-core accessibility test specified
- [ ] All forms use `<fieldset>` and `<legend>` for groups
- [ ] All icons use Iconify format (`mdi:icon-name`, `emis:icon-name`)

### Translation Key Inventory

For each new screen, document the new translation keys needed in the P05 output:

```markdown
### New Translation Keys

| Key | Value |
|-----|-------|
| {Feature}.{Screen}.Title | "{Localised title}" |
| {Feature}.{Screen}.{Action} | "{Localised action}" |
| {Feature}.{Screen}.EmptyState | "{Localised empty message}" |
| {Feature}.{Screen}.Error.{Scenario} | "{Localised error message}" |
```

### Integration Template

```markdown
### Design System Integration

**EMIS UI Kit components used:**
- {List of @emisgroup/ui-* components with their source package}

**Design tokens used:**
- {List of var(--token-name) values}

**Translation keys to add:**
- {List of new t() keys}

**Iconify icons used:**
- {List of icon names}
```
