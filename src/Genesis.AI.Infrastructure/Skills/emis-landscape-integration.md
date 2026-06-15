# SKILL: emis-landscape-integration
# Phase: P03 Architecture — Phase 5

## EMIS Landscape Integration (EMIS Principle 7: Reuse)

**Purpose:** Check reuse opportunities before designing new services. EMIS Principle 7 requires checking the Architectural Landscape.

### Questions

1. "Checked EMIS Architectural Landscape?" → Does this already exist?
2. "Existing EMIS services to integrate?" → List services
3. "For each integration, API contract?" → OpenAPI, FHIR, custom
4. "Authentication for integrations?" → CIS2, mTLS, API keys
5. "Failure handling?" → Circuit breaker, cache, degrade

### Common EMIS Services

- **EMIS Spine Connector** — NHS Spine/PDS integration
- **EMIS Audit Service** — clinical safety logging
- **EMIS Auth Service** — CIS2 OAuth2
- **EMIS FHIR Gateway** — FHIR UK Core

### Integration Template

```markdown
### Integration Points

| Service | Purpose | API | Auth | Failure Handling |
|---------|---------|-----|------|-----------------|
| {Service Name} | {Purpose} | {OpenAPI/FHIR/custom} | {CIS2/mTLS/API key} | {Circuit breaker/cache/degrade} |
```

### Validation Format

```
"Integrations:
- {Service 1}: {Purpose, API, auth, failure handling}
- {Service 2}: {Purpose, API, auth, failure handling}
- EMIS Principle 7: ✅ Reusing {N} services

Correct?"
```
