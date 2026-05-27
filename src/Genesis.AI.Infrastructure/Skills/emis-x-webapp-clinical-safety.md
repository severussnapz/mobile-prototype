---
name: emis-x-webapp-clinical-safety
description:
  Clinical safety guardrails and steers for EMIS-X microfrontend applications
  covering patient context readiness, patient banner state handling, and safe
  rendering of clinical content. This skill should be used when developing
  patient-facing applications, working with patient context, or when users ask
  about clinical safety requirements. Rules are prefixed WCLIN and must be
  satisfied by all generated code in patient context applications.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-webapp
    - requirements
---

# EMIS-X Webapp Clinical Safety Guardrails and Steers

This skill defines mandatory clinical safety guardrails and steers for EMIS-X
microfrontend applications that operate in a patient context. All generated code
in patient context applications **must** satisfy every applicable rule. Apply
these proactively during code generation — not as a post-hoc review.

**Target versions:** React 18.3+,
`@emisgroup/acp-utility-patient-context`.

## Applicability

These rules apply to any EMIS-X microfrontend whose `package.json`
`applicationDiscovery.applications` array includes an entry with
`"patientContext": true`. Applications without patient context are not subject
to these rules.

## Rules Index

| ID       | Name                              | Type      | Severity |
| -------- | --------------------------------- | --------- | -------- |
| WCLIN-001 | Patient Banner State Readiness    | Steer     | Critical |
| WCLIN-001a| Patient Banner State Subscription | Guardrail | Critical |
| WCLIN-002 | Patient Context Presence Check    | Steer     | Critical |
| WCLIN-002a| Patient Presence Check Detection  | Guardrail | Critical |

---

## Rendering Gate Order

Patient context applications must follow this exact order when gating content:

1. **WCLIN-002 — Patient presence:** Check `patientContext.personGuid`. If empty,
   show "No patient selected" and stop. Subscribe to `subscribeToPersonSwap`
   to react if the patient is cleared after initial load.
2. **WCLIN-001 — Banner state:** Only if a patient is present, subscribe to
   `subscribeToPatientBannerState` and handle `Initiated` / `Loaded` / `Error`
   / `Confidential` before rendering content.

---

## WCLIN-001: Patient Banner State Readiness

**Type:** Steer

**Requirement:** Patient context applications must not render clinical content
until the patient banner state confirms the patient is fully loaded. The
application must subscribe to `subscribeToPatientBannerState` from
`@emisgroup/acp-utility-patient-context` and handle all four
`PatientBannerState` values before rendering any patient-related content.

**Severity:** Critical — rendering clinical content before the patient is
confirmed loaded risks displaying stale data, data from the wrong patient, or
incomplete clinical information. This is a patient safety concern.

**Exceptions:** None.

**Evidence Required:** Confirm the application subscribes to
`subscribeToPatientBannerState`. State how each `PatientBannerState` value is
handled: `Initiated` (loading/waiting), `Loaded` (render content), `Error`
(error message displayed), `Confidential` (confidential patient message
displayed). Confirm the subscription is cleaned up on component unmount.

### PatientBannerState Values

| State          | Meaning                            | Required Handling                              |
| -------------- | ---------------------------------- | ---------------------------------------------- |
| `Initiated`    | Patient context loading in progress| Show a loading indicator; do NOT render content |
| `Loaded`       | Patient fully loaded and ready     | Render the application content                 |
| `Error`        | Patient context failed to load     | Show a user-friendly error message             |
| `Confidential` | Patient is marked as confidential  | Show a message stating the patient is confidential; do NOT render clinical content |

### Required Pattern

The banner state check should occur **after** confirming a patient is present
(see WCLIN-002). The combined pattern is shown in the recommended hook below.

```typescript
import { getPatientContext } from '@emisgroup/acp-utility-patient-context';
import { PatientBannerState } from '@emisgroup/acp-utility-patient-context';

const patientContext = getPatientContext();

// Step 1: Check patient is present (WCLIN-002)
if (!patientContext.personGuid) {
  return <Alert variant="info">No patient selected.</Alert>;
}

// Step 2: Check banner state (WCLIN-001)
const [bannerState, setBannerState] = useState<PatientBannerState>(
  patientContext.patientBannerState,
);

useEffect(() => {
  const subscription = patientContext.subscribeToPatientBannerState(() => {
    setBannerState(patientContext.patientBannerState);
  });

  return () => subscription.unsubscribe(); // ✅ Clean up on unmount
}, []);

// Gate rendering based on banner state
switch (bannerState) {
  case PatientBannerState.Initiated:
    return <ProgressIndicator label="Loading patient…" />;
  case PatientBannerState.Error:
    return <Alert variant="error">Unable to load patient. Please try again.</Alert>;
  case PatientBannerState.Confidential:
    return <Alert variant="warning">This patient record is confidential.</Alert>;
  case PatientBannerState.Loaded:
    return <AppContent />;
}
```

### Custom Hook Pattern (Recommended)

Extract both the presence check and the banner state subscription into a
reusable hook that returns a discriminated status:

```typescript
import { useEffect, useState, useCallback } from 'react';
import {
  getPatientContext,
  PatientBannerState,
} from '@emisgroup/acp-utility-patient-context';

export type PatientReadiness =
  | { status: 'no-patient' }
  | { status: 'loading' }
  | { status: 'loaded' }
  | { status: 'error' }
  | { status: 'confidential' };

export const usePatientReadiness = (): PatientReadiness => {
  const patientContext = getPatientContext();

  const deriveState = useCallback((): PatientReadiness => {
    // WCLIN-002: Check patient presence first
    if (!patientContext.personGuid) {
      return { status: 'no-patient' };
    }
    // WCLIN-001: Then check banner state
    switch (patientContext.patientBannerState) {
      case PatientBannerState.Initiated:
        return { status: 'loading' };
      case PatientBannerState.Error:
        return { status: 'error' };
      case PatientBannerState.Confidential:
        return { status: 'confidential' };
      case PatientBannerState.Loaded:
        return { status: 'loaded' };
    }
  }, []);

  const [readiness, setReadiness] = useState<PatientReadiness>(deriveState);

  useEffect(() => {
    // Re-evaluate when banner state changes
    const bannerSub = patientContext.subscribeToPatientBannerState(() => {
      setReadiness(deriveState());
    });

    // Re-evaluate when patient is swapped or cleared (WCLIN-002)
    const swapSub = patientContext.subscribeToPersonSwap(() => {
      setReadiness(deriveState());
    });

    return () => {
      bannerSub.unsubscribe();
      swapSub.unsubscribe();
    };
  }, [deriveState]);

  return readiness;
};
```

Usage in the application root:

```typescript
const readiness = usePatientReadiness();

switch (readiness.status) {
  case 'no-patient':
    return <Alert variant="info">No patient selected.</Alert>;
  case 'loading':
    return <ProgressIndicator label="Loading patient…" />;
  case 'error':
    return <Alert variant="error">Unable to load patient. Please try again.</Alert>;
  case 'confidential':
    return <Alert variant="warning">This patient record is confidential.</Alert>;
  case 'loaded':
    return <AppContent />;
}
```

### Common Mistakes

| ❌ Wrong                                                      | ✅ Correct                                                     |
| ------------------------------------------------------------- | -------------------------------------------------------------- |
| Render content immediately without checking banner state       | Gate rendering behind `PatientBannerState.Loaded`              |
| Skip the patient presence check                                | Check `personGuid` before banner state (WCLIN-002)              |
| Ignore `Error` state — show blank screen                       | Display an error message to the user                           |
| Ignore `Confidential` state — render clinical data             | Display a confidential patient message                         |
| Assume patient is loaded if `patientContext` exists             | Always check `patientBannerState` via subscription             |
| Forget to unsubscribe on unmount                               | Return `subscription.unsubscribe()` in `useEffect` cleanup    |
| Skip the `Initiated` state — render partial content            | Show a loading indicator and wait for `Loaded`                 |

### Verification Checklist

```
□ personGuid checked before banner state? (WCLIN-002)
□ No-patient state → "No patient selected" message shown? (WCLIN-002)
□ subscribeToPersonSwap used to react to patient clear? (WCLIN-002)
□ subscribeToPatientBannerState is called? (WCLIN-001)
□ Initiated state → loading indicator shown?
□ Loaded state → application content rendered?
□ Error state → user-friendly error message shown?
□ Confidential state → confidential message shown (no clinical content)?
□ All subscriptions cleaned up on unmount?
□ No clinical content rendered before Loaded state confirmed?
```

---

## WCLIN-001a: Patient Banner State Subscription Detection

**Type:** Guardrail — deterministic subset of WCLIN-001

**Requirement:** Applications with
`applicationDiscovery.applications[].patientContext: true` in their
`package.json` must contain at least one call to
`subscribeToPatientBannerState` in their production source code. This ensures the
application gates rendering on patient readiness.

**Severity:** Critical

**Exceptions:** None.

### Detection Logic

1. Read `package.json` → check any `applicationDiscovery.applications[].patientContext === true`
2. If no application has `patientContext: true`, skip (rule does not apply)
3. Scan all production TypeScript files for `subscribeToPatientBannerState`
4. If no match found → **fail** — the application does not gate on patient
   readiness

### Verification Checks

- `package.json` has `applicationDiscovery.applications[].patientContext: true`
- At least one `.ts` or `.tsx` production file contains
  `subscribeToPatientBannerState`

✅ **Good:**

```typescript
import { subscribeToPatientBannerState } from '@emisgroup/acp-utility-patient-context';

const PatientApp = () => {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const subscription = subscribeToPatientBannerState((state) => {
      setReady(state === 'ready');
    });
    return () => subscription.unsubscribe();
  }, []);

  if (!ready) return <LoadingSpinner />;
  return <ClinicalContent />;
};
```

❌ **Bad:**

```typescript
// Wrong: no subscribeToPatientBannerState call — renders without waiting for patient readiness
const PatientApp = () => {
  return <ClinicalContent />;
};
```

---

## WCLIN-002: Patient Context Presence Check

**Type:** Steer

**Requirement:** Patient context applications must check whether a patient is
actually selected before proceeding to the banner state flow. If
`patientContext.personGuid` is empty (no patient selected), the application must
display a "No patient selected" message and must not render clinical content or
begin loading patient data.

Because the patient context can be **cleared after initial load** (e.g. the user
deselects the patient), the application must also subscribe to
`subscribeToPersonSwap` and re-evaluate `personGuid` when the patient changes
or is cleared.

**Severity:** Critical — rendering without a patient risks showing stale data,
data from a previous patient, or triggering errors when patient-dependent API
calls have no identifier.

**Exceptions:** None.

**Evidence Required:** Confirm `patientContext.personGuid` is checked before the
banner state subscription. State what is displayed when `personGuid` is empty
(must be a "No patient selected" message). Confirm `subscribeToPersonSwap` is
used to handle the patient being cleared after initial load. Confirm the swap
subscription is cleaned up on unmount.

### Required Flow

```
Is personGuid populated?
  ├─ NO  → Show "No patient selected" message. Stop.
  └─ YES → Proceed to WCLIN-001 (banner state check)
               ├─ Initiated    → Show loading indicator
               ├─ Loaded       → Render app content
               ├─ Error        → Show error message
               └─ Confidential → Show confidential message
```

### Handling Patient Clear

The user may clear the patient context after the application has loaded. The
application must react to this:

```typescript
const patientContext = getPatientContext();

useEffect(() => {
  // React when the patient is swapped or cleared
  const swapSub = patientContext.subscribeToPersonSwap(() => {
    // Re-check personGuid — it may now be empty
    if (!patientContext.personGuid) {
      setReadiness({ status: 'no-patient' });
    }
  });

  return () => swapSub.unsubscribe();
}, []);
```

### Common Mistakes

| ❌ Wrong                                                       | ✅ Correct                                                      |
| -------------------------------------------------------------- | --------------------------------------------------------------- |
| Skip straight to banner state without checking `personGuid`    | Check `personGuid` first; show message if empty                 |
| Assume `personGuid` is always populated in patient context apps| It may be empty on initial load or after patient is cleared     |
| Check `personGuid` once on mount but ignore patient clear      | Subscribe to `subscribeToPersonSwap` to react to changes       |
| Show a loading spinner when no patient is selected             | Show a clear "No patient selected" message                      |

---

## WCLIN-002a: Patient Presence Check Detection

**Type:** Guardrail — deterministic subset of WCLIN-002

**Requirement:** Applications with
`applicationDiscovery.applications[].patientContext: true` in their
`package.json` must reference `personGuid` in at least one production source
file. This ensures the application checks whether a patient is actually selected
before rendering clinical content.

**Severity:** Critical

**Exceptions:** None.

### Detection Logic

1. Read `package.json` → check any
   `applicationDiscovery.applications[].patientContext === true`
2. If no application has `patientContext: true`, skip (rule does not apply)
3. Scan all production TypeScript files for `personGuid`
4. If no match found → **fail** — the application does not verify a patient is
   present before rendering

### Verification Checks

- `package.json` has `applicationDiscovery.applications[].patientContext: true`
- At least one `.ts` or `.tsx` production file references `personGuid`

✅ **Good:**

```typescript
import { getPatientContext } from '@emisgroup/acp-utility-patient-context';

const ClinicalView = () => {
  const context = getPatientContext();

  if (!context?.personGuid) {
    return <NoPatientSelected />;
  }

  return <PatientRecord personGuid={context.personGuid} />;
};
```

❌ **Bad:**

```typescript
// Wrong: no personGuid check — renders clinical content without verifying a patient is selected
const ClinicalView = () => {
  return <PatientRecord />;
};
```

---

## Gotchas

- `subscribeToPatientBannerState` and `personGuid` checks are **both** required in patient-context apps — they serve different purposes. The banner state subscription gates on readiness (WCLIN-001a); the `personGuid` check gates on presence (WCLIN-002a). One does not imply the other.
- The analyser only checks for the **presence** of `subscribeToPatientBannerState` and `personGuid` in source code — it cannot verify they are used correctly (e.g., that the subscription actually gates rendering). The steer rules WCLIN-001 and WCLIN-002 cover the full behavioural requirement.
- Files in `__tests__/`, `*.test.ts`, and `*.spec.ts` are excluded from the scan — the function must appear in production code, not just in tests.
- If `patientContext` is `false` or absent in all `applicationDiscovery.applications`, both WCLIN-001a and WCLIN-002a are skipped automatically. You do not need to suppress them.
- The `subscribeToPersonSwap` API (from the same package) is for reacting to patient changes after initial load — it is **not** a substitute for `subscribeToPatientBannerState` which gates on initial readiness.
- Clinical safety rules are derived from DCB0129 (Clinical Risk Management). They are non-negotiable — suppressing them requires explicit clinical safety officer approval and a documented risk assessment.
