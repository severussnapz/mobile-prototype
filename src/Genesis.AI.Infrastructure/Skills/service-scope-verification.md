# SKILL: service-scope-verification
# Phase: P04 Design — Phase 0B

## Service Scope Verification

Read the `### Service Classification` section from P03 Architecture for each requirement. Apply routing rules before designing anything.

### Routing Rules

| Scope | Action |
|-------|--------|
| `existing_use` | Skip Phases 1-11 for this service. Write a `### Design` section with: `Service: {Name} — existing, consumed as-is. No design changes. Dependency: {endpoint(s)}.` |
| `existing_extend` | Phase 1: design new endpoints only. Never redesign existing contracts. Phase 2: new tables/columns only. |
| `existing_modify` | Scope changes to the specific files/endpoints listed in `affected_endpoints`. Note what must NOT be changed. |
| `new` | Full Phases 1-11. |

### Output

For each requirement, log the routing decision:
```
REQ-001: {ServiceName} → existing_use → SKIP Phases 1-11. Writing dependency stub.
REQ-002: {ServiceName} → new → FULL design.
REQ-003: {ServiceName} → existing_extend → ADDITIVE ONLY — new /transcription endpoint.
```

### Existing Service Stub Template

```markdown
### Design (Added by Pipeline 04)

#### Service: {ServiceName} — Existing (consumed as-is)

**Dependency endpoint(s):**
- {HTTP method} {path} — {purpose}

**No code changes required.** Pipeline 08 will generate a dependency task (not implementation task) for this requirement.
```
