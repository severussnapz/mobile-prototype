# Skill: API & Contract Design Craft
**Stage:** P04 — Design (API/DB)
**Injection:** All phases of P04

---

## API-001 — Resources, Not Verbs (Guardrail)

**Severity:** High

Model operations as resources manipulated by standard HTTP verbs. URLs are noun-paths; the HTTP method is the verb.

**Compliant:**
```
✅ POST   /api/v1/projects/{id}/artefacts          — creates an artefact
✅ PATCH  /api/v1/projects/{id}/github-config       — updates github config section
✅ POST   /api/v1/conversations/{id}/session-close  — creates a session-close artefact
```

**Non-compliant:**
```
❌ POST /api/v1/createArtefact
❌ POST /api/v1/doSessionClose
❌ GET  /api/v1/getProjectById
```

When an operation resists resource-shaping (generate, trigger, classify), model the *result or the request* as the resource, not the verb.

---

## API-002 — One PATCH Per Concern (Guardrail)

**Severity:** Critical

Each PATCH endpoint covers exactly one form section or one logical concern. A PATCH that covers multiple unrelated field groups causes state contamination — an update to one section silently resets another.

**Compliant:**
```
✅ PATCH /projects/{id}/details          — name, description, timesheet code
✅ PATCH /projects/{id}/github-config    — repo URLs, installation ID
✅ PATCH /projects/{id}/p00-config       — roles, compliance settings
```

**Non-compliant:**
```
❌ PATCH /projects/{id}   — covers all fields in one endpoint
```

Partial update semantics must be explicit in the contract: absent field = unchanged; null field = clear. Pick one and document it. Never leave it ambiguous.

---

## API-003 — Design the Errors as Carefully as the Successes (Guardrail)

**Severity:** Critical

Every failure mode a caller can trigger is part of the contract. Enumerate them at design time — this is what ERROR-CATALOGUE.md exists for. An error discovered by the frontend in integration is a design escape.

**Required for every new endpoint:**
- List the HTTP status code for each failure class (400/403/404/409/503).
- Write a plain-English `userMessage` for every blocking error.
- Classify each failure as Tier 1 (blocking, ProblemDetails + userMessage) or Tier 2 (background, push_failure_log + push-status endpoint).

**Status code rules:**
- 400 — caller's request is malformed
- 403 — authenticated but not authorised (or resource exists but caller must not know)
- 404 — not found (or not found *for this caller* — same response, different reason)
- 409 — state conflict (already exists, already approved, version mismatch)
- 503 — dependency unavailable, safe to retry

Never use 500 for a condition the caller caused. Never expose stack traces, ORM messages, or internal identifiers in error responses.

---

## API-004 — Enums as Strings (Guardrail)

**Severity:** Critical

All enum values in API responses are serialised as strings using `ToString()` — never as integers, never as kebab-case. The frontend matches on them directly; a numeric enum is an undocumented protocol that breaks silently on reorder.

**Compliant:**
```json
{ "stageType": "RequirementsDiscovery" }
{ "domain": "ClinicalSafety" }
{ "state": "Ratified" }
```

**Non-compliant:**
```json
{ "stageType": 1 }
{ "domain": "clinical-safety" }
```

---

## API-005 — Every Response Field Is Deliberate (Guardrail)

**Severity:** Critical

Fields cost forever — once in a contract, callers depend on them. Before adding a field to a response DTO, confirm it is needed by a named consumer. Equally: every field a handler computes must cross the HTTP boundary. A field computed in the handler but absent from the response DTO is a silent drop.

**Required mapping test for every new field:**
```
handler produces FieldX → response DTO includes FieldX → integration test asserts FieldX in HTTP body
```

All three must exist before the deliverable is closed.

---

## API-006 — Idempotency for Retried Operations (Steer)

**Severity:** High

Any endpoint a client might reasonably retry after a timeout must state what a duplicate call does. PUT and DELETE are naturally idempotent. POST endpoints that create resources need either natural deduplication (same filePath + same content = same version via the existing artefact versioning) or an explicit idempotency key.

Document the idempotency behaviour in the contract — never leave it implicit.

---

## API-007 — Design for the Generator (Guardrail)

**Severity:** High

The `API-CONTRACT.yaml` output feeds NSwag for TypeScript type generation. It must be a clean, machine-consumable OpenAPI spec — not documentation-flavoured YAML.

**Required:**
- Every schema fully typed — no `object` grab-bags
- Nullability explicit (`nullable: true` where needed, absent where not)
- Every response schema named and $ref'd — not inlined ad hoc
- No free-form description fields substituting for typed schema

**Test before saving:** mentally write the generated TypeScript type for the two most important request/response schemas. Awkward generated types mean the contract needs redesign.

---

## API-008 — Additive Is Free; Everything Else Is Breaking (Guardrail)

**Severity:** Critical

Classify every contract change before including it in the manifest version:

**Non-breaking (additive — safe):**
- New optional response field
- New endpoint
- Widening a response type (string → string | null)

**Breaking (requires new manifest version + CHANGE record + domain badges):**
- Removing or renaming a field
- Changing a field's type or format
- Tightening validation (making optional required, narrowing an enum)
- Changing a default value
- Reordering anything a client might enumerate

When uncertain whether a change is breaking: treat it as breaking. The cost of an unnecessary version bump is low; the cost of a missed breaking change in a live system is a patient-facing incident.
