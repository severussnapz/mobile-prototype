# EMIS-X Design System — ui/ workspace reference

> Sourced from `/Users/luke.smith/git/ui/` — verified 2026-05-22

---

## Rule summary (non-negotiable guardrails)

- **DS-001:** `@emisgroup/ui-*` components only. No native `<button>`, `<input>`, `<select>`, `<dialog>`, `<textarea>`.
- **DS-002:** `var(--token-*)` CSS custom properties only. No hardcoded hex/rgb anywhere.
- **DS-004:** Iconify `~icons/ic/outline-*` (or `baseline-*`) only. NOT lucide-react / react-icons / @heroicons.
- **WCS-007a:** All UI strings via `useTranslation()` / `t()`. No raw string literals in JSX.
- **WCS-007b:** British English in all translation JSON values.
- **A11Y-004a:** Every `Input`, `Select`, `Textarea` equivalent needs `aria-label`, `aria-labelledby`, or `htmlFor`/`id` pair.
- **A11Y-007a:** Loading states need `role="status" aria-live="polite"`; error states need `role="alert" aria-live="assertive"`.
- **A11Y-010:** `jest-axe ^9.0.0` in `devDependencies`.

---

## Design tokens

Token file: `/Users/luke.smith/git/ui/packages/design-tokens/tokens.json`  
Project CSS stub (authoritative for this project): `/Users/luke.smith/Desktop/Archive 1/prototype/src/index.css`

### Colours
```
--token-colour-brand-primary:          #005EB8  (NHS Blue)
--token-colour-brand-primary-dark:     #003D78
--token-colour-brand-primary-light:    #D1E3F3

--token-colour-feedback-error-base:    #DA291C
--token-colour-feedback-error-light:   #FDECEA
--token-colour-feedback-warning-base:  #FFB81C
--token-colour-feedback-warning-light: #FFF6DE
--token-colour-feedback-success-base:  #007F3B
--token-colour-feedback-success-light: #E8F5EE
--token-colour-feedback-info-base:     #005EB8
--token-colour-feedback-info-light:    #D1E3F3

--token-colour-neutral-0:    #FFFFFF
--token-colour-neutral-50:   #F8FAFB  (page background)
--token-colour-neutral-100:  #F0F4F5  (row hover, table header)
--token-colour-neutral-200:  #D8DDE0  (row separator / borders)
--token-colour-neutral-400:  #ADB5BD
--token-colour-neutral-600:  #4C6272  (secondary/meta text)
--token-colour-neutral-800:  #2D3F4A
--token-colour-neutral-900:  #212B32  (primary text)
```

### Spacing (8px grid — token-spacing-N = N×4px)
```
--token-spacing-1:  4px
--token-spacing-2:  8px
--token-spacing-3:  12px
--token-spacing-4:  16px
--token-spacing-5:  20px
--token-spacing-6:  24px
--token-spacing-8:  32px
--token-spacing-10: 40px
--token-spacing-12: 48px
```

### Typography
```
--token-font-size-xs:   12px
--token-font-size-sm:   14px
--token-font-size-base: 16px
--token-font-size-lg:   18px
--token-font-size-xl:   20px
--token-font-size-2xl:  24px

--token-font-weight-normal:   400
--token-font-weight-semibold: 600
--token-font-weight-bold:     700

--token-line-height-base: 1.5
```

### Shape
```
--token-radius-sm:   4px   (badges, pills)
--token-radius-base: 8px   (cards, panels, dialogs)
--token-radius-lg:   12px
--token-shadow-sm:   0 1px 3px rgba(0,0,0,0.08)
--token-shadow-md:   0 2px 8px rgba(0,0,0,0.12)
```

### Breakpoints (SCSS only — cannot use in CSS media queries)
```scss
$content-breakpoint-small:  "screen and (min-width: 576px)"
$content-breakpoint-medium: "screen and (min-width: 768px)"
$content-breakpoint-large:  "screen and (min-width: 992px)"
$content-breakpoint-xlarge: "screen and (min-width: 1200px)"
```
Project uses: mobile <768px / tablet 768–1023px / desktop ≥1024px

---

## Component APIs

All components at `/Users/luke.smith/git/ui/components/{Name}/src/`.

### Button
```tsx
import { Button } from '@emisgroup/ui-button'
// variant: "mono" | "inverted" | "filled" | "filled-inverted" | "danger" | "warning" | "success"
// size: "small" | "medium"
// ariaLabel?: string  (required for icon-only buttons)
// ariaPressed?: boolean
// borderless?: boolean
// circular?: boolean  (for icon-only round buttons)
// disabled?: boolean
<Button variant="filled" size="small" ariaLabel="..." onClick={...}>
  <Icon ... /> {/* Icon as child — auto-detected */}
</Button>
```
- Badge as child → `hasBadge` styling applied automatically
- Vertical layout (icon above text): `vertical` prop — requires Icon as first child

### Input
```tsx
import { Input } from '@emisgroup/ui-input'
// Extends HTMLInputElement props +
// clearable?: boolean   — shows X button when value present
// invalid?: boolean     — red border state
// focusPlaceholder?: string
// onClear?: () => void
// Must always have aria-label, aria-labelledby, or associated <label htmlFor>
<Input aria-label={t('...')} clearable invalid={hasError} />
```

### Tabs (Radix-based)
```tsx
import { Tabs } from '@emisgroup/ui-tabs'
// activationMode: "automatic" | "manual"  — use "manual" for async-loading tabs
<Tabs defaultValue="inbox" activationMode="manual">
  <Tabs.TabList>
    <Tabs.Tab value="inbox">{t('tab.inbox')} <Count>{n}</Count></Tabs.Tab>
  </Tabs.TabList>
  <Tabs.TabContent value="inbox">...</Tabs.TabContent>
</Tabs>
```

### Count
```tsx
import { Count } from '@emisgroup/ui-count'
// Renders <span role="status"> — exactly 1 child required
<Count>{42}</Count>
```

### Badge
```tsx
import { Badge } from '@emisgroup/ui-badge'
// variant: "primary" | "danger" | "disabled" | "inactive"
// Renders <span role="status">
<Badge variant="primary">{t('status.inProgress')}</Badge>
```

### DropdownMenu (Radix-based)
```tsx
import { DropdownMenu } from '@emisgroup/ui-dropdown-menu'
// DropdownMenu.Content: align default "start" — use align="end" for right-aligned menus
<DropdownMenu>
  <DropdownMenu.Trigger asChild>
    <Button circular variant="mono" ariaLabel={t('...')}>
      <Icon ... />
    </Button>
  </DropdownMenu.Trigger>
  <DropdownMenu.Content align="end">
    <DropdownMenu.Item onSelect={...}>{t('action.history')}</DropdownMenu.Item>
    <DropdownMenu.Item onSelect={...}>{t('action.reject')}</DropdownMenu.Item>
  </DropdownMenu.Content>
</DropdownMenu>
// Keyboard: Arrow keys navigate; Enter activates; Escape closes; focus returns to trigger
```

### Dialog (Radix-based)
```tsx
import { Dialog, DialogInner, DialogTrigger, Header, Content, Footer } from '@emisgroup/ui-dialog'
// preventBackdropClose — blocks dismiss on outside click (use for mandatory-reason modals)
// fullscreen — full-screen variant
// modal — true by default
<Dialog open={open} onOpenChange={setOpen}>
  <DialogInner preventBackdropClose>
    <Header>{t('dialog.heading')}</Header>
    <Content>...</Content>
    <Footer>
      <Button variant="mono" onClick={() => setOpen(false)}>{t('common.cancel')}</Button>
      <Button variant="danger" disabled={!valid}>{t('common.confirm')}</Button>
    </Footer>
  </DialogInner>
</Dialog>
// Focus trapped inside; Escape calls onOpenChange(false) unless preventBackdropClose
```

### Notification (Toast — Radix Toast-based)
```tsx
import { Notification, NotificationContainer } from '@emisgroup/ui-notification'
// variant: "success" | "error" | "info"
// duration: default 5000ms; use Infinity for persistent
// Wrap app in <NotificationContainer>
<Notification open={show} variant="success" onClose={() => setShow(false)} duration={5000}>
  <Notification.Title>{t('toast.title')}</Notification.Title>
  <Notification.Content>{t('toast.body')}</Notification.Content>
  <Notification.Close>{t('common.close')}</Notification.Close>
</Notification>
```

### Skeleton
```tsx
import { Skeleton } from '@emisgroup/ui-skeleton'
// Skeleton.Item: variant "square" | "circle" | "rounded"
// Shimmer animation built-in — no extra CSS needed
<Skeleton.Area role="status" aria-live="polite" aria-label={t('loading')}>
  {Array.from({ length: 8 }).map((_, i) => (
    <Skeleton.Item key={i} style={{ height: 44, marginBottom: 1 }} />
  ))}
</Skeleton.Area>
```

### Table
```tsx
import { Table } from '@emisgroup/ui-table'
// No built-in row hover — implement with CSS on consuming component
// Variants: tableZebra (striped), borders, tableCollapsedBorders, tableSeparateBorders
// Row hover pattern (project standard):
// tr { transition: background-color 150ms ease-in-out; }
// tr:hover { background-color: var(--token-colour-neutral-100); }
// tr:hover .actionsButton, tr:focus-within .actionsButton { opacity: 1; }
// .actionsButton { opacity: 0; transition: opacity 150ms; }
```

### Alert / Banner
```tsx
// Use Alert for inline errors (replaces content)
// Use Banner for persistent warnings above content (source unavailability etc.)
import { Alert } from '@emisgroup/ui-alert'
import { Banner } from '@emisgroup/ui-banner'
```

### Icon
```tsx
// Import pattern — Iconify virtual imports
import OutlineArrowUpward from '~icons/ic/outline-arrow-upward'
import OutlineMoreVert from '~icons/ic/outline-more-vert'
import OutlineSearch from '~icons/ic/outline-search'
import OutlineInbox from '~icons/ic/outline-inbox'
import OutlineCheckCircle from '~icons/ic/outline-check-circle'
// Sizes: 16px (small), 20px (default), 24px (large action)
// Colour: inherit from text, or set via style={{ color: 'var(--token-...)' }}
```

---

## Available components (full list)

Accordion, Alert, Avatar, Badge, Banner, Breadcrumbs, Button, Card, Checkbox, ChipInput, CollapsiblePanel, Combobox, Command, CopyToClipboard, Count, DataList, DatePicker, DetailPanel, Dialog, Dropdown, DropdownMenu, Flyout, FoldIndicator, Form, Graph, Icon, Input, Layouts, List, Notification, Pagination, Pill, Popover, Popup, ProgressIndicator, RadioButton, RichTextEditor, SearchDropdown, Sections, Skeleton, Slider, Spangle, Splitter, StepIndicator, Switch, Table, TableOfContents, Tabs, Tag, Theme, Timeline, TitleBar, ToggleButtons, Tree

---

## Theme system

```tsx
import { useTheme } from '@emisgroup/ui-theme'
// Themes: "light" | "dark" | "system"
// Palettes: "emis" | "optum"
// Densities: "xsmall" | "small" | "medium" | "large" | "xlarge"
// FontSizes: "small" | "medium" | "large" | "xlarge"
// Stored in localStorage under keys: emis-ui-density, emis-ui-fontsize, emis-ui-palette, emis-ui-theme
```

Internal SCSS tokens (short names used inside component source — NOT for product code):
`var(--primary)`, `var(--hover)`, `var(--neutral-dark)`, `var(--border-bright)`, `var(--background-dim)`, `var(--severe)`, `var(--positive)`, `var(--spacing-small)`, `var(--font-size)`, `var(--border-radius)`, `var(--animation-speed)`
→ Product code must use `var(--token-*)` names, not these internal names.

---

## Extension tokens (project-specific — must be declared in PxD)

Any token not in the base design system must be:
1. Named with `var(--token-*)` pattern
2. Listed in the relevant REQ's PxD Visual Design section with hex value + usage
3. Flagged for V1f task generation to register in the token extension layer

Known extension tokens for this project (declared in REQ-008):
```
--token-colour-annotation-highlight-yellow: #FFF9C4
--token-colour-annotation-highlight-green:  #C8E6C9
--token-colour-annotation-highlight-red:    #FFCDD2
```
