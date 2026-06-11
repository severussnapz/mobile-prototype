# SKILL: data-migration-strategy
# Phase: P04 Design — Phase 8

## Data Migration Strategy

**Purpose:** Plan schema versioning and any required data migration for this requirement.

### Questions

1. "Does this requirement add to an existing table?" → If yes: is the change additive-only or does it modify existing columns?
2. "Is any data transformation required?" → Backfill nulls, normalise formats, etc.
3. "What is the Flyway migration version number?" → Check `db/migrations/` for latest version, use `max + 1`

> ⚠️ **FLYWAY VERSION RULE:** Always check `db/migrations/` directory and list all existing files before assigning a version number. Use the highest existing version + 1. Never guess — duplicate Flyway version numbers cause `Found more than one migration with version N` at runtime.

### Migration Rules

- All DDL changes go in Flyway migrations — never modify existing migration files
- Migrations must be idempotent where possible (`CREATE TABLE IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`)
- Breaking changes (column rename, type change, column removal) require a multi-step migration plan
- No data manipulation in the same migration as DDL if the table contains production data

### Migration Template

```sql
-- V{N}__{description}.sql

-- Add {column} to {table}
ALTER TABLE {table_name}
    ADD COLUMN IF NOT EXISTS {column_name} {TYPE} {CONSTRAINTS};

-- Indexes (always add indexes in the same migration as the column)
CREATE INDEX IF NOT EXISTS idx_{table}_{col} ON {table_name} ({tenant_id}, {col});
```

### Migration Strategy Template

```markdown
### Data Migration Strategy

**Migration file:** `V{N}__{description}.sql`
**Type:** {Additive / Destructive (multi-step) / Data backfill}
**Breaking change:** {Yes/No}
**Rollback plan:** {Drop column / Revert to previous migration}
```
