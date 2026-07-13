# Skill: Data Modelling & Migration Discipline

**Apply whenever:** adding or changing a table, column, index, or constraint; designing an aggregate's persistence; writing any Flyway migration; or planning a schema change against a system that cannot stop. The schema outlives every service that reads it — model accordingly.

---

## Modelling judgement

- **Model the invariant, then the access.** Start from what must always be true (one pin per role per manifest; artefact versions immutable; soft-deleted rows excluded everywhere) and make the schema enforce it — unique constraints, FKs, NOT NULL — rather than trusting application code alone. The DB constraint is the last guard when a bug or a repair script bypasses the aggregate. `UNIQUE (project_id, version)` and `UNIQUE (manifest_id, role)` are invariants made structural.
- **Normalise by default; denormalise with a receipt.** Duplicate data only for a *measured* read-path need, and record where the duplication lives and what keeps it consistent. Every denormalisation is a standing consistency bug you've chosen to manage.
- **Value snapshot vs live reference — decide explicitly.** A foreign key means "always the current state of that row"; copied fields mean "the state as of this moment." Pins, audit records, and anything provenance-shaped are snapshots (the manifest stores filePath+version as values, deliberately not an FK-following-latest); operational relationships are FKs. Choosing by accident produces either audit trails that mutate or references that stale.
- **Immutability as a modelling tool**: append-only versioned rows (the artefact pattern — new version per change, latest-wins queries) buy you history, auditability, and trivial concurrency at the cost of storage. In a regulated system that trade is almost always right for anything a human approves. Mutable-in-place is for genuinely operational state.
- **Soft deletes are the house rule** (`IsDeleted`, no hard deletes on domain entities) — which means *every* query path must filter them, ideally via a global query filter, not per-query discipline. And know the exception: data-protection erasure obligations may require true deletion paths — design where those live before someone improvises one.
- **Name the growth dimension of every new table** (per-project, per-artefact-version, per-message?) in the design, with the index story for its dominant queries. This is where data modelling meets capacity (see performance-capacity.md).

## Migration discipline

- **One migration, one purpose, sequentially numbered** (`Vnn__description.sql`), matching the entity configuration *exactly* — every `HasColumnName` has its column, every property mapped. Config/migration drift is a seam failure caught only at runtime.
- **Check the latest number on disk, not in notes**, before authoring — a remembered "latest is V23" when V24 exists on disk is a collision (this happened; verify-before-claim.md applies).
- **Migrations are immutable once merged.** A wrong migration gets a new corrective migration, never an edit — environments that already ran it can't re-run history.
- **Test the migration against realistic data**, not an empty schema: an `ALTER TABLE ... NOT NULL` that's instant on 0 rows locks a 10M-row table for minutes. Know your migration tool's locking behaviour for each operation class.

## Zero-downtime schema evolution (expand–migrate–contract)

Any change to a table that live code is reading requires the three-phase dance, because deploy and migrate are never truly atomic and rollback must stay possible:

1. **Expand** — add the new structure alongside the old (new nullable column, new table). Old code ignores it; new code writes both/reads either. Never rename in place; a rename is an add + a later remove.
2. **Migrate** — backfill data, flip reads to the new structure, verify. Backfills on big tables run batched, resumable, and off-peak.
3. **Contract** — only after every reader is confirmed on the new structure (and a rollback window has passed): remove the old column/table in its own migration.

The compatibility rule that falls out: **every migration must be compatible with the app version on either side of it** — the previous app version must run against the migrated schema (or rollback is impossible), and the new app version against the un-migrated schema (or deploy ordering becomes load-bearing). Destructive operations (drop, rename, type-narrow) are only ever legal in a contract phase, never bundled with the expand.

## Repair scripts are migrations in disguise

Any manual data correction against a shared environment gets migration-grade treatment: written down, reviewed, tested against a copy, executed with a logged audit trail of what changed. Ad-hoc UPDATE statements against production are how systems of record stop being systems of record (see incident-response-operations.md on data incidents).
