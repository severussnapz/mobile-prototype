# SKILL: accessibility-requirements
# Phase: P05 Product Experience Design — Phase 5

## Accessibility Requirements

**Purpose:** Define WCAG 2.1 AA compliance requirements for this feature.

### Mandatory for Every Screen

**Keyboard navigation:**
- All interactive elements reachable by Tab
- Logical tab order (left-to-right, top-to-bottom)
- Visible focus indicators on all focusable elements
- No keyboard traps (Dialogs must handle Escape key)

**Screen reader support:**
- `aria-label` or `aria-labelledby` on all interactive elements without visible text labels
- Status updates use `aria-live="polite"` regions
- Error messages use `role="alert"`
- Form groups use `fieldset` and `legend`

**Colour and contrast:**
- Text contrast ≥ 4.5:1 (normal), ≥ 3:1 (large, ≥18pt or ≥14pt bold)
- Never convey information by colour alone — always add text or icon

**Touch targets:**
- Minimum 44×44 px for all interactive elements

### Screen-Specific Accessibility Requirements

For each screen, specify:

```markdown
### Accessibility: {ScreenName}

**Focus management:**
- {On modal open: focus moves to dialog title}
- {On modal close: focus returns to trigger element}

**ARIA requirements:**
- {list aria attributes}

**Keyboard shortcuts:**
- {Escape: close dialog / cancel action}

**Screen reader announcements:**
- {List live region content}
```

### Axe-Core Test Requirement

All components MUST have an automated axe-core accessibility test:

```typescript
it('has no accessibility violations', async () => {
    const { container } = render(<{ComponentName} {...props} />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
});
```
