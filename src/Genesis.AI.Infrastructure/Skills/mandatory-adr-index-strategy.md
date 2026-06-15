# SKILL: mandatory-adr-index-strategy
# Phase: P03 Architecture — Phase 1

## Mandatory Index Strategy ADR

This ADR is required for every project that uses Aurora PostgreSQL. Create it in Phase 1 without asking — it is non-optional.

### Create This ADR

```
ADR-{NNN}: PostgreSQL Index Strategy — Mandatory for All Tables

- Context: Without explicit index planning, queries degrade under load. Naive FK-only
  indexes are insufficient for multi-tenant systems with high query volumes.
- Decision: Every PostgreSQL table DDL created in Pipeline 04 must include an explicit
  index strategy derived from the API contract query patterns.
  Minimum: composite (tenant_id, <primary_query_column>) index on every table.
  Plus: any covering indexes implied by API contract query parameters.
  If no additional indexes are needed: a `-- No additional indexes: <reason>` comment
  is required in the DDL.
- Alternatives: Add indexes reactively after performance issues observed.
- Rationale: Proactive index design prevents production performance incidents.
  tenant_id is always the first column in composite indexes (multi-tenant isolation).
- Consequences: Pipeline 04 must include index DDL for every table.
  Flyway migration review includes index completeness check.
- EMIS Principle: Principle 5 (Managed Services / Performance)
- Guardrail: PG-004, PG-005
```

### Enforcement in Pipeline 04

When Pipeline 04 generates DDL, the index section is mandatory. Flag any table missing it.
