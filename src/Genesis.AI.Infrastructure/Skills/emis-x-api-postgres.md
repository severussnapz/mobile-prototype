---
name: emis-x-api-postgres
description: >
  Use this skill when generating, reviewing, or auditing Flyway SQL
  migrations, PostgreSQL table definitions, naming conventions, data types,
  or database registration code for databases the service owns. Covers
  PG-001 through PG-007.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-api
    - requirements
---

# EMIS-X PostgreSQL Guardrails

Guardrails for EMIS-X microservices that **own** a PostgreSQL database. Apply during code generation and code review.

**Target versions:** PostgreSQL 17.x, Flyway 11.x, Npgsql EF Core Provider 10.0, Entity Framework Core 10.0. Do not use `UseSnakeCaseNamingConvention()` — map columns explicitly per-property.

These guardrails do **not** apply to external databases the service connects to but does not own. For shared data access guardrails (DbContext, repository pattern, entity configuration), see the **emis-x-api-data-access** skill.

## Guardrails Index

| Guardrail | Name                             | Severity |
| --------- | -------------------------------- | -------- |
| PG-001    | Flyway Migration Naming          | High     |
| PG-002    | Naming Conventions               | High     |
| PG-003    | Data Type Mapping                | Medium   |
| PG-004    | Data Integrity Constraints       | High     |
| PG-005    | Indexing                         | Medium   |
| PG-006    | DbContext Registration           | High     |
| PG-007    | Column Identifier Naming         | Medium   |
| PG-008    | Acceptable PostgreSQL extensions | High     |

---

## PG-001: Flyway Migration Naming

**Type:** Guardrail

**Requirement:** Flyway migration files must use **sequential numbering** with this format:
- Versioned: `V{version}__{snake_case_description}.sql` where version is either `{major}_{minor}` (e.g. `V1_2__`) or a single number (e.g. `V1__`)
- Repeatable: `R__{snake_case_description}.sql`

Use double underscore (`__`) between version and description. Descriptions must be **snake_case**. Migrations must be idempotent where possible (use `IF NOT EXISTS`, `IF EXISTS`). Never modify existing migrations — they are immutable once applied.

**Severity:** High

**Exceptions:** None. Do not use timestamp-based versioning.

✅ **Good:**

```
V1__initial_setup.sql
V1_1__setup_database.sql
V1_2__create_scope_schema.sql
V1_3__create_role_schema.sql
V1_4__create_user_schema.sql
V1_70__varcharn_type_conversion.sql
V1_140__create_job_category_tables.sql
R__function_get_users_for_simple_view.sql
R__trigger_create_role_scope_verify_scope.sql
```

❌ **Bad:**

```
V20250101120000__CreateGreetingsTable.sql   -- Timestamp versioning, PascalCase
001_create_greetings.sql                    -- No Flyway V prefix
V1__create greetings table.sql              -- Spaces in description
V1__CreateGreetingsTable.sql                -- PascalCase description
```

---

## PG-002: Naming Conventions

**Type:** Guardrail

**Requirement:** All database identifiers must use `snake_case`. Table names for **new** tables must be **singular**. Constraints do not need to be explicitly named — PostgreSQL assigns appropriate names automatically — but if named, they should use the patterns below.

**Severity:** High

**Exceptions:** Legacy databases may use **plural** table names (e.g. `consultations`, `assessments`, `batches`). When writing migrations that **ALTER existing tables**, always use the **actual table name** from the schema — even if it does not conform to this standard. Check the existing schema or the initial migration file (e.g. `V1__consolidated_schema.sql`) to confirm the real table name before writing any `ALTER TABLE` statement.

### Naming Patterns

| Element | Convention | Example |
|---------|-----------|---------|
| Tables (new) | Singular, snake_case | `greeting`, `user`, `role_scope` |
| Columns | snake_case | `greeting_name`, `created_at`, `organisation_uuid` |
| Primary keys (if named) | `pk_{table}_{columns}` | `pk_greeting_greeting_uuid`, `pk_role_scope_role_uuid_scope_uuid` |
| Foreign keys (if named) | `fk_{table}_{column}` | `fk_greeting_response_greeting_uuid` |
| Indexes | `idx_{table}_{columns}` | `idx_greeting_name` |
| Unique indexes | `idx_uq_{table}_{columns}` | `idx_uq_greeting_name` |
| Unique constraints (if named) | `uq_{table}_{columns}` | `uq_greeting_name` |
| Check constraints (if named) | `chk_{table}_{description}` | `chk_greeting_status_valid` |

✅ **Good:**

```sql
-- Named constraints (preferred for complex schemas)
CREATE TABLE greeting (
    greeting_uuid uuid NOT NULL DEFAULT uuid_generate_v4(),
    name varchar NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_greeting_greeting_uuid PRIMARY KEY (greeting_uuid)
);

-- Unnamed constraints (acceptable — PostgreSQL auto-names them)
CREATE TABLE greeting (
    greeting_uuid uuid NOT NULL DEFAULT uuid_generate_v4(),
    name varchar NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (greeting_uuid)
);

CREATE UNIQUE INDEX idx_uq_greeting_name ON greeting (name);

ALTER TABLE greeting_response
    ADD CONSTRAINT fk_greeting_response_greeting_uuid
    FOREIGN KEY (greeting_uuid) REFERENCES greeting (greeting_uuid);
```

❌ **Bad:**

```sql
CREATE TABLE Greetings (                    -- PascalCase, plural
    Id UUID PRIMARY KEY,                    -- PascalCase column name
    GreetingName VARCHAR(100) NOT NULL      -- PascalCase column name
);
```

---

## PG-003: Data Type Mapping

**Type:** Guardrail

**Requirement:** Use appropriate PostgreSQL data types. String columns should use `varchar` (unbounded or with explicit length) or `text` — both are identical in PostgreSQL. Consider using native Enumerated Types (`CREATE TYPE ... AS ENUM`) rather than lookup tables for simple, rarely-changing value sets.

**Severity:** Medium

**Exceptions:** None.

| .NET Type | PostgreSQL Type | Notes |
|-----------|----------------|-------|
| `Guid` / Value Object ID | `uuid` | Default `uuid_generate_v4()` |
| `string` | `varchar` or `text` | Both are identical in PostgreSQL — use either. Only add `(n)` if a business rule mandates a length limit |
| `DateTime` | `timestamptz` | Always referenced to UTC — never `timestamp` |
| `decimal` | `numeric(p,s)` | Specify precision and scale for money/quantities |
| `bool` | `boolean` | Never use integer or `bit` for booleans |
| `int` | `integer` | Use `bigint` only for large ranges |
| `enum` | `CREATE TYPE ... AS ENUM` or `smallint` FK | Native enums for simple sets; lookup tables for complex/frequently-changing values |
| Full-text | `tsvector` | For search columns |
| Auto-increment | `generated by default as identity` | Preferred over `serial` for new tables |
| JSON data | `jsonb` | Never use `json` — `jsonb` is indexable, queryable, and more efficient |

### Prohibited Types

| Prohibited | Replacement | Reason |
|-----------|-------------|--------|
| `float`, `real`, `double precision` | `numeric(p,s)` | Binary floating-point introduces rounding errors for precise values (monetary, clinical) |
| `money` | `numeric(p,s)` | `money` has locale-dependent formatting and limited precision |
| `serial`, `bigserial` | `generated by default as identity` | Implicit sequence ownership causes issues during `pg_dump`/`pg_restore` |
| `timestamp` (without TZ) | `timestamptz` | Stores timestamp using local system time rather than referenced to UTC |
| `char(n)` | `varchar` or `varchar(n)` | `char(n)` pads with spaces, wastes storage, and causes comparison bugs |
| `json` | `jsonb` | `json` stores raw text, cannot be indexed, and is slower to query |
| `timetz` / `time with time zone` | `timestamptz` | Does not store a true timezone, only an offset — unreliable |
| `bit` / `bit varying` | `boolean` or `integer` | Use `boolean` for flags, `integer` for bitmasks |

### Enum Pattern (Native)

```sql
CREATE TYPE greeting_status AS ENUM ('active', 'archived', 'deleted');

CREATE TABLE greeting (
    greeting_uuid uuid NOT NULL DEFAULT uuid_generate_v4(),
    status greeting_status NOT NULL DEFAULT 'active',
    CONSTRAINT pk_greeting_greeting_uuid PRIMARY KEY (greeting_uuid)
);
```

### Enum Pattern (Lookup Table)

For enums that change frequently or need additional metadata:

```sql
CREATE TABLE greeting_status (
    greeting_status_id smallint NOT NULL,
    name varchar NOT NULL,
    CONSTRAINT pk_greeting_status_greeting_status_id PRIMARY KEY (greeting_status_id),
    CONSTRAINT uq_greeting_status_name UNIQUE (name)
);

INSERT INTO greeting_status (greeting_status_id, name) VALUES (1, 'Active'), (2, 'Archived'), (3, 'Deleted');

CREATE TABLE greeting (
    greeting_uuid uuid NOT NULL DEFAULT uuid_generate_v4(),
    greeting_status_id smallint NOT NULL DEFAULT 1,
    CONSTRAINT pk_greeting_greeting_uuid PRIMARY KEY (greeting_uuid),
    CONSTRAINT fk_greeting_greeting_status_id FOREIGN KEY (greeting_status_id) REFERENCES greeting_status (greeting_status_id)
);
```

```csharp
builder.Property(g => g.Status).HasColumnName("greeting_status_id").HasConversion<int>();
```

❌ **Bad:**

```sql
CREATE TABLE greeting (
    id SERIAL PRIMARY KEY,                   -- Use uuid, not SERIAL; bare 'id' violates PG-007
    name TEXT,                               -- TEXT is fine (identical to varchar in PostgreSQL)
    amount FLOAT,                            -- Use numeric for money
    is_active BIT DEFAULT 1,                 -- Use boolean
    metadata JSON,                           -- Use JSONB, not JSON
    country_code CHAR(2),                    -- Use varchar(2), not CHAR
    created_at TIMESTAMP DEFAULT now()       -- Use timestamptz
);
```

---

## PG-004: Data Integrity Constraints

**Type:** Guardrail

**Requirement:** Enforce data integrity at the database level. Always define NOT NULL, UNIQUE, CHECK, and FOREIGN KEY constraints with explicit names. Declare `NOT NULL` or `NULL` explicitly to show intent. Use CASCADE or RESTRICT as appropriate for referential actions.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```sql
CREATE TABLE greeting (
    greeting_uuid uuid NOT NULL DEFAULT uuid_generate_v4(),
    name varchar NOT NULL,
    message varchar NOT NULL,
    greeting_status_id smallint NOT NULL DEFAULT 1,
    active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_greeting_greeting_uuid PRIMARY KEY (greeting_uuid),
    CONSTRAINT fk_greeting_greeting_status_id FOREIGN KEY (greeting_status_id) REFERENCES greeting_status (greeting_status_id),
    CONSTRAINT uq_greeting_name UNIQUE (name)
);
```

❌ **Bad:**

```sql
CREATE TABLE greeting (
    id uuid PRIMARY KEY,              -- Bare 'id' (PG-007); unnamed constraint
    name varchar,                     -- NULL allowed without intent
    status_id smallint                -- No FK, no default
);
```

---

## PG-005: Indexing

**Type:** Guardrail

**Requirement:** Create indexes for columns used in WHERE clauses, JOIN conditions, and ORDER BY. Validate with `EXPLAIN ANALYZE`. Use partial indexes and GIN indexes where appropriate.

**Severity:** Medium

**Exceptions:** Small lookup tables may not need additional indexes beyond their primary key.

✅ **Good:**

```sql
-- Standard index
CREATE INDEX idx_greeting_organisation_uuid ON greeting (organisation_uuid);

-- Composite index
CREATE INDEX idx_role_organisation_uuid_name ON role (organisation_uuid, name);

-- Unique index
CREATE UNIQUE INDEX idx_uq_greeting_name ON greeting (name);

-- Partial unique index (conditional uniqueness)
CREATE UNIQUE INDEX idx_uq_user_in_organisation_is_default
    ON user_in_organisation (user_uuid) WHERE organisation_is_default;

-- GIN index for full-text search
CREATE INDEX idx_job_category_search_vector ON job_category USING gin (search_vector);
```

---

## PG-006: DbContext Registration

**Type:** Guardrail

**Requirement:** Register the DbContext with `UseNpgsql` and the connection string from configuration. Do **not** use `UseSnakeCaseNamingConvention()` — all snake_case mapping is done explicitly per-property in entity configuration classes via `.HasColumnName()`.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
services.AddDbContext<GreetingContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
```

❌ **Bad:**

```csharp
// UseSnakeCaseNamingConvention — we map columns explicitly per-property
services.AddDbContext<GreetingContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());
```

## PG-007: Column Identifier Naming

**Type:** Guardrail

**Requirement:** Do not name columns `id`, `guid`, or `uuid` — always prefix with the entity name **and** use a suffix that matches the data type: `_uuid` for `uuid` columns, `_id` for integer identity columns, `_guid` for external GUID references. Bare identifiers are ambiguous in queries with joins and make column provenance unclear. Type-matching suffixes make the schema self-documenting — a developer reading `greeting_uuid` immediately knows both the entity and the data type.

**Severity:** Medium

**Exceptions:** None.

### Suffix Convention

| Data Type | Suffix | Example |
|-----------|--------|--------|
| `uuid` | `_uuid` | `greeting_uuid`, `organisation_uuid` |
| `integer` / `smallint` / `bigint` | `_id` | `greeting_status_id`, `sequence_id` |
| External GUID reference | `_guid` | `external_system_guid` |

✅ **Good:**

```sql
CREATE TABLE user_account (
    user_account_uuid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organisation_uuid uuid NOT NULL,
    role_id smallint NOT NULL,
    CONSTRAINT pk_user_account_user_account_uuid PRIMARY KEY (user_account_uuid)
);
```

❌ **Bad:**

```sql
CREATE TABLE user_account (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),     -- Bare 'id' — use user_account_uuid
    user_id uuid NOT NULL,                           -- Wrong suffix — uuid columns use _uuid not _id
    guid uuid NOT NULL,                              -- Bare 'guid' — use external_system_guid
    uuid uuid NOT NULL                               -- Bare 'uuid' — use user_account_uuid
);
```

## PG-008: Acceptable PostgreSQL extensions

**Type:** Guardrail

**Requirement:** Only use approved PostgreSQL extensions that are available by Amazon Aurora PostgreSQL as documented on [this page](https://docs.aws.amazon.com/AmazonRDS/latest/AuroraPostgreSQLReleaseNotes/AuroraPostgreSQL.Extensions.html). Use extensions where necessary to support a required feature (e.g. `uuid-ossp` for `uuid_generate_v4()`, `pg_stat_statements` for query performance monitoring) but do not add extensions without a specific need. Unnecessary extensions increase the attack surface and maintenance overhead of the database.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";  -- Required for uuid_generate_v4()
```

❌ **Bad:**

```sql
CREATE EXTENSION IF NOT EXISTS "pg-ulid";    -- ULID extension not supported on Amazon Aurora PostgreSQL
```

---

## Flyway Migration Template

```sql
-- V1_{seq}__{description}.sql

CREATE TABLE IF NOT EXISTS {table_name} (
    {table_name}_uuid uuid NOT NULL DEFAULT uuid_generate_v4(),
    -- columns
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY ({table_name}_uuid)
);

CREATE INDEX IF NOT EXISTS idx_{table_name}_{column} ON {table_name} ({column});
```

---

## Gotchas

- Table names for **new** tables are **singular** (`greeting`, not `greetings`). Agents default to plural because Rails, Django, and most ORMs use plural table names. EMIS-X PostgreSQL uses singular. However, some legacy databases predate this standard and use **plural** table names. When writing `ALTER TABLE` or `CREATE INDEX ... ON` statements against an existing table, always verify the actual table name in the schema first — do not assume singular.
- `varchar` without a length (`varchar`, not `varchar(255)`) is the standard for string columns. Only add a length constraint when a specific business rule mandates it. Agents will add `varchar(255)` by default because most database tutorials use it. `text` is also acceptable — PostgreSQL treats `text` and `varchar` identically.
- `timestamp` (without timezone) is banned — always use `timestamptz`. `timestamp` silently drops timezone information and stores the local date/time only, while `timestamptz` stores the timestamp referenced to UTC and converts on input and output.
- `timestamptz` is preferred but despite the name it ddoes not actually store a timezone — it stores a UTC timestamp and converts to/from the client's local time on input/output. If it is important to know the local time zone of the record (e.g. for scheduling), store the timezone separately in a `varchar` column (e.g. `user_timezone varchar`) and use that for conversions in application code.
- `serial` is banned for new tables — use `generated by default as identity` instead. `serial` creates an implicit sequence with ownership that behaves unexpectedly during `pg_dump`/`pg_restore`.
- `char(n)` is banned — use `varchar` or `varchar(n)` instead. `char(n)` pads values with spaces, wastes storage, and causes subtle comparison bugs.
- `json` is banned — use `jsonb` instead. `json` stores raw text and cannot be indexed or efficiently queried. `jsonb` is binary, indexable, and faster.
- `timetz` / `time with time zone` is banned — it does not store a true timezone, only an offset. Use `timestamptz` instead.
- `bit` / `bit varying` is banned — use `boolean` for flags or `integer` for bitmasks.
-  Constraints do not need to be explicitly named — PostgreSQL assigns appropriate names automatically. Named constraints are preferred for complex schemas but unnamed constraints are acceptable.
- Column identifiers must be prefixed with the entity name and suffixed to match the data type — `_uuid` for uuid columns, `_id` for integer identity columns (e.g. `user_uuid` not `id`; `greeting_uuid` not `greeting_id` when the column is uuid). See PG-007. Bare `id`/`guid`/`uuid` columns are ambiguous in joins.
- Flyway migration filenames use **double** underscores between version and description (`V1_1__description`, not `V1_1_description`). A single underscore is treated as part of the version number and Flyway will fail to parse it.
- Both `V{major}_{minor}__` and `V{n}__` versioning formats are valid Flyway syntax.
- Never modify an existing Flyway migration — they are immutable once applied. Flyway checksums the file content; changing a deployed migration causes a checksum mismatch error on the next deployment.


