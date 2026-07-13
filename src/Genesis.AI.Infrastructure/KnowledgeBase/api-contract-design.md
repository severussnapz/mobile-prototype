# Skill: API & Contract Design Craft

**Apply whenever:** designing a new endpoint, evolving an existing one, producing the P04 API-CONTRACT.yaml, or reviewing any change to a request/response shape. This is the craft of designing a *good* contract — distinct from the contract-layer *mechanics* (versioning, pinning, manifests) which are already designed.

---

## Resources, not verbs

Model the domain as resources with state, manipulated by standard verbs — `POST /projects/{id}/artefacts`, not `POST /createArtefact`. When an operation resists resource-shaping (generate a session close, trigger a push), model the *result or the request* as the resource (`POST .../session-close` creating a session-close artefact) rather than inventing RPC verbs. A URL should read as a noun-path; the verb is the method.

Nesting expresses ownership, and stops at the ownership boundary: `/projects/{id}/conversations/{id}/stream` is right because conversations belong to projects; four levels deep is a smell that the resource has its own identity and deserves a top-level path.

## Design the errors as carefully as the successes

- Every failure a caller can trigger is part of the contract: enumerate them at design time — this is exactly what ERROR-CATALOGUE.md exists for. An error discovered by the frontend in integration is a design escape.
- The two-tier pattern is the house standard: blocking errors return ProblemDetails with a plain-English actionable `userMessage`; background/best-effort failures log to a failure store and surface via a status endpoint, never blocking the primary action. New endpoints declare which tier each failure belongs to.
- Status codes carry meaning — 400 (caller's request malformed) vs 403 (authenticated but not allowed) vs 404 (not there, or not yours to know about) vs 409 (state conflict) vs 503 (dependency down, retry later). A contract where everything is 400 or 500 forces callers to parse prose.
- Never leak internals: no stack traces, no raw exception text, no ORM messages. And mind information disclosure in the *choice* of error: 404 vs 403 can reveal existence (see security-engineering.md).

## Shape rules that prevent downstream pain

- **Enums as strings** (`ToString()`, `HasConversion<string>()`), never ints and never kebab-cased — the frontend matches on them directly; a numeric enum is an undocumented contract. (Established Genesis rule; it exists because it bit.)
- **Every response field is deliberate.** Fields cost forever: once shipped, someone depends on it. Conversely, every field the handler computes must actually cross the boundary — the DTO-mapping-completeness rule is the enforcement of this craft point.
- **Consistency beats local elegance**: same pagination shape, same timestamp format (ISO 8601, UTC), same ID representation, same error envelope everywhere. A caller who has integrated one endpoint should be able to guess the next.
- **PATCH per concern**: one PATCH endpoint per form section / concern, not one mega-PATCH covering unrelated field groups — shared-endpoint state contamination is a known Genesis failure mode. Partial update semantics must be explicit (absent field = unchanged; null = clear — pick one and document it).
- **Idempotency by design for anything retried**: PUT/DELETE are naturally idempotent; POST endpoints that create-on-behalf-of-a-retry-prone-caller need an idempotency key or a natural dedupe (the SHA-retry-on-422 GitHub push pattern is the model). Any endpoint a client might reasonably retry after a timeout must state what a duplicate call does.

## Compatibility discipline (the craft side of versioning)

- **Additive is free, everything else is breaking**: new optional fields and new endpoints don't break callers; removing, renaming, retyping, or changing semantics of an existing field does — and "semantics" includes tightening validation. Judge every contract diff against that line.
- **Breaking changes are new versions with a CHANGE record**, never in-place edits — the contract-layer mechanics handle propagation; the craft obligation is *recognising* a break, especially the sneaky ones: widening an enum a client exhaustively switches on, changing a default, reordering pagination.
- **Design for the generator**: the OpenAPI contract feeds NSwag — which means it must be a clean, complete, machine-consumable spec, not documentation-flavoured YAML. Every schema fully typed, nullability explicit, no `object` grab-bags. A contract NSwag can't generate good types from is a defect in the contract.
- **Consumer-first review**: before finalising any contract, write the client call for its two most important operations — as pseudocode or a real snippet. Awkwardness in the client code is a contract defect found at the cheapest possible moment.
