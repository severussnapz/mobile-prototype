# SKILL: database-schema-design
# Phase: P04 Design — Phase 2

## Database Schema Design

**Purpose:** Design DDL for PostgreSQL or access patterns for DynamoDB. Database type from P03 ADR.

### Fast-Track Rule

Read the database ADR from P03. If `DB type = Aurora Postgres` → use Postgres DDL template. If `DB type = DynamoDB` → use access pattern template. Never ask the DB type question again.

### Postgres DDL Rules (MANDATORY)

> ⚠️ **MANDATORY INDEX RULE:** Every DDL block MUST end with an `-- Indexes` section. Derive indexes from the API contract query parameters. Minimum: composite `(tenant_id, <primary_query_column>)` index. Add covering indexes for WHERE/ORDER BY columns. If no additional indexes needed: `-- No additional indexes: <reason>`. A DDL block with no `-- Indexes` section is INCOMPLETE.

> ⚠️ **MANDATORY IDEMPOTENCY RULE:** If this requirement has an SQS command handler, DDL must include either: (a) `idempotency_key UUID NOT NULL UNIQUE` on the target table, or (b) `processed_messages (message_id UUID PRIMARY KEY, processed_at TIMESTAMPTZ)` table. Specify key source in a comment.

> ⚠️ **MANDATORY TENANT ISOLATION:** Every table that stores tenant data must have a `tenant_id UUID NOT NULL` column. The tenant_id must be the first column in all composite indexes.

### Postgres DDL Template

```sql
CREATE TABLE {table_name} (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    {column_name} {TYPE} NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_{table_name}_tenant_{query_col} ON {table_name} (tenant_id, {query_col});
-- Add covering indexes for API query patterns
```

### DynamoDB Access Pattern Template

```markdown
**Table:** {TableName}
**Partition Key:** {pk} (String)
**Sort Key:** {sk} (String)

**GSI-1: {IndexName}**
- Partition Key: {field}
- Sort Key: {field}

**Access Patterns:**
1. {Pattern 1}: Query({pk})
2. {Pattern 2}: Query(GSI-1, {field})
```

### Validation

```
"Database schema for REQ-{NNN}:
- Table/access pattern: {name}
- Columns/attributes: {list}
- Primary key: {key}
- Indexes: composite (tenant_id, {query_col}) + {covering indexes}
- Constraints: {list}
- Idempotency: {column/table, key source} | N/A

Correct?"
```
