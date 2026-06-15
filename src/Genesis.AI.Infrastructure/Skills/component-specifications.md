# SKILL: component-specifications
# Phase: P05 Product Experience Design — Phase 3

## Component Specifications

**Purpose:** Write detailed React component specifications from the wireframes.

### Component Specification Template

```typescript
// {ComponentName}.tsx — specification

interface {ComponentName}Props {
    // Required props
    {propName}: {Type};
    // Optional props
    {optionalProp}?: {Type};
    // Event handlers
    on{Action}: ({param}: {Type}) => void;
}

// State:
// - {stateName}: {Type} — {purpose}
// - {stateName}: {Type} — {purpose}

// Behaviour:
// - On mount: {what happens}
// - On {event}: {what happens}
// - Loading state: <ProgressSpinner />
// - Error state: <Banner variant="error">{message}</Banner>
// - Empty state: {see empty-states skill}
```

### displayName Requirement (Mandatory)

Every component MUST set displayName:
```typescript
{ComponentName}.displayName = '{ComponentName}';
```

### Prop Validation

- No `any` types — all props must have explicit TypeScript types
- Event handler props must be prefixed `on{Action}` (not `handle{Action}`)
- `children` props must be typed as `React.ReactNode`

### Validation

```
"Component spec for {ComponentName}:
- Props interface: defined with explicit types
- State: defined
- Behaviour: documented
- displayName: set
- EMIS UI Kit: all interactive elements mapped
- Translation keys: all user-facing text uses t()

Correct?"
```
