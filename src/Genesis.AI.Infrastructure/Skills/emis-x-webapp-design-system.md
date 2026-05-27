---
name: emis-x-webapp-design-system
description: Design system guardrails and steers for EMIS-X microfrontend applications covering mandatory use of @emisgroup/ui-* components, Radix UI composition patterns, design tokens for colours, Iconify icon patterns, loading state components, and component API discovery. This skill should be used when creating or modifying UI components, working with design system elements, or when users ask about EMIS-X design system usage. Rules are prefixed DS and must be satisfied by all generated code.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-webapp
    - requirements
---

# EMIS-X Webapp Design System Guardrails and Steers

This skill defines mandatory design system guardrails and steers for EMIS-X microfrontend applications. All generated code **must** satisfy every applicable rule.

**Target versions:** React 18.3+, TypeScript 5.8+, `@emisgroup/ui-*` packages, `@emisgroup/design-tokens`.

**Key resources:**
- **TypeScript definitions:** `node_modules/@emisgroup/ui-*/dist/index.d.ts` (check FIRST — always accessible)
- **Documentation:** https://ui.emisgroup.uk/ (password-protected, for human developers)
- **Sandbox:** https://sandbox.ui.emisgroup.uk/ (password-protected)
- **Repository:** https://github.com/emisgroup/ui
- **Icon browser:** https://icon-sets.iconify.design/ic/

## Rules Index

| ID        | Name                            | Type      | Severity |
| --------- | ------------------------------- | --------- | -------- |
| DS-001    | Design System Components Required | Guardrail | High     |
| DS-002    | Design Tokens for Colours        | Guardrail | High     |
| DS-003    | Radix UI Composition Pattern     | Steer     | High     |
| DS-004    | No Third-Party Icon Libraries    | Guardrail | High     |
| DS-005    | Button Variant Names             | Guardrail | Medium   |
| DS-006    | Loading State Components         | Steer     | Medium   |
| DS-007    | Check TypeScript Definitions First | Steer   | High     |
| DS-008    | Responsive Design with Container Queries | Guardrail | High |
| DS-009    | Dialog Trigger Pattern         | Guardrail | High     |
| DS-010    | ACP Layout Variables           | Steer     | High     |
| DS-010a   | Viewport Unit Without ACP Variable Detection | Guardrail | High |
| DS-011    | Component Style Overrides        | Steer     | Medium   |
| DS-012    | Button Group for Adjacent Buttons | Guardrail | Medium   |

---

## DS-001: Design System Components Required

**Type:** Guardrail

**Requirement:** ALL interactive and visual UI elements must use `@emisgroup/ui-*` components. Native HTML is only permitted for semantic structure. Install dependencies with `pnpm add` before writing imports.

**Severity:** High

**Exceptions:** None.

### Permitted Native HTML (Semantic Structure Only)

`<div>`, `<section>`, `<article>`, `<main>`, `<header>`, `<footer>`, `<nav>`, `<aside>`, `<p>`, `<span>`, `<h1>`–`<h6>`, `<ul>`, `<ol>`, `<li>`, `<dl>`, `<dt>`, `<dd>`, `<figure>`, `<figcaption>`, `<time>`, `<code>`, `<pre>`, `<blockquote>`, `<hr>`, `<br>`

### Forbidden Native HTML (Use Design System Instead)

ANY element with interaction or visual styling: `<button>`, `<input>`, `<select>`, `<textarea>`, `<table>`, `<tr>`, `<td>`, `<th>`, `<dialog>`, `<a>` (for buttons/actions), `<fieldset>`, `<legend>`, `<form>` controls, etc.

### Available Component Categories

- **Forms:** Input, Textarea, Checkbox, Radio, Switch, DatePicker
- **Data Display:** Table, Card, Badge, Avatar, Tooltip
- **Navigation:** Tabs, Breadcrumbs, Pagination, Link
- **Feedback:** Alert, Toast, ProgressIndicator, Skeleton
- **Overlay:** Dialog, Dropdown, Popover, Sheet
- **Layout:** Container, Grid, Stack
- **Actions:** Button, IconButton

### Component-to-Package Mapping

| Component | Package |
| --------- | ------- |
| Button, IconButton | `@emisgroup/ui-button` |
| Input, Textarea | `@emisgroup/ui-input` |
| Checkbox | `@emisgroup/ui-checkbox` |
| Radio | `@emisgroup/ui-radio` |
| Switch | `@emisgroup/ui-switch` |
| Dropdown | `@emisgroup/ui-dropdown` |
| Table | `@emisgroup/ui-table` |
| Dialog | `@emisgroup/ui-dialog` |
| Card | `@emisgroup/ui-card` |
| Badge | `@emisgroup/ui-badge` |
| Skeleton | `@emisgroup/ui-skeleton` |
| ProgressIndicator | `@emisgroup/ui-progress-indicator` |

### Installation Pattern

```bash
pnpm add @emisgroup/ui-table @emisgroup/ui-button @emisgroup/ui-card
```

### Import Pattern

```typescript
import { Table } from '@emisgroup/ui-table';
import { Button } from '@emisgroup/ui-button';
import { Card } from '@emisgroup/ui-card';
```

### Common Mistakes

| ❌ Wrong | ✅ Correct |
|---------|-----------|
| `<button onClick={...}>Click</button>` | `<Button onClick={...}>Click</Button>` from `@emisgroup/ui-button` |
| `<input type="text" />` | `<Input />` from `@emisgroup/ui-input` |
| `<select><option>X</option></select>` | `<Dropdown><Dropdown.Item value="x">X</Dropdown.Item></Dropdown>` |
| `<textarea />` | `<Textarea />` from `@emisgroup/ui-input` |
| `<input type="checkbox" />` | `<Checkbox>Label</Checkbox>` from `@emisgroup/ui-checkbox` |
| `<input type="radio" />` | `<Radio>Label</Radio>` from `@emisgroup/ui-radio` |
| `<table><tr><td>...</td></tr></table>` | `<Table><Table.Body><Table.Row><Table.Cell>...</Table.Cell>...` |
| `<dialog>...</dialog>` | `<Dialog><Dialog.Trigger>...<Dialog.Inner>...</Dialog.Inner></Dialog>` |
| `<a href="#" onClick={...}>Action</a>` | `<Button variant="borderless" onClick={...}>Action</Button>` |
| `<ul><li>Tab 1</li></ul>` for tabs | `<Tabs><Tabs.List><Tabs.Trigger>Tab 1</Tabs.Trigger>...` |
| `<div class="card">...</div>` | `<Card>...</Card>` from `@emisgroup/ui-card` |
| `<span class="badge">New</span>` | `<Badge>New</Badge>` from `@emisgroup/ui-badge` |
| `"Loading..."` text | `<Skeleton />` or `<ProgressIndicator />` |
| Write imports before install | `pnpm add package-name` THEN write imports |
| `claims.name` | `claims.givenName + ' ' + claims.familyName` |

---

## DS-002: Design Tokens for Colours

**Type:** Guardrail

**Requirement:** ALL colours must use design tokens via `var(--token-name)`. Custom hex or RGB values break theming, dark mode, and accessibility. Raw pixel values for spacing are acceptable.

**Severity:** High

**Exceptions:** None.

### Colour Token System

The EMIS-X design token system uses **semantic colour families**, each with
modifier suffixes for shade variants. Prefer the **base** (unsuffixed) token
unless a specific shade is required.

#### Semantic Colours

Each family has these modifiers: `-bright`, `-light`, base (no suffix), `-dim`,
`-dark`, `-inverse`.

| Intent / User language | Token family | Background example | Text-on-background |
|------------------------|-------------|-------------------|--------------------|
| Blue, primary, info | `--primary` | `var(--primary)` | `var(--primary-bright)` |
| Green, success, positive, complete | `--positive` | `var(--positive)` | `var(--positive-bright)` |
| Red, error, danger, destructive | `--negative` | `var(--negative)` | `var(--negative-bright)` |
| Yellow, warning, caution | `--cautionary` | `var(--cautionary)` | `var(--cautionary-dark)` |
| Grey, neutral, inactive, disabled | `--neutral` | `var(--neutral)` | `var(--neutral-bright)` |

> ⚠️ The warning family is `--cautionary`, **not** `--warning`. There is no
> `--warning` token.

#### Non-Colour Semantics

| Token | Purpose |
|-------|--------|
| `--text` / `--text-bright` / `--text-dim` / `--text-dark` | Text colours |
| `--link` / `--link-dim` | Link colours |
| `--background` / `--background-dim` | Surface backgrounds |
| `--border` / `--border-light` | Border colours |
| `--accent` | Brand accent |

#### Tagged Colours

Use for categorical/visual distinction (e.g. chart series, user-assigned tags):

`--accent-1`, `--accent-2`, `--accent-3`, `--hover`, `--selected`, `--urgent`,
`--white`, `--black`

#### Decorative Colours

Use for illustrative/decorative purposes only — never for semantic meaning:

`--deco-aubergine`, `--deco-kobi`, `--deco-rainforest`, `--deco-vermillion`,
`--deco-pacific`, `--deco-champagne`, `--deco-wasabi`, `--deco-razzmatazz`,
`--deco-honey`, `--deco-umber`, `--deco-turbo`

#### Raw Palette Tokens (Avoid)

Tokens like `--positive-50-base`, `--negative-30`, `--neutral-99` are **raw
palette tokens** from the design-token scale. Prefer the semantic family tokens
(e.g. `--positive` instead of `--positive-50-base`) because semantic tokens
respond correctly to theme changes and dark mode. Use raw palette tokens only
when no semantic token provides the required shade.

### SCSS Pattern

```scss
@use '~@emisgroup/design-tokens/build/scss/variables';

.myComponent {
  // ✅ Use design tokens for colours
  color: var(--text-primary);
  background: var(--surface-background);

  // ✅ Raw pixel values for spacing OK
  padding: 16px;
  margin-bottom: 32px;

  // ✅ Design tokens for typography
  font-size: var(--font-size-body);
}
```

### CSS Pattern

```css
.my-element {
  color: var(--text-primary);
  background-color: var(--surface-background);
  padding: var(--spacing-md);
  border-radius: var(--radius-sm);
}
```

❌ **Bad:**

```scss
.bad {
  color: #ff0000;              // ❌ Custom hex
  background: rgb(0, 100, 200); // ❌ Custom RGB
}
```

### Translating Colour Requests

When a user asks to "make it green" or "use a red background", map the
request to the correct semantic token family — do not guess hex values or use
raw palette tokens.

| User says | Use | Not |
|-----------|-----|-----|
| "green", "success", "positive", "complete" | `--positive` | `--positive-50-base`, `#4B9640` |
| "red", "error", "danger", "destructive" | `--negative` | `--negative-50-base`, `#dc3545` |
| "yellow", "warning", "caution" | `--cautionary` | `--warning`, `#ffc107` |
| "blue", "primary", "info" | `--primary` | `#3b78bd` |
| "grey", "disabled", "inactive" | `--neutral` | `#949494` |

### Focus Indicator Tokens

```scss
button:focus-visible {
  outline: 2px solid var(--focus-ring-color);
  outline-offset: 2px;
}
```

### Common Token Mistakes

| ❌ Wrong | ✅ Correct |
|---------|-----------|
| `color: #ff0000` | `color: var(--text-error)` |
| `var(--warning)` | `var(--cautionary)` |
| `var(--positive-50-base)` (raw palette) | `var(--positive)` (semantic) |
| `variant="primary"` | `variant="filled"` |
| `variant="secondary"` | `variant="mono"` or `variant="borderless"` |

---

## DS-003: Radix UI Composition Pattern

**Type:** Steer

**Requirement:** Radix UI-based components (Dropdown, Dialog, Select) use composition with subcomponents. Use the correct subcomponent names — never mix with native HTML children.

**Severity:** High

**Exceptions:** None.

**Evidence Required:** State which Radix-based components were used and confirm subcomponent names were verified against TypeScript definitions (`node_modules/@emisgroup/ui-*/dist/index.d.ts`). List the subcomponents used (e.g., `Dialog.Content`, `Dialog.Header`, `Dialog.Footer`) and confirm no non-existent subcomponents (e.g., `Dialog.Body`) or native HTML children (e.g., `<option>` inside `<Dropdown>`) were used.

### Dropdown Pattern

```typescript
import { Dropdown } from '@emisgroup/ui-dropdown';

<Dropdown value={value} onValueChange={setValue}>
  <Dropdown.Trigger placeholder="Select option">
    {displayValue}
  </Dropdown.Trigger>
  <Dropdown.Content>
    <Dropdown.Item value="option1">Option 1</Dropdown.Item>
    <Dropdown.Item value="option2">Option 2</Dropdown.Item>
  </Dropdown.Content>
</Dropdown>
```

❌ **Wrong:** `<Dropdown><option value="x">X</option></Dropdown>` (NOT HTML `<option>`)

### Dialog Pattern

Dialogs **must** use `Dialog.Trigger` to control visibility and `Dialog.Inner` for the dialog surface. Never conditionally render the `<Dialog>` wrapper — the component manages its own visibility via the `open` prop.

```typescript
import { Dialog, DialogTitle } from '@emisgroup/ui-dialog';
import { Button } from '@emisgroup/ui-button';

const [open, setOpen] = useState(false);

<Dialog open={open} onOpenChange={setOpen}>
  <Dialog.Trigger>
    <Button type="button">Open dialog</Button>
  </Dialog.Trigger>
  <Dialog.Inner aria-label="example">
    <Dialog.Header>
      <DialogTitle>Dialog title</DialogTitle>
    </Dialog.Header>
    <Dialog.Content>
      <p>Dialog body content goes here.</p>
    </Dialog.Content>
    <Dialog.Footer>
      <Button.Group>
        <Button borderless onClick={() => setOpen(false)} type="button">
          Cancel
        </Button>
        <Button onClick={() => setOpen(false)} type="button" variant="filled">
          Save
        </Button>
      </Button.Group>
    </Dialog.Footer>
  </Dialog.Inner>
</Dialog>
```

✅ Available subcomponents: `Dialog.Trigger`, `Dialog.Inner`, `Dialog.Header`, `Dialog.Content`, `Dialog.Footer`, `DialogTitle`
❌ Does NOT exist: `Dialog.Body`
✅ Must use `Dialog.Trigger` — never conditionally render the `<Dialog>` wrapper
✅ Title must use `<DialogTitle>` inside `Dialog.Header`
✅ `Dialog.Inner` must have `aria-label`

❌ **Anti-pattern — always-visible dialog:**

```typescript
// ❌ WRONG — Dialog is always rendered and visible; no Trigger to show/hide it
{isOpen && (
  <Dialog open={isOpen} onOpenChange={setIsOpen}>
    <Dialog.Content>...</Dialog.Content>
  </Dialog>
)}
```

### Form Components

❌ **Never use native HTML:**

```typescript
<input type="text" />         // ❌ NO
<textarea />                  // ❌ NO
<input type="checkbox" />     // ❌ NO
<select><option /></select>   // ❌ NO
```

✅ **Always use design system:**

```typescript
import { Input, Textarea } from '@emisgroup/ui-input';
import { Checkbox } from '@emisgroup/ui-checkbox';
import { Dropdown } from '@emisgroup/ui-dropdown';

<Input type="text" />
<Textarea />
<Checkbox>Label text</Checkbox>
<Dropdown><Dropdown.Item value="x">X</Dropdown.Item></Dropdown>
```

---

## DS-004: No Third-Party Icon Libraries

**Type:** Guardrail

**Requirement:** All icons must use the EMIS-X Iconify pattern (`~icons/ic/outline-*`). Never install or import `lucide-react`, `react-icons`, or other third-party icon libraries.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```typescript
import Archive from '~icons/ic/outline-archive';
import Edit from '~icons/ic/outline-edit';
import Add from '~icons/ic/outline-add';
import Delete from '~icons/ic/outline-delete';
import Search from '~icons/ic/outline-search';

<Button>
  <Add size={20} aria-hidden="true" />
  Add New
</Button>
```

❌ **Bad:**

```typescript
import { Archive } from 'lucide-react';  // ❌ NO
import { FaArchive } from 'react-icons'; // ❌ NO
```

**Browse icons:** https://icon-sets.iconify.design/ic/

---

## DS-005: Button Variant Names

**Type:** Guardrail

**Requirement:** Use only the valid button variant names. Check `node_modules/@emisgroup/ui-button/dist/index.d.ts` for the current list.

**Severity:** Medium

**Exceptions:** None.

✅ **Valid variants:**

- `"mono"` — Subtle/tertiary actions
- `"inverted"` — Inverted colour scheme
- `"filled"` — Primary actions
- `"filled-inverted"` — Primary inverted
- `"danger"` — Destructive actions
- `"warning"` — Warning actions
- `"success"` — Success actions
- `"borderless"` — Text-only buttons

❌ **Never use:** `"primary"`, `"secondary"`, `"tertiary"` (these don't exist)

---

## DS-006: Loading State Components

**Type:** Steer

**Requirement:** Use appropriate design system components for loading states. Never use plain text like `"Loading..."`.

**Severity:** Medium

**Exceptions:** None.

**Evidence Required:** For each loading state, state which component was chosen (`<Skeleton>` for content with known layout, `<ProgressIndicator>` for actions/spinners) and why it suits the use case. Confirm no plain text loading indicators (e.g., `"Loading..."`) are used.

### Skeleton — For Content with Known Layout

```typescript
import { Skeleton } from '@emisgroup/ui-skeleton';
import { Table } from '@emisgroup/ui-table';

// Table loading skeleton
const TableSkeleton = () => (
  <Table stripedRows>
    <thead>
      <tr>
        <th scope="col">Name</th>
        <th scope="col">Date</th>
      </tr>
    </thead>
    <tbody>
      {[...Array(4)].map((_, index) => (
        <tr key={`skeleton-${index}`}>
          <td><Skeleton.Text style={{ width: '80%' }} /></td>
          <td><Skeleton.Text style={{ width: '60%' }} /></td>
        </tr>
      ))}
    </tbody>
  </Table>
);

// Card loading
<Skeleton variant="card" lines={5} />

// List loading
<Skeleton variant="listitem" lines={3} />
```

**Skeleton variants:** `variant="card"`, `variant="image"`, `variant="listitem"`
**Subcomponents:** `Skeleton.Text`, `Skeleton.Item`, `Skeleton.Area`

### ProgressIndicator — For Actions/Spinners

```typescript
import { ProgressIndicator } from '@emisgroup/ui-progress-indicator';

// Full-page loading
<div style={{ display: 'flex', justifyContent: 'center', padding: '2rem' }}>
  <ProgressIndicator size="large" />
</div>

// Inline loading (button)
<Button disabled={loading}>
  {loading ? <ProgressIndicator size="small" /> : 'Submit'}
</Button>
```

✅ **Good:**

```tsx
// Skeleton for content with known layout
<Skeleton variant="card" lines={5} />

// ProgressIndicator for action/spinner
<ProgressIndicator size="large" />
```

❌ **Bad:**

```tsx
// Plain text loading indicator
{isLoading && <p>Loading...</p>}

// Custom spinner instead of design system component
{isLoading && <div className="spinner" />}
```

---

## DS-007: Check TypeScript Definitions First

**Type:** Steer

**Requirement:** Always check TypeScript type definitions before using any component API. Never guess prop names, variants, or subcomponents.

**Severity:** High

**Exceptions:** None.

**Evidence Required:** State which `@emisgroup/ui-*` component APIs were checked and confirm prop names, variants, and subcomponents were verified against TypeScript definitions before use.

### How to Discover Component APIs

1. **Check TypeScript types FIRST** (always accessible):
   ```bash
   cat node_modules/@emisgroup/ui-{component}/dist/index.d.ts
   ```
2. **Reference ui.emisgroup.uk** (password-protected, for human developers)
3. **Look for:**
   - Subcomponents (e.g., `Dropdown.Item`, `Dialog.Header`)
   - Valid prop values (e.g., `variant: "filled" | "mono" | ...`)
   - Required vs optional props
   - Example usage patterns

4. **Use TypeScript errors as guidance:**
   - "Type 'X' is not assignable to type 'A | B | C'" → Valid values are A, B, C
   - "Property 'X' does not exist" → Check type definition for actual properties

❌ **Never guess** prop names, variants, or subcomponents

---

## DS-008: Responsive Design with Container Queries

**Type:** Guardrail

**Requirement:** ALL responsive layout behaviour MUST use CSS container queries (`@container`) instead of media queries (`@media`). Breakpoint SCSS variables MUST be used to control content responsiveness. Media queries are forbidden in component SCSS because microfrontends do not control the viewport — they render inside a host application shell, and only container queries respond correctly to the actual space available to the component.

**Severity:** High

**Exceptions:** None.

### Breakpoints

| Variable | Width | Usage |
|----------|-------|-------|
| `$breakpoints-small` | 576px | Small devices, compact layouts |
| `$breakpoints-medium` | 768px | Tablets, medium panels |
| `$breakpoints-large` | 992px | Desktops, wide panels |
| `$breakpoints-x-large` | 1200px | Large desktops, full-width layouts |

Breakpoints MUST be used to control the responsiveness of content on the page.

### Container Setup

The parent element must declare a containment context:

```scss
.pageWrapper {
  container-type: inline-size;
  container-name: page;
}
```

### Container Query Pattern

✅ **Good:**

```scss
@use '~@emisgroup/design-tokens/build/scss/variables';

.grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 16px;

  @container page (min-width: $breakpoints-medium) {
    grid-template-columns: 1fr 1fr;
  }

  @container page (min-width: $breakpoints-large) {
    grid-template-columns: 1fr 1fr 1fr;
  }
}

.sidebar {
  display: none;

  @container page (min-width: $breakpoints-large) {
    display: block;
    width: 280px;
  }
}
```

❌ **Bad:**

```scss
// ❌ Media queries — do NOT use in microfrontend components
@media (min-width: 768px) {
  .grid {
    grid-template-columns: 1fr 1fr;
  }
}

// ❌ Hardcoded pixel values instead of breakpoint variables
@container page (min-width: 768px) {
  .grid {
    grid-template-columns: 1fr 1fr;
  }
}

// ❌ No container context defined on parent
.grid {
  @container (min-width: $breakpoints-medium) {
    grid-template-columns: 1fr 1fr;
  }
}
```

### Common Mistakes

| ❌ Wrong | ✅ Correct |
|---------|----------|
| `@media (min-width: 768px)` | `@container page (min-width: $breakpoints-medium)` |
| `@media (max-width: 576px)` | `@container page (max-width: $breakpoints-small)` |
| `@container (min-width: 768px)` with hardcoded value | `@container page (min-width: $breakpoints-medium)` with variable |
| No `container-type` on parent | Add `container-type: inline-size` on the containing element |
| Using viewport units (`100vw`) for responsive sizing | Use container-relative or percentage-based sizing |

---

## DS-009: Dialog Trigger Pattern

**Type:** Guardrail

**Requirement:** All `<Dialog>` implementations from `@emisgroup/ui-dialog` must use EITHER:
- **Pattern 1 (Direct Trigger):** `Dialog.Trigger` subcomponent — trigger and dialog in same component tree **(PREFERRED)**, OR
- **Pattern 2 (Programmatic Control):** `open={...}` prop with conditional rendering — trigger and dialog in separate component trees

All implementations must include `Dialog.Inner` with `aria-label`. Dialog title must use `<DialogTitle>` inside `Dialog.Header`.

**Severity:** High

**Exceptions:** None.

### Pattern Preference

**Default to Pattern 1 for all new implementations.** Pattern 1 (Dialog.Trigger) is simpler, has better encapsulation, and follows the design system's intended compositional model. Only use Pattern 2 when:
- Trigger and dialog components live in different parts of the component tree
- Architectural constraints prevent co-location
- Parent needs to orchestrate multiple dialogs from separate triggers

If you can refactor to co-locate trigger and dialog, prefer Pattern 1.

### Pattern 1: Dialog.Trigger (Direct Trigger) — PREFERRED

**Use when:** Trigger and dialog live in the same component (default choice for new implementations).

```typescript
<Dialog>
  <Dialog.Trigger>
    <Button type="button">Open</Button>
  </Dialog.Trigger>
  <Dialog.Inner aria-label="descriptive label">
    <Dialog.Header>
      <DialogTitle>Title</DialogTitle>
    </Dialog.Header>
    <Dialog.Content>...</Dialog.Content>
    <Dialog.Footer>
      <Button.Group>
        <Button borderless onClick={() => setOpen(false)} type="button">Cancel</Button>
        <Button onClick={() => setOpen(false)} type="button" variant="filled">Save</Button>
      </Button.Group>
    </Dialog.Footer>
  </Dialog.Inner>
</Dialog>
```

**Important:** Do NOT conditionally render the `<Dialog>` wrapper with this pattern — the component manages its own open/closed state.

### Pattern 2: Programmatic Control (Conditional Rendering) — FALLBACK

**Use when:** Trigger and dialog live in separate component trees (e.g., dropdown menu item opens dialog in parent) and refactoring to Pattern 1 is not feasible.

**Required props for Pattern 2:**
- `open={...}` — controls dialog visibility
- `onOpenChange={...}` — callback for dialog close events (Escape key, backdrop click, close button)

```typescript
// Parent component manages dialog state
const [activeDialog, setActiveDialog] = useState<"edit" | null>(null);

// Trigger (e.g., in menu)
<DropdownMenu.Item onClick={() => setActiveDialog("edit")}>
  Edit
</DropdownMenu.Item>

// Dialog (conditionally rendered)
{activeDialog === "edit" && (
  <EditDialog 
    onClose={() => setActiveDialog(null)} 
    onSuccess={handleSuccess}
  />
)}

// EditDialog.tsx
function EditDialog({ onClose, onSuccess }: EditDialogProps) {
  return (
    <Dialog open={true} onOpenChange={onClose}>
      <Dialog.Inner aria-label="Edit system">
        <Dialog.Header>
          <DialogTitle>Edit System</DialogTitle>
        </Dialog.Header>
        <Dialog.Content>...</Dialog.Content>
        <Dialog.Footer>
          <Button.Group>
            <Button borderless onClick={onClose} type="button">Cancel</Button>
            <Button onClick={handleSave} type="button" variant="filled">Save</Button>
          </Button.Group>
        </Dialog.Footer>
      </Dialog.Inner>
    </Dialog>
  );
}
```

**Key differences:**
- Uses `open={true}` prop (dialog always open when mounted)
- MUST include `onOpenChange` prop for proper keyboard/backdrop interactions
- Parent conditionally renders the entire dialog component
- `onOpenChange` prop maps to parent's close handler
- No `Dialog.Trigger` needed — trigger lives elsewhere in tree

### Forbidden Patterns

| ❌ Wrong | ✅ Correct |
|---------|----------|
| `<Dialog>` without `<Dialog.Trigger>` AND without `open={...}` | Use Pattern 1 (Dialog.Trigger) OR Pattern 2 (open prop) |
| `<Dialog open={...}>` without `onOpenChange={...}` (Pattern 2) | Add `onOpenChange` prop for keyboard/backdrop close events |
| `<Dialog>` without `<Dialog.Inner>` | Include `<Dialog.Inner aria-label="...">` |
| `<Dialog.Inner>` without `aria-label` | Add `aria-label` to `<Dialog.Inner>` |
| `{isOpen && <Dialog><Dialog.Trigger>...` (Pattern 1 + conditional render) | Remove conditional rendering — Pattern 1 manages own state |
| `<Dialog.Content>` as direct child of `<Dialog>` | Wrap in `<Dialog.Inner>` |
| `<Dialog.Header>Title</Dialog.Header>` | Use `<DialogTitle>` inside `<Dialog.Header>` |

---

## DS-010: ACP Layout Variables

**Type:** Steer

**Requirement:** Microfrontend applications must use ACP-provided CSS custom
properties to calculate available viewport space. The ACP host shell renders a
global navigation bar, an optional patient banner, a left-side navigation menu,
and a footer — all outside the microfrontend’s control. Hardcoding heights or
widths, or assuming the app occupies the full viewport, causes content to
overflow and display unnecessary scrollbars.

**Severity:** High

**Exceptions:** None.

**Evidence Required:** Confirm the app’s root layout container uses
`--acp-navapp-height` (or `--acp-navbar-height` where patient context is not
used) and `--acp-fixed-navbar-width` to calculate available space. Confirm no
hardcoded viewport height/width assumptions exist.

### Available Variables

| Variable | Description |
|----------|-------------|
| `--acp-navbar-height` | Height of the global navigation bar only |
| `--acp-navapp-height` | Height of the global navigation bar + patient banner (use this for patient-context apps) |
| `--acp-footer-height` | Height of the ACP footer (not currently rendered — include for forward-compatibility) |
| `--acp-fixed-navbar-width` | Width of the left-side navigation menu (always present) |

### Layout Pattern

```scss
.app-root {
  height: calc(100vh - var(--acp-navapp-height) - var(--acp-footer-height, 0px));
  width: calc(100vw - var(--acp-fixed-navbar-width));
  overflow: auto;
}
```

Use `--acp-navbar-height` instead of `--acp-navapp-height` only if the app does
not use patient context (no patient banner is displayed).

The fallback `var(--acp-footer-height, 0px)` ensures the layout works today
(footer not yet rendered) and adapts automatically when the footer is enabled.

✅ **Good:**

```scss
.app-root {
  height: calc(100vh - var(--acp-navapp-height) - var(--acp-footer-height, 0px));
  width: calc(100vw - var(--acp-fixed-navbar-width));
  overflow: auto;
}
```

❌ **Bad:**

```scss
.app-root {
  // Hardcoded pixel values — breaks when nav bar or banner changes
  height: calc(100vh - 64px);
  width: calc(100vw - 240px);

  // Assumes full viewport — ignores ACP shell chrome
  height: 100vh;
  width: 100vw;
}
```

---

## DS-010a: Viewport Unit Without ACP Variable Detection

**Type:** Guardrail — deterministic subset of DS-010

**Requirement:** SCSS/CSS files that use `100vh` must also reference the correct
ACP height variable in the same file. If the application declares
`patientContext: true` in `applicationDiscovery` (in `package.json`), the file
must use `--acp-navapp-height` (which includes the patient banner height);
`--acp-navbar-height` alone is not sufficient. When `patientContext` is `false`
or absent, either `--acp-navbar-height` or `--acp-navapp-height` is accepted.
Files that use `100vw` must also reference `--acp-fixed-navbar-width` in the
same file. This ensures the microfrontend accounts for the ACP host shell chrome
rather than assuming it occupies the full viewport.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```scss
.app-root {
  // 100vh compensated by ACP height variable
  height: calc(100vh - var(--acp-navapp-height) - var(--acp-footer-height, 0px));
  // 100vw compensated by ACP width variable
  width: calc(100vw - var(--acp-fixed-navbar-width));
}
```

❌ **Bad:**

```scss
.app-root {
  // 100vh without ACP variable — ignores nav bar and patient banner
  height: 100vh;
  // 100vw without ACP variable — ignores side navigation
  width: 100vw;
}
```

---

## DS-011: Component Style Overrides

**Type:** Steer

**Requirement:** When a design system component lacks a variant for the required
colour (e.g. Badge has no green/success variant), override its styles using a CSS
module class with an attribute selector to beat the component's `@layer
components` specificity. Always use semantic design tokens — never hex/rgb.

**Severity:** Medium

**Exceptions:** None.

**Evidence Required:** State which component was overridden, confirm the required
variant does not exist (checked TypeScript definitions), and confirm the override
uses semantic design tokens with an attribute selector for specificity.

### Why Overrides Need Specificity Bumps

`@emisgroup/ui-*` components inject their CSS into a `<style id="ui">` tag
inside `@layer components`. A plain CSS module class (single class selector) has
the same specificity as the component's internal class, so source order
determines the winner — and the component's injected styles typically win.

Adding an attribute selector (e.g. `[data-variant]`) bumps specificity above the
component's base class without resorting to `!important`.

### Pattern

```scss
// ✅ Override Badge to use green (positive) when no success variant exists
.completedBadge[data-variant] {
  background-color: var(--positive);
  color: var(--positive-bright);
}
```

```tsx
<Badge variant="primary" className={styles.completedBadge}>
  {t('Complete')}
</Badge>
```

The `variant` prop is still required (Badge requires it for its internal logic),
but the CSS override wins for visual styling.

### Common Mistakes

| ❌ Wrong | ✅ Correct |
|---------|----------|
| `.myOverride { background: var(--positive); }` (plain class — loses to component) | `.myOverride[data-variant] { background: var(--positive); }` (attribute selector wins) |
| `.myOverride { background: #4B9640; }` (hex value) | `.myOverride[data-variant] { background: var(--positive); }` (semantic token) |
| Using `!important` | Using attribute selector for specificity bump |
| Replacing the component with native HTML | Override the style; keep the component |

### Checklist Before Overriding

1. Confirm the required variant does not exist — check `node_modules/@emisgroup/ui-{component}/dist/index.d.ts`
2. Use a CSS module class with `[data-variant]` (or another `data-*` attribute the component renders)
3. Use semantic design tokens only — never hex/rgb
4. Set `variant` to the closest available variant (the component may use it for internal logic)
5. Verify the override works by running the app — `@layer` ordering can vary

---

## DS-012: Button Group for Adjacent Buttons

**Type:** Guardrail

**Requirement:** When two or more `<Button>` components appear side-by-side
(e.g. in dialog footers, form action bars, toolbars, or confirmation prompts),
they MUST be wrapped in `<Button.Group>` from `@emisgroup/ui-button`.
`Button.Group` renders a flex container with `gap: var(--spacing-small)` (0.5rem)
that provides consistent spacing, alignment, and wrapping. Never use manual
`flex`/`gap`/`margin` to space buttons — the design system component handles
this automatically and responds to design token changes.

**Severity:** Medium

**Exceptions:** A single button standing alone does not require `Button.Group`.

### Required Pattern

```typescript
import { Button } from '@emisgroup/ui-button';

// ✅ Two or more adjacent buttons — use Button.Group
<Button.Group>
  <Button borderless onClick={onCancel} type="button">
    Cancel
  </Button>
  <Button onClick={onSave} type="button" variant="filled">
    Save
  </Button>
</Button.Group>
```

### Dialog Footer Pattern

```typescript
import { Dialog, DialogTitle } from '@emisgroup/ui-dialog';
import { Button } from '@emisgroup/ui-button';

<Dialog.Footer>
  <Button.Group>
    <Button borderless onClick={() => setOpen(false)} type="button">
      Cancel
    </Button>
    <Button onClick={handleSave} type="button" variant="filled">
      Save
    </Button>
  </Button.Group>
</Dialog.Footer>
```

### Common Mistakes

| ❌ Wrong | ✅ Correct |
|---------|----------|
| `<Dialog.Footer><Button>A</Button><Button>B</Button></Dialog.Footer>` (no gap) | `<Dialog.Footer><Button.Group><Button>A</Button><Button>B</Button></Button.Group></Dialog.Footer>` |
| `<div style={{display:'flex', gap:'8px'}}><Button>A</Button><Button>B</Button></div>` (manual spacing) | `<Button.Group><Button>A</Button><Button>B</Button></Button.Group>` |
| `<div className={styles.buttonRow}><Button>A</Button><Button>B</Button></div>` (custom CSS class) | `<Button.Group><Button>A</Button><Button>B</Button></Button.Group>` |

---

## Gotchas

- `@emisgroup/ui-*` component APIs change between versions — **always** check `node_modules/@emisgroup/ui-{component}/dist/index.d.ts` before using any prop. The docs site (`ui.emisgroup.uk`) may lag behind the installed version.
- `variant="primary"` does not exist on `Button` — the correct values are `filled`, `outlined`, `ghost`, and `danger`. This is the single most common mistake.
- Hex and RGB colour values (`#FF0000`, `rgb(255,0,0)`) bypass theming, dark mode, and accessibility preferences. Always use design tokens: `var(--token-name)`. The Stylelint config catches this in SCSS but not in inline styles.
- The warning colour family is `--cautionary`, **not** `--warning`. There is no `--warning` token.
- Prefer semantic tokens (`--positive`) over raw palette tokens (`--positive-50-base`). Semantic tokens respond to theme changes and dark mode; raw palette tokens are fixed values.
- When overriding `@emisgroup/ui-*` component styles, a plain CSS module class has the same specificity as the component's internal class (both are single class selectors). The component's styles are injected into `@layer components` via a `<style>` tag, which typically wins by source order. Use an attribute selector (e.g. `.myClass[data-variant]`) to bump specificity above the component's base class.
- Native HTML elements (`<button>`, `<input>`, `<table>`, `<select>`) must never be used for interactive/visual UI — even if the design system component seems "heavier". Native HTML for semantic structure (`<div>`, `<section>`, `<p>`, `<h1>`–`<h6>`) is fine.
- Icons must use the Iconify pattern (`~icons/ic/outline-*`) — third-party icon libraries like `lucide-react`, `react-icons`, or `@heroicons` are forbidden because they don't match the EMIS-X visual language.
- `<Dialog>` manages its own open/close state internally via `<Dialog.Trigger>` — do NOT conditionally render `<Dialog>` with `{isOpen && <Dialog>}`. Use the `open` prop if you need programmatic control.
- Radix-based components (`Dialog`, `DropdownMenu`, `Popover`) use a compound component pattern (`Component.Trigger`, `Component.Content`, etc.). Missing a required subcomponent causes silent rendering failures, not TypeScript errors.
- `--acp-footer-height` is not currently set by the ACP host — always use a fallback: `var(--acp-footer-height, 0px)`. Without the fallback, `calc()` evaluates to an invalid value and the layout breaks.
- Use `--acp-navapp-height` (nav bar + patient banner) for patient-context apps, and `--acp-navbar-height` (nav bar only) for non-patient apps. Using the wrong variable causes the content area to be the wrong height.
- When placing two or more `<Button>` components next to each other (dialog footers, toolbars, form actions), always wrap them in `<Button.Group>`. Without it the buttons render flush against each other with no gap. `Button.Group` provides `gap: var(--spacing-small)` automatically.
