---
name: emis-x-webapp-coding-standards
description: Coding standards guardrails and steers for EMIS-X microfrontend applications covering TypeScript strict mode practices, component structure templates, displayName requirements, file organisation, import ordering, conventional commit messages, and performance patterns. This skill should be used when writing component code, organising files, making commits, or when users ask about coding standards. Rules are prefixed WCS and must be satisfied by all generated code.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-webapp
    - requirements
---

# EMIS-X Webapp Coding Standards Guardrails and Steers

This skill defines mandatory coding standards guardrails and steers for EMIS-X microfrontend applications. All generated code **must** satisfy every applicable rule.

**Target versions:** React 18.3+, TypeScript 5.8+.

## Rules Index

| ID        | Name                          | Type      | Severity |
| --------- | ----------------------------- | --------- | -------- |
| WCS-001   | TypeScript Strict Practices   | Guardrail | High     |
| WCS-002   | Component Structure           | Steer     | Medium   |
| WCS-002a  | Component Structure Detection | Guardrail | Medium   |
| WCS-002b  | Props Definition Patterns     | Guardrail | Medium   |
| WCS-003   | Display Name Required         | Guardrail | Medium   |
| WCS-004   | File Organisation             | Guardrail | Medium   |
| WCS-005   | Import Order                  | Guardrail | Low      |
| WCS-006   | Conventional Commits          | Steer     | Medium   |
| WCS-007   | Internationalisation          | Steer     | High     |
| WCS-007a  | Hardcoded Text Detection      | Guardrail | High     |
| WCS-007b  | British English Spelling      | Guardrail | High     |

---

## WCS-001: TypeScript Strict Practices

**Type:** Guardrail

**Requirement:** Never use `: any` or `as any` in production code. Use `unknown` instead of `any` in catch blocks and for untyped data. Use `Record<string, unknown>` instead of `Record<string, any>`. Always check type definitions before using APIs — never assume field names.

**Severity:** High

**Exceptions:** Test files and `__mocks__/` may use `any` where mocking requires it (e.g., `Mock<any>`).

✅ **Good:**

```typescript
// ✅ Explicit types
const formatName = (first: string, last: string): string => {
  return `${last}, ${first}`;
};

// ✅ Interfaces for props
interface ComponentProps {
  title: string;
  onAction: () => void;
  className?: string;
}

// ✅ Type guards
const isError = (response: ApiResponse): response is ErrorResponse => {
  return 'error' in response;
};

// ✅ Use 'unknown' not 'any'
const process = (data: unknown) => {
  if (typeof data === 'string') {
    // TypeScript knows type here
  }
};

// ✅ Null safety
const name = patient?.demographics?.name ?? 'Unknown';
```

---

## WCS-002: Component Structure

**Type:** Steer

**Requirement:** Components must follow a consistent structure: imports, types/interfaces, constants, component function, export, display name.

**Severity:** Medium

**Exceptions:** None.

**Evidence Required:** Confirm the component follows the standard section ordering: (1) imports, (2) types/interfaces, (3) constants, (4) component function with hooks → handlers → effects → render, (5) export, (6) displayName.

### Component Template

```typescript
// 1. Imports
import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@emisgroup/ui-button';
import styles from './Component.module.scss';

// 2. Types/Interfaces
interface ComponentProps {
  title: string;
  onAction: () => void;
}

// 3. Constants (outside component)
const DEFAULT_TIMEOUT = 5000;

// 4. Component
const Component = ({ title, onAction }: ComponentProps): React.JSX.Element => {
  // Hooks
  const [state, setState] = useState('');
  const { t } = useTranslation();

  // Event handlers
  const handleClick = () => onAction();

  // Effects
  useEffect(() => {
    // Effect logic
  }, []);

  // Render
  return (
    <div className={styles.component}>
      <h2>{title}</h2>
      <Button onClick={handleClick}>{t('Common.Submit')}</Button>
    </div>
  );
};

// 5. Export
export default Component;

// 6. Display name
Component.displayName = 'Component';
```

✅ **Good:** Follows the ordering above — imports, types, constants, component, export, displayName.

❌ **Bad:**

```typescript
// Types defined INSIDE the component
const Component = () => {
  interface Props { title: string; } // Wrong: should be before the component
  return <div />;
};
// Missing displayName and export
```

---

## WCS-002a: Component Structure Detection

**Type:** Guardrail

**Requirement:** Detect components that are missing key structural sections: (1) a types/interfaces block before the component function OR imported from a co-located types file, (2) an export statement, and (3) a displayName assignment. This is the deterministically testable subset of WCS-002.

**Severity:** Medium

**Exceptions:** Index files (`index.ts`, `index.tsx`) and barrel exports.

### Detection

- Component files (`.tsx`) must contain an `interface` or `type` definition for props **OR** import props from a co-located types file (unless the component takes no props) — see WCS-002b for props definition patterns
- Component files must contain an `export` statement
- Component files must contain a `.displayName =` assignment (cross-references WCS-003)

✅ **Good:**

```typescript
// Inline definition
interface PatientCardProps { name: string; }
const PatientCard = ({ name }: PatientCardProps) => <div>{name}</div>;
export default PatientCard;
PatientCard.displayName = 'PatientCard';
```

```typescript
// Imported from types file
import type { PatientCardProps } from './PatientCardProps';
const PatientCard = ({ name }: PatientCardProps) => <div>{name}</div>;
export default PatientCard;
PatientCard.displayName = 'PatientCard';
```

❌ **Bad:**

```typescript
// Missing: interface/type definition or import, displayName
const PatientCard = ({ name }) => <div>{name}</div>;
export default PatientCard;
```

---

## WCS-002b: Props Definition Patterns

**Type:** Guardrail

**Requirement:** Components that use props must define them using one of three acceptable formats. **The chosen format must be consistent throughout the project** — if one component uses a separate props file, all components should follow the same pattern. Props interfaces must NOT use an `I` prefix (e.g., `IPatientCardProps`) — this is a .NET pattern that does not translate to TypeScript conventions.

**Severity:** Medium

**Exceptions:** None.

### Format 1: Inline Interface (Recommended for Simple Components)

Define the props interface at the top of the component file, before the component function.

```typescript
// PatientCard.tsx
interface PatientCardProps {
  name: string;
  age: number;
}

const PatientCard = ({ name, age }: PatientCardProps) => (
  <div>{name}, {age}</div>
);

export default PatientCard;
PatientCard.displayName = 'PatientCard';
```

### Format 2: ComponentProps.ts File

Create a dedicated file named `ComponentProps.ts` alongside the component. **Do not use an `I` prefix** (e.g., `IPatientCardProps`).

```typescript
// PatientCardProps.ts
export interface PatientCardProps {
  name: string;
  age: number;
}
```

```typescript
// PatientCard.tsx
import { type PatientCardProps } from './PatientCardProps';

const PatientCard = ({ name, age }: PatientCardProps) => (
  <div>{name}, {age}</div>
);

export default PatientCard;
PatientCard.displayName = 'PatientCard';
```

### Format 3: Component.types.ts File

Create a file named `Component.types.ts` containing all type definitions for the component (props, state types, helper types, etc.). Useful when a component has multiple related type definitions.

```typescript
// PatientCard.types.ts
export interface PatientCardProps {
  name: string;
  age: number;
}

export interface PatientStatus {
  active: boolean;
  lastVisit: Date;
}
```

```typescript
// PatientCard.tsx
import { type PatientCardProps, type PatientStatus } from './PatientCard.types';

const PatientCard = ({ name, age }: PatientCardProps) => (
  <div>{name}, {age}</div>
);

export default PatientCard;
PatientCard.displayName = 'PatientCard';
```

### Consistency Requirement

Whichever format is chosen, it must be used consistently across the project. Mixing formats creates confusion and maintenance burden.

✅ **Good:**
- All components define props inline, OR
- All components use `ComponentProps.ts` files, OR
- All components use `Component.types.ts` files

❌ **Bad:**
- Some components define props inline, others use separate files
- Inconsistent file naming (`PatientCardProps.ts` vs `PatientCard.types.ts`)

### I Prefix Forbidden

Do not use `I` prefix for interface names — this is a .NET/C# pattern that does not align with TypeScript conventions.

✅ **Good:** `PatientCardProps`, `UserContextProps`

❌ **Bad:** `IPatientCardProps`, `IUserContextProps`

---

## WCS-003: Display Name Required

**Type:** Guardrail

**Requirement:** All components must set `ComponentName.displayName = 'ComponentName'` after export. This aids debugging in React DevTools and error messages.

**Severity:** Medium

**Exceptions:** None.

✅ **Good:**

```typescript
const MedicationList = () => <ul>{/* ... */}</ul>;
export default MedicationList;
MedicationList.displayName = 'MedicationList';
```

❌ **Bad:**

```typescript
const MedicationList = () => <ul>{/* ... */}</ul>;
export default MedicationList;
// Missing: MedicationList.displayName = 'MedicationList';
```

---

## WCS-004: File Organisation

**Type:** Guardrail

**Requirement:** Each component must have its own folder containing the component file and tests. If a component has custom styling, it **must** use SCSS modules (`.module.scss`) for scoped styles. Components that only use design system components without custom styling do not require a style module file.

**Severity:** Medium

**Exceptions:** None.

### Standard Structure

```
ComponentName/
├── ComponentName.tsx           # Main component (required)
├── ComponentName.module.scss   # Scoped styles (required if component has custom styling)
├── ComponentName.test.tsx      # Tests (required)
├── index.ts                    # Re-export (optional)
├── types.ts                    # Component types (optional)
├── hooks/
│   └── useComponentLogic.ts    # Component-specific hooks (optional)
└── utils/
    └── helpers.ts              # Component-specific utilities (optional)
```

### Style Module Rules

1. **If a component has custom styling**, it **must** use CSS modules (`.module.scss` or `.module.css`)
2. **Global stylesheets are not allowed** (`.scss` or `.css` without `.module`) - they cause style conflicts
3. **No style file is required** if the component only uses design system components

### Import Pattern for CSS Modules

```typescript
import styles from './ComponentName.module.scss';

// Use in JSX:
<div className={styles.container}>{/* ... */}</div>
```

✅ **Good:** Each component in its own folder with co-located styles and tests.

```
PatientCard/
├── PatientCard.tsx
├── PatientCard.module.scss    # CSS module - scoped styles
└── PatientCard.test.tsx

AlertBanner/
├── AlertBanner.tsx             # Only uses @emisgroup/ui-* components
└── AlertBanner.test.tsx        # No .module.scss needed - no custom styling
```

❌ **Bad:**

```
src/
├── PatientCard.tsx
├── PatientCard.scss            # ❌ Global styles instead of CSS module
├── MedicationList.tsx
├── styles.scss                 # ❌ Shared styles file
└── tests/
    ├── PatientCard.test.tsx    # ❌ Tests separated from components
    └── MedicationList.test.tsx
```

### Rationale

- **CSS Modules** (`.module.scss`) provide scoped class names (`styles.container` → `PatientCard_container_abc123`), preventing style conflicts between components
- **Global stylesheets** (`.scss`) create globally available class names that can conflict across components and modules
- **No empty files**: Components that don't need custom styling shouldn't have empty style modules - use design system components directly

---

## WCS-005: Import Order

**Type:** Guardrail

**Requirement:** Imports must follow a consistent group order, with alphabetical sorting within each group. This aligns with the `import/order` rule from `eslint-plugin-import-x` (used via `@epic-web/config` → `@emisgroup/emisx-config`). All external packages (including `@emisgroup/*`) belong to the same group and are sorted alphabetically. Icon imports (`~icons/*`) are classified as "unknown" by `import-x` (because `~` is not a word character) and must appear **after** all other groups. Use inline type specifiers (`import { type Foo }`) rather than top-level type-only imports (`import type { Foo }`), per the `import/consistent-type-specifier-style` rule.

**Severity:** Low

**Exceptions:** None.

```typescript
// 1. External packages (including @emisgroup/*), alphabetical
import { useTranslation } from '@emisgroup/acp-application-intl';
import { getUserContext } from '@emisgroup/acp-utility-user-context';
import { Button } from '@emisgroup/ui-button';
import axios from 'axios';
import { useState } from 'react';

// 2. Relative imports (./ or ../), alphabetical
import styles from './Component.module.scss';
import { usePatientData } from './hooks/usePatientData';
import { type WidgetProps } from './types';

// 3. Virtual/unknown imports (~icons/*) — must appear last
import BuildIcon from '~icons/ic/baseline-build';
```

✅ **Good:** The example above follows correct ordering.

❌ **Bad:**

```typescript
// Wrong: react before @emisgroup, relative before external, icons mixed in
import { useState } from 'react';
import BuildIcon from '~icons/ic/baseline-build';
import { Button } from '@emisgroup/ui-button';
import styles from './Component.module.scss';
import axios from 'axios';
```

**Note:** Type-only imports use inline specifiers within their group — they are NOT placed in a separate group at the end.

---

## WCS-006: Conventional Commits

> See shared `conventional-commits` skill for the full format specification,
> commit types, and examples.

**Type:** Steer

**Requirement:** Follow the `conventional-commits` skill. Enforce via Husky
commit-msg hook with commitlint.

**Severity:** Medium

### Git Branch Naming

```bash
feature/patient-medication-list
fix/token-refresh-issue
refactor/extract-card-components
docs/update-setup-instructions
```

**Husky Hooks:**
- Pre-commit: Linting and formatting
- Commit-msg: Validates conventional commit format

---

## WCS-007: Internationalisation

**Type:** Steer

**Requirement:** All user-visible static text must be externalised via `react-i18next`. Never hardcode English strings in JSX or user-visible props. The default and primary locale is `en-GB` (British English) — all translation values must use British English spelling and conventions.

**Severity:** High

**Exceptions:** None.

**Evidence Required:** Confirm all user-visible text uses `t()` from `useTranslation()`. State which translation keys were added to `src/locales/en-GB/translation.json`. Confirm all values use British English spelling (e.g., "Organisation" not "Organization", "Colour" not "Color", "Authorise" not "Authorize").

### Setup

```typescript
import { useTranslation } from 'react-i18next';

const Component = (): React.JSX.Element => {
  const { t } = useTranslation();

  return (
    <div>
      <h1>{t('Component.title')}</h1>
      <p>{t('Component.description')}</p>
      <Button type="button">{t('Component.save')}</Button>
    </div>
  );
};
```

### Translation File (`src/locales/en-GB/translation.json`)

```json
{
  "Component": {
    "title": "Patient Summary",
    "description": "View and manage patient information.",
    "save": "Save changes"
  }
}
```

### Key Naming Convention

- Top-level key: PascalCase component name (e.g., `"PatientCard"`)
- Nested keys: camelCase describing the text purpose (e.g., `"loadingMessage"`, `"saveButton"`, `"errorTitle"`)
- Shared keys: Use a `"Common"` top-level key for text reused across components (e.g., `"Common.save"`, `"Common.cancel"`, `"Common.loading"`)

### User-Visible Text Includes

- Text content between JSX tags: `<p>...</p>`, `<Button>...</Button>`, `<h1>...</h1>`
- Props rendered as visible text: `placeholder`, `title` (on tooltips), `label`, `alt`
- `aria-label` values (screen readers announce these to users)
- Error messages shown to the user
- Toast and alert messages

### Interpolation and Plurals

```typescript
// Interpolation
t('PatientList.showing', { count: patients.length })
// en-GB: "Showing {{count}} patients"

// Plurals
t('PatientList.record', { count: records.length })
// en-GB: "{{count}} record" / "{{count}} records"
// In JSON: "record_one": "{{count}} record", "record_other": "{{count}} records"
```

### British English Requirements

All `en-GB` translation values must use British English:

| ❌ American English | ✅ British English |
|---|---|
| Organization | Organisation |
| Color | Colour |
| Authorize | Authorise |
| Center | Centre |
| Behavior | Behaviour |
| Catalog | Catalogue |
| Dialog | Dialogue (in prose); `<Dialog>` (component name) |
| License (verb) | Licence (noun) / License (verb) |
| Customize | Customise |
| Analyze | Analyse |

### Common Mistakes

| ❌ Wrong | ✅ Correct |
|---|---|
| `<p>No results found</p>` | `<p>{t('Search.noResults')}</p>` |
| `<Button>Save</Button>` | `<Button>{t('Common.save')}</Button>` |
| `placeholder="Enter name"` | `placeholder={t('Form.namePlaceholder')}` |
| `title="Settings"` | `title={t('Settings.title')}` |
| `aria-label="Close"` | `aria-label={t('Common.close')}` |
| `setError('Something went wrong')` | `setError(t('Common.genericError'))` |
| `"Organization"` in en-GB JSON | `"Organisation"` in en-GB JSON |

---

## WCS-007a: Hardcoded Text Detection

**Type:** Guardrail — deterministic subset of WCS-007

**Requirement:** Detect hardcoded multi-word English text in JSX component files. All user-visible text must be externalised via `t()` from `react-i18next`. Single-word content is not flagged (may be a component name or technical term) to minimise false positives.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```tsx
const { t } = useTranslation();
return <p>{t('PatientCard.noRecordsFound')}</p>;
```

❌ **Bad:**

```tsx
return <p>No records found</p>;  // Hardcoded multi-word text
```

---

## WCS-007b: British English Spelling

**Type:** Guardrail — deterministic subset of WCS-007

**Requirement:** All `en-GB` translation files must use British English spelling. Scan `**/locales/en-GB/**/*.json` and `**/translation.json` files for known American English spellings and flag them as violations. The analyser checks all string values in the JSON against a dictionary of American-to-British mappings.

**Severity:** High

**Exceptions:** None. Component names and technical terms that happen to match American spellings (e.g., `<Dialog>` as a React component) are not checked — only translation JSON string values are scanned.

---

## Performance Patterns

### Code Splitting

```typescript
import { lazy, Suspense } from 'react';

const HeavyChart = lazy(() => import('./HeavyChart'));

<Suspense fallback={<Skeleton variant="card" lines={5} />}>
  <HeavyChart data={data} />
</Suspense>
```

### Memoisation

```typescript
// useMemo for expensive calculations
const filtered = useMemo(() =>
  data.filter(i => i.active).sort((a, b) => b.date - a.date),
  [data]
);

// useCallback for callbacks to memoised children
const handleClick = useCallback((id: string) => {
  console.log(id);
}, []);

// React.memo for pure components
const ExpensiveComponent = memo(({ data }: Props) => {
  // Heavy rendering
  return <div>{/* UI */}</div>;
});
```

### Cleanup

```typescript
useEffect(() => {
  const handleResize = () => {  };
  window.addEventListener('resize', handleResize);

  return () => window.removeEventListener('resize', handleResize);
}, []);

useEffect(() => {
  const controller = new AbortController();

  axios.get('/api/data', {
    signal: controller.signal,
    timeout: 30_000,
  }).then(({ data }) => { /* ... */ });

  return () => controller.abort();
}, []);
```

### Debouncing User Input

**When to use:** Components driven by user input that trigger API calls — search boxes, filters, autocomplete pickers, form validation.

**Why:** Protects APIs from excessive requests, improves performance for both app and API, keeps logs accurate to actual user intent.

**Minimum delay:** 500ms (recommended default)

**Pattern:** Use the `use-debounce` package.

```typescript
import { useDebounce } from 'use-debounce';

function UserSearchPicker({ onSearch }: Props) {
  // Tracks immediate user input
  const [inputValue, setInputValue] = useState('');

  // Mirrors the search value after debounce delay
  const [debouncedInputValue] = useDebounce(inputValue, 500);

  // Trigger API call only after debounced value changes
  useEffect(() => {
    if (debouncedInputValue) {
      onSearch(debouncedInputValue);
    }
  }, [debouncedInputValue, onSearch]);

  return (
    <TextInput
      value={inputValue}
      onChange={(e) => setInputValue(e.target.value)}
      placeholder="Search users..."
    />
  );
}
```

**When NOT to debounce:**
- Submit buttons (user explicitly clicks)
- Navigation actions
- Critical real-time updates (patient context changes, emergency alerts)

---

## Workflow: Fixing TypeScript Errors

Follow this step-by-step workflow when resolving TypeScript compilation errors.

**STEP 1: Read the Error Message**
```
□ Copy the full error message
□ Identify error type:
  [ ] "Property 'X' does not exist on type 'Y'" → Field name wrong
  [ ] "Type 'A' is not assignable to type 'B'" → Type mismatch
  [ ] "Cannot find module 'X'" → Import issue or missing dependency
  [ ] Other: ________________
```

**STEP 2: Check Type Definitions**
```
For "Property doesn't exist" errors:
□ Find the type definition file:
  - For IUserClaim: node_modules/@emisgroup/acp-utility-common/dist/index.d.ts
  - For UI components: node_modules/@emisgroup/ui-{component}/dist/index.d.ts
□ Search for the interface/type in the file
□ List actual available fields: ________________
□ Common fixes:
  - claims.name → claims.givenName + claims.familyName (WSEC-002)
  - variant="primary" → variant="filled" (check valid enum values) (DS-005)
  - width="80%" → style={{ width: '80%' }}
```

**STEP 3: Fix the Code**
```
□ Replace assumed field/prop with actual field from type definition
□ Use correct type from error message
□ For missing modules: Check if package installed, run pnpm add if needed
```

**STEP 4: Verify Fix**
```
□ Run: get_errors on file → No errors? [ ] YES [ ] NO
□ Run: pnpm tsc --noEmit → Exit code: ___
□ Exit code = 0? [ ] YES [ ] NO
```

---

## Workflow: Fixing Linting Errors

Follow this step-by-step workflow when resolving lint failures.

**STEP 1: Run Lint and Read Output**
```
□ Run: pnpm lint
□ Read FULL error output (don't skip)
□ Note exit code: ___
□ Exit code = 1? → FAILURE, must fix ALL errors
```

**STEP 2: Categorize Errors**
```
Common linting errors:
[ ] Unused imports → Remove them
[ ] Unused variables → Remove or prefix with _ if intentionally unused
[ ] Missing dependency in useEffect → Add to deps array or use eslint-disable with comment
[ ] Prefer const over let → Change to const if variable not reassigned
[ ] Missing return type → Add explicit return type to function
[ ] Inconsistent quotes/formatting → Run pnpm format
[ ] SCSS linting errors → Check import paths, use design tokens
```

**STEP 3: Fix Errors One by One**
```
□ Fix error #1 → Run get_errors to verify
□ Fix error #2 → Run get_errors to verify
□ Continue until all fixed
```

**STEP 4: Auto-fix Where Possible**
```
□ Run: pnpm fix (auto-fixes formatting and some ESLint issues)
□ Run: pnpm lint again
□ Remaining errors: ___
```

**STEP 5: Verify All Fixed**
```
□ Run: pnpm lint → Exit code: ___
□ Exit code = 0? [ ] YES [ ] NO
□ If NO: Return to STEP 3
□ Show results to user: "✅ Linting: All checks passed (exit code: 0)"
```

---

## Gotchas

- `displayName` must be set **after** the export statement, not before it. Setting it before export can cause it to be tree-shaken away in production builds.
- Import order is enforced by `eslint-plugin-import-x` via `@emisgroup/emisx-config` — running `pnpm fix` will auto-sort imports in most cases, but icon imports (`~icons/*`) must be manually moved to the end if the auto-fixer doesn't handle them.
- `@emisgroup/*` packages are external dependencies, not special — they sort alphabetically alongside `react`, `axios`, etc. in the same import group. Do not create a separate group for them.
- Inline type specifiers (`import { type Foo }`) are required, not top-level type-only imports (`import type { Foo }`). The ESLint rule `import/consistent-type-specifier-style` enforces this.
- Hardcoded text detection (WCS-007a) only flags multi-word strings in JSX. Single words like `"Submit"` pass because they could be component names or technical terms. This is intentional to reduce false positives.
- Translation keys must use British English spelling in `en-GB` locale files (WCS-007b). American spellings like "Organization" or "Color" are flagged even if they appear in JSON values, not just keys.
- Husky git hooks validate commit messages locally — but if hooks are bypassed (e.g., `--no-verify`), invalid commit messages will still land. CI does not currently re-validate commit format.
