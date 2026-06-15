# SKILL: service-classification-rules
# Phase: P03 Architecture — Phase 2 / Phase 3

## Service Classification Rules

Every requirement MUST have a `### Service Classification` section in its Architecture output. Multiple requirements can share a service. Multiple services can appear in one requirement.

This classification drives Pipeline 08 task generation — without it, the coding agent scaffolds everything as `new`.

### Classification Values

| Scope | Meaning | Pipeline 04 implication |
|-------|---------|------------------------|
| `new` | Brand new microservice, full scaffold required | Full API, domain, infrastructure scaffold |
| `existing_extend` | Existing service, adding new endpoints/APIs only | Additive only — do not redesign existing contracts |
| `existing_modify` | Existing service, modifying existing logic or contracts | Delta change — scope must be bounded |
| `existing_use` | Existing service consumed as-is, no code changes | Document the dependency only — skip Phases 1-11 for this service in Pipeline 04 |

### Service Classification Template

```markdown
### Service Classification

| Field | Value |
|-------|-------|
| `service_name` | {e.g. GpcTranscriptionService} |
| `service_scope` | new | existing_extend | existing_modify | existing_use |
| `target_repository` | {null for new; GitHub repo full name for existing} |
| `affected_endpoints` | {null for new; list of affected paths for existing} |
| `v3_agents` | ["EMIS-X_API_ENGINEER"] | ["EMIS-X_WEBAPP_ENGINEER"] | both |
```

### Routing Impact

- `existing_use` → Pipeline 04 and Pipeline 08 skip this service entirely. Document the dependency endpoint(s) only.
- `existing_extend` → Pipeline 04 designs new endpoints only. Never touch existing contracts.
- `existing_modify` → Pipeline 04 scopes changes to specified files/endpoints only.
