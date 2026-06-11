# SKILL: platform-boundaries-method
# Phase: P03 Architecture — Phase 3

## Platform Boundaries Analysis

**Purpose:** Define service decomposition and classify service scope per requirement.

### Questions

1. "How many services?" → List names
2. "For each service, what does it own?" → Domain, data
3. "How do services communicate?" → Sync/Async/Both
4. "Data ownership?" → Each service owns its DB?
5. "For each requirement, classify the service scope:" → See `service-classification-rules` skill

### Platform Boundaries Template

```markdown
### Platform Boundaries

**Service:** {Name}
**Owns:** {Domain/capability}
**Depends On:** {Services}
**Exposes:** {Endpoints}
**Communication:** {Sync via ALB | Async via EventBridge | Both}
```

### Validation Format

```
"Platform boundaries:
- Services: {N} ({List names and ownership})
- Communication: {Sync via ALB, Async via EventBridge}
- Data: Each service owns database ✅
- Service classifications per requirement:
  - REQ-001: {ServiceName} → new
  - REQ-002: {ServiceName} → existing_extend (adds /transcription endpoint)
  - REQ-003: {ServiceName} → existing_modify (patches ConsentHandler)
  - REQ-004: {ServiceName} → existing_use (consumes /patients/{id} — no changes)

Correct?"
```
