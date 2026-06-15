# SKILL: wireframe-design
# Phase: P05 Product Experience Design — Phase 2

## Wireframe Design

**Purpose:** Design layout wireframes as text/ASCII for each screen in the user flow.

> **Text wireframes only.** No image generation. Layouts expressed as ASCII art or structured markdown tables.

### Wireframe Template

```
┌─────────────────────────────────────────────────────────┐
│ [Header: {Screen Title}]                    [User] [Logout] │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  [Breadcrumb: Home > {Section} > {Screen}]                │
│                                                           │
│  ┌──────────────────────────────────────────────────┐   │
│  │ {Primary content area}                            │   │
│  │                                                    │   │
│  │  [Input: {Field label}]  [Input: {Field label}]   │   │
│  │                                                    │   │
│  │  [Button: {Primary action}]  [Button: Cancel]     │   │
│  └──────────────────────────────────────────────────┘   │
│                                                           │
│  {Error/success banner area}                              │
└─────────────────────────────────────────────────────────┘
```

### Wireframe Rules

- Every interactive element must map to an EMIS UI Kit component (see `emis-ui-kit-baseline`)
- Every input must have a visible label (no placeholder-only inputs — accessibility requirement)
- Primary actions use `variant="filled"`, secondary use `variant="mono"`, destructive use `variant="danger"`
- Loading states: always show `<ProgressSpinner>` or `<ProgressBar>` — never blank screen
- Error states: use `<Banner>` component above the relevant form section

### Validation

```
"Wireframe for REQ-{NNN} {ScreenName}:
- Layout: {description}
- EMIS components used: {list}
- All inputs have labels: Yes
- Loading/error states defined: Yes

Correct?"
```
