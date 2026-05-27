---
name: emis-x-webapp-accessibility
description: Accessibility guardrails and steers for EMIS-X microfrontend applications covering WCAG 2.1 AA compliance including keyboard navigation, ARIA attributes, focus management, form accessibility, dialog patterns, non-text content, status messages, colour contrast, heading structure, and automated testing with axe-core. This skill should be used when creating or modifying UI components, performing accessibility audits, or when users ask about accessibility requirements. Rules are prefixed A11Y and must be satisfied by all generated code.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-webapp
    - requirements
---

# EMIS-X Webapp Accessibility Guardrails and Steers

This skill defines mandatory accessibility guardrails and steers for EMIS-X microfrontend applications. All generated code **must** satisfy every applicable rule. The target standard is **WCAG 2.1 Level AA**.

**Target versions:** React 18.3+, `jest-axe` for automated testing, `@emisgroup/ui-*` design system components (which provide baseline accessibility).

## Rules Index

| ID        | Name                          | Type      | Severity | WCAG |
| --------- | ----------------------------- | --------- | -------- | ---- |
| A11Y-001  | Keyboard Navigation           | Guardrail | High     | 2.1.1 |
| A11Y-002  | Focus Management              | Guardrail | High     | 2.4.7 |
| A11Y-003  | Dialog & Modal Accessibility  | Steer     | High     | 4.1.2 |
| A11Y-004  | Form Accessibility            | Steer     | High     | 1.3.1, 3.3.1, 3.3.2 |
| A11Y-004a | Form Label Detection          | Guardrail | High     | 1.3.1 |
| A11Y-005  | ARIA Patterns                 | Steer     | High     | 1.3.1, 4.1.2 |
| A11Y-005a | ARIA Attribute Validation     | Guardrail | High     | 4.1.2 |
| A11Y-006  | Non-text Content              | Steer     | High     | 1.1.1 |
| A11Y-006a | Image Alt Text Detection      | Guardrail | High     | 1.1.1 |
| A11Y-007  | Status Messages               | Steer     | Medium   | 4.1.3 |
| A11Y-007a | Status Announcement Detection | Guardrail | Medium   | 4.1.3 |
| A11Y-008  | Colour & Contrast             | Steer     | High     | 1.4.1, 1.4.3 |
| A11Y-009  | Structure & Headings          | Guardrail | Medium   | 1.3.1, 2.4.6 |
| A11Y-010  | Automated Testing             | Guardrail | High     | — |

---

## A11Y-001: Keyboard Navigation (WCAG 2.1.1)

**Type:** Guardrail

**Requirement:** All interactive elements must be operable via keyboard. All `onClick` handlers on non-interactive elements must have corresponding keyboard support (`onKeyDown` for Enter/Space) and `tabIndex`.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```typescript
const handleKeyDown = (e: React.KeyboardEvent) => {
  if (e.key === 'Enter' || e.key === ' ') {
    e.preventDefault();
    handleAction();
  }
};
```

### Verification Checks

- All `onClick` on `<th>`, `<div>`, `<span>` must have `tabIndex` + `onKeyDown`
- No positive `tabIndex` values (use `tabIndex={0}` or `tabIndex={-1}` only)
- Custom clickable elements must have `role` + keyboard handlers
- Tab order must be logical

---

## A11Y-002: Focus Management (WCAG 2.4.7)

**Type:** Guardrail

**Requirement:** Focus indicators must always be visible. Never remove `outline` without providing an accessible alternative using `focus-visible`.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```scss
button:focus-visible {
  outline: 2px solid var(--focus-ring-color);
  outline-offset: 2px;
}
```

❌ **Bad:**

```scss
button:focus {
  outline: none;  // ❌ NO — breaks keyboard navigation
}
```

### Verification Checks

- No `outline: none` or `outline: 0` without accessible alternative
- `focus-visible` used for custom focus styles

---

## A11Y-003: Dialog & Modal Accessibility (WCAG 4.1.2)

**Type:** Steer

**Requirement:** All dialogs must have `role="dialog"`, `aria-modal="true"`, and `aria-labelledby` pointing to the dialog title. Focus must be trapped inside open dialogs. Use the EMIS-X `<Dialog>` component for compliance.

**Severity:** High

**Exceptions:** None.

**Evidence Required:** List all dialogs/modals in the component. For each, confirm `role="dialog"`, `aria-modal="true"`, and `aria-labelledby` are present (or that the EMIS-X `<Dialog>` component is used). State how focus trapping is implemented and confirm no incorrect roles exist on overlay backdrops.

### Verification Checks

- All dialogs have `role="dialog"`
- All modals have `aria-modal="true"`
- All dialogs have `aria-labelledby`
- No incorrect roles on overlay backdrops (no `role="button"` on backdrop divs)
- Focus trapped inside open dialogs

---

## A11Y-004: Form Accessibility (WCAG 1.3.1, 3.3.1, 3.3.2)

**Type:** Steer

**Requirement:** All form inputs must have programmatic labels. Error messages must use live regions. Error fields must be marked invalid. Required fields must be indicated programmatically.

**Severity:** High

**Exceptions:** None.

**Evidence Required:** List all form inputs and confirm each has a programmatic label (`<label>`, `aria-label`, or `aria-labelledby`). For inputs with error states, confirm `aria-invalid="true"` is set, error messages use `role="alert"` or `aria-live`, and errors are linked via `aria-describedby`. Confirm required fields use `aria-required="true"` or the `required` attribute.

### Verification Checks

- All form inputs have `<label>`, `aria-label`, or `aria-labelledby`
- Error messages use `role="alert"` or `aria-live`
- Error fields have `aria-invalid="true"`
- Error messages linked via `aria-describedby`
- Required fields have `aria-required="true"` or `required` attribute

✅ **Good:**

```typescript
<input aria-invalid={hasError} />
<div role="alert" aria-live="assertive">{error}</div>
```

---

## A11Y-004a: Form Label Detection (WCAG 1.3.1)

**Type:** Guardrail

**Requirement:** Detect `<input>`, `<select>`, and `<textarea>` elements that lack a programmatic label (`aria-label`, `aria-labelledby`, or an associated `<label>`).

**Severity:** High

**Exceptions:** Hidden inputs (`type="hidden"`) and submit buttons (`type="submit"`).

### Forbidden Patterns

- `<input>` without `aria-label`, `aria-labelledby`, or `id` matching a `<label htmlFor>`
- `<select>` or `<textarea>` without a label association

---

## A11Y-005: ARIA Patterns (WCAG 1.3.1, 4.1.2)

**Type:** Steer

**Requirement:** Use correct ARIA roles and states for interactive patterns. Icon-only buttons must have `aria-label`. Toggle elements must have `aria-expanded` or `aria-pressed`.

**Severity:** High

**Exceptions:** None.

**Evidence Required:** List all interactive patterns in the component (tabs, toggles, expandable sections, icon-only buttons). For each, state the ARIA roles and states applied and confirm they match the expected pattern (e.g., `role="tablist"`/`role="tab"`/`aria-selected` for tabs, `aria-expanded` for collapsibles, `aria-pressed` for toggles, `aria-label` for icon-only buttons).

### Verification Checks

- Tab patterns use `role="tablist"` / `role="tab"` / `aria-selected`
- Icon-only buttons have `aria-label`
- Generic action buttons ("Remove", "×", "Close") have contextual `aria-label`
- Expandable/collapsible elements have `aria-expanded`
- Toggle buttons have `aria-pressed`

✅ **Good:**

```typescript
<button aria-label="Close">×</button>
<button aria-pressed={isPressed}>Toggle</button>
```

---

## A11Y-005a: ARIA Attribute Validation (WCAG 4.1.2)

**Type:** Guardrail

**Requirement:** Detect invalid ARIA attribute names (misspelt or non-existent `aria-*` attributes) and `role` attributes with values that are not valid WAI-ARIA roles.

**Severity:** High

**Exceptions:** None.

### Forbidden Patterns

- `aria-labelled` (should be `aria-labelledby`)
- `aria-role` (not a valid attribute — use `role` instead)
- `role="link"` on a non-`<a>` element without `href` (use `<a>` instead)
- Any `aria-*` attribute not in the WAI-ARIA specification

---

## A11Y-006: Non-text Content (WCAG 1.1.1)

**Type:** Steer

**Requirement:** All `<img>` elements must have `alt` text (or `alt=""` for decorative images). Decorative icons must have `aria-hidden="true"`. Audio/media controls must have accessible names.

**Severity:** High

**Exceptions:** None.

**Evidence Required:** List all images and icons in the component. For each, state whether it is informative (has descriptive `alt` text) or decorative (has `alt=""` or `aria-hidden="true"`). Confirm no `<img>` elements are missing `alt` attributes and no informative icons lack accessible names.

✅ **Good:**

```typescript
<img alt="Patient photo" src={url} />
<img alt="" src={decorative} />  {/* Decorative */}
<Add size={20} aria-hidden="true" />  {/* Icon inside labelled button */}
```

---

## A11Y-006a: Image Alt Text Detection (WCAG 1.1.1)

**Type:** Guardrail

**Requirement:** All `<img>` elements must have an `alt` attribute. Decorative images must use `alt=""`. This is the deterministically testable subset of A11Y-006.

**Severity:** High

**Exceptions:** None.

### Forbidden Patterns

- `<img src="...">` without an `alt` attribute
- `<img src="..." />` without an `alt` attribute

---

## A11Y-007: Status Messages (WCAG 4.1.3)

**Type:** Steer

**Requirement:** Dynamic status updates must use `role="status"` or `aria-live="polite"`. Loading states must be announced to screen readers.

**Severity:** Medium

**Exceptions:** None.

**Evidence Required:** Identify all dynamic status updates in the component (success/error notifications, loading states, save confirmations). State how each is announced to screen readers (`role="status"`, `aria-live`, or `<ProgressIndicator>`) and confirm no status changes occur silently.

### Verification Checks

- Save/status indicators use `role="status"` or `aria-live="polite"`
- Loading components (`<ProgressIndicator>`) have `aria-live` or `role="status"`

---

## A11Y-007a: Status Announcement Detection (WCAG 4.1.3)

**Type:** Guardrail — deterministic subset of A11Y-007

**Requirement:** Components with loading/status rendering patterns (`isLoading`, `isError`, `isFetching`, `isSubmitting`, `isSaving`) must include ARIA live-region attributes (`role="status"`, `role="alert"`, `aria-live`, `aria-busy`) or use accessible components (`<ProgressIndicator>`, `<Spinner>`).

**Severity:** Medium

**Exceptions:** None.

---

## A11Y-008: Colour & Contrast (WCAG 1.4.1, 1.4.3)

**Type:** Steer

**Requirement:** Information must not be conveyed by colour alone — include text or icons alongside colour. All colours must use design tokens (which guarantee WCAG AA contrast). No hardcoded inline colours with potential contrast issues.

**Severity:** High

**Exceptions:** None.

**Evidence Required:** Identify all instances where colour conveys meaning (status indicators, error states, severity levels, badges). Confirm each has a non-colour indicator (text label, icon, or pattern) alongside the colour. Confirm all colour values use design tokens (cross-reference DS-002).

### Verification Checks

- Status badges/pills include text or icons alongside colour
- No inline `style={{ color: }}` with low-contrast combinations (light greys, etc.)
- All colours use design tokens (see DS-002)

---

## A11Y-009: Structure & Headings (WCAG 1.3.1, 2.4.6)

**Type:** Guardrail

**Requirement:** Heading levels must not skip (e.g., `<h1>` then `<h3>` with no `<h2>`). Labels must use proper elements, not styled `<div>` elements.

**Severity:** Medium

**Exceptions:** None.

### Verification Checks

- No heading level skips (`<h1>` → `<h3>` without `<h2>`)
- No `<div className={...label...}>` adjacent to form inputs — use proper labels

---

## A11Y-010: Automated Testing

**Type:** Guardrail

**Requirement:** All components must include axe-core accessibility tests using `jest-axe`. Run `toHaveNoViolations()` on rendered component output.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```typescript
import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);

it('has no accessibility violations', async () => {
  const { container } = render(<Component {...defaultProps} />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

---

## Workflow: Accessibility Remediation

Follow this step-by-step workflow when remediating accessibility issues.

**STEP 1: Keyboard Navigation**
```
□ All interactive elements accessible via Tab? [ ] YES [ ] NO
□ Tab order logical? [ ] YES [ ] NO
□ Focus indicators visible? [ ] YES [ ] NO
□ Escape key closes modals? [ ] YES [ ] NO
□ Enter/Space activates buttons? [ ] YES [ ] NO
```

**STEP 2: Screen Reader Support**
```
□ All images have alt text (or alt="" if decorative)? [ ] YES [ ] NO
□ Icon-only buttons have aria-label? [ ] YES [ ] NO
□ Form inputs have associated labels? [ ] YES [ ] NO
□ Error messages have role="alert"? [ ] YES [ ] NO
□ Loading states announced with aria-live? [ ] YES [ ] NO
```

**STEP 3: Color and Contrast**
```
□ ALL colors use design tokens? [ ] YES [] NO
  → Search for violations: grep -r "color: #\|background: #" src/
□ Information not conveyed by color alone? [ ] YES [ ] NO
  → Status indicators have icons + text
□ Text readable at 200% zoom? [ ] YES [ ] NO
```

**STEP 4: ARIA Attributes**
```
□ Semantic HTML used first (button, nav, main)? [ ] YES [ ] NO
□ ARIA added only when semantic HTML insufficient? [ ] YES [ ] NO
□ aria-expanded on toggleable elements? [ ] YES [ ] NO
□ aria-invalid on form inputs with errors? [ ] YES [ ] NO
□ aria-describedby links to help text/errors? [ ] YES [ ] NO
```

**STEP 5: Automated Testing**
```
□ Add axe-core tests if missing:
  import { axe, toHaveNoViolations } from 'jest-axe';
  const results = await axe(container);
  expect(results).toHaveNoViolations();
□ Run tests: pnpm test
□ Any violations? If YES, fix them
```

**STEP 6: Manual Testing**
```
□ Navigate entire feature with keyboard only
□ Test with screen reader (VoiceOver/NVDA)
□ Check color contrast with browser DevTools
□ Verify at 200% zoom
□ All pass? [ ] YES [ ] NO
```

---

## Gotchas

- `@emisgroup/ui-*` design system components provide baseline accessibility (focus rings, ARIA, keyboard handling) — but you must still supply correct props (e.g., `label` on `TextInput`, `aria-label` on icon-only buttons). The component handles _mechanics_, not _semantics_.
- `outline: none` or `outline: 0` on `:focus` completely removes the focus indicator — never do this. If a custom focus style is needed, use `:focus-visible` and ensure the replacement meets the 3:1 contrast ratio.
- `role="button"` on a `<div>` or `<span>` does NOT make it keyboard-operable — you must also add `tabIndex={0}` and `onKeyDown` handling for Enter and Space. Prefer using `<button>` instead.
- `aria-label` overrides **all** child text content for screen readers — use it on icon-only elements, not on elements that already have visible text. For additional descriptions, use `aria-describedby` instead.
- `autoFocus` works on initial render but fails silently when a component mounts inside an already-rendered view (e.g., conditional renders, tab panels). Use a `useEffect` with `ref.current?.focus()` for reliable focus management.
- `jest-axe` catches ~30–40% of WCAG violations — it cannot detect keyboard traps, logical focus order, or meaningful alt text. Automated tests supplement manual testing, they do not replace it.
- Live regions (`aria-live="polite"`) must exist in the DOM **before** content is injected. If the container itself is conditionally rendered, the announcement is missed. Render the container always and update its content.
