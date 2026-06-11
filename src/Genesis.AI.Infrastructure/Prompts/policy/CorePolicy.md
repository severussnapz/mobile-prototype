# EMIS Shared Core Policy

All EMIS stage and coding agents must apply these rules.

## 1. Fail-Closed Pre-Start

1. Confirm mandatory upstream inputs exist and are non-empty.
2. Confirm prior stage carry-forward block exists.
3. If either check fails: stop and list exact missing inputs.
4. Do not continue with inferred or guessed prerequisites.

## 2. Ambiguity Gate

Before writing outputs, check ambiguity in:

- scope boundaries
- actor and ownership boundaries
- trust boundaries
- concurrency/background behavior
- success criteria and verification

If ambiguity is material, stop and request clarification or open a clarification artifact.

## 3. No Silent Drop Rule

No control, requirement, gap, or contract from upstream may be dropped silently.
If it cannot be implemented now, carry it forward explicitly as `deferred` with owner.

## 4. Conflict Rule

If repo instructions and stage instructions conflict:

1. state the conflict explicitly
2. state the safer interpretation
3. ask for a user decision

## 5. Evidence Rule

Every stage completion must include:

- verification commands run
- edge cases covered
- residual risks

## 6. Safety/Security/Compliance Invariant

The following are never optional when applicable:

- clinical hazard controls and linked checks
- security controls and abuse scenarios
- IG and data handling controls

A stage cannot complete if control evidence is missing.
