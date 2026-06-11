# SKILL: adr-register-protocol
# Phase: P03 Architecture — Phase 1

## ADR Register Protocol

### Mandatory Rule

Do NOT assign a new ADR number until you have checked the existing ADR register to confirm the number is not already used. ADR numbers are assigned sequentially and never reused.

### ADR Register Format

Maintain a running ADR table from Phase 1 onwards. Add every decision as it is confirmed:

```markdown
# Architecture Decision Records

| ADR | Title | Decision | Guardrail | Status |
|-----|-------|----------|-----------|--------|
| ADR-001 | {Title} | {Choice} | {e.g. PG-001} | ✅ Accepted |
```

### ADR Content Template

```
**ADR-{NNN}: {Title}**
- Context: {Why this decision was needed}
- Decision: {The choice made}
- Alternatives: {What else was considered}
- Rationale: {Why this choice over alternatives}
- Consequences: {Trade-offs, known downsides}
- EMIS Principle: Principle {N} ({Name})
- Guardrail: {e.g. ENG-002, PG-001, API-001}
```

### When to Create an ADR

Create an ADR for every non-default decision:
- Database type choice (Postgres vs DynamoDB)
- Auth provider variant
- Any deviation from the EMIS-X mandated stack
- Service architecture decisions (synchronous vs async, BFF vs direct)
- Cross-cutting infrastructure choices (caching strategy, message queue, etc.)

Always create the mandatory index strategy and idempotency ADRs (see `mandatory-adr-index-strategy` and `mandatory-adr-idempotency` skills).
