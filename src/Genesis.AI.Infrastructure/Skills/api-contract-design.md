# SKILL: api-contract-design
# Phase: P04 Design — Phase 1

## API Contract Design (OpenAPI 3.0)

**Purpose:** Design API contracts per requirement. Guardrails enforced inline.

### Fast-Track Rules (from ROUTING CONTEXT)

- `swagger_present: true` for this endpoint → take existing contract as authoritative, run annotation pass only, design gaps only
- `service_scope = existing_extend` → design new endpoints only
- `service_scope = existing_use` → skip (handled in Phase 0B)

### For Each New or Gap Endpoint

Design using OpenAPI 3.0. Apply these guardrails at design time:

**AUTH-004:** Every endpoint MUST have `[Authorize(Policy = "...")]`. No anonymous endpoints without explicit business justification.

**API-001:** All response bodies MUST follow JSON:API shape:
```json
{ "data": { "type": "resourceType", "id": "...", "attributes": { ... } } }
```

**API-005:** All paths MUST be versioned: `/api/v1/{resource}`

**API-007:** All error responses MUST use JSON:API `errors[]` format:
```json
{ "errors": [{ "status": "400", "title": "...", "detail": "..." }] }
```

**SEC-001/002:** Parameterised queries only. No string concatenation.

**ENG-002:** Commands (POST/PUT/DELETE) and Queries (GET) are separate handlers.

**ENG-007:** No single-letter parameter names in handlers.

### Endpoint Design Template

```yaml
{HTTP Method} /api/v1/{resource}:
  summary: {Purpose}
  operationId: {CamelCaseName}
  security:
    - bearerAuth: [{scope}]
  requestBody:
    schema: {RequestDto}
  responses:
    200:
      schema: {JSON:API resource}
    400:
      schema: errors[]
    401: Unauthorized
    403: Forbidden
    404: Not Found
    422: Unprocessable Entity
```

### Cross-Requirement Chain Detection

If this requirement's API output feeds another requirement's input, or if multiple requirements share a coordinating workflow:
- Detect: "Does REQ-{N} depend on output from REQ-{M}?"
- If yes: design an orchestration endpoint, BFF aggregation, or saga — see `cross-requirement-chain` skill.

### Validation

```
"API contract for REQ-{NNN}:
- Endpoints: {list with methods and paths}
- Auth: [Authorize(Policy='{policy}')] on all ✅
- JSON:API: all responses ✅
- Versioned: /api/v1/ ✅
- Error responses: errors[] ✅

Correct?"
```
