# SKILL: output-write-protocol
# Phase: P04 Design — Phase 12 / P06 — Phase 12

## Output Write Protocol

> 📝 **WRITE NOW — MANDATORY:** For each requirement, write the Design section to the REQ file **one at a time**. After writing: log `"✅ REQ{N} Design section written ({M}/{TOTAL} complete). Moving to REQ{N+1}."` Then discard from working context before processing the next requirement. Do NOT batch multiple requirements in memory before writing.

## What to Write

Write `## Design (Added by Pipeline 04)` section to each requirement file containing:

1. `### API Contract` — all endpoints with auth, request/response types, status codes
2. `### Database Schema` — DDL or DynamoDB access patterns
3. `### Component Interfaces` — C# interfaces and frontend component specs
4. `### State Machine` — if applicable, else note "No state machine required"
5. `### Data Validation` — validator class names and rules
6. `### Error Handling` — exception types and HTTP status mappings
7. `### Integration Contracts` — external DTO mappings
8. `### Data Migration` — Flyway migration file and type
9. `### Testing Strategy` — test matrix
10. `### Performance Optimisation` — caching, indexes, N+1 review
11. `### API Documentation` — Swagger annotation requirements

## After Writing

Update `feedback/P04_REVIEW_LIST.md` to mark Written as `✅` for the row.

## No-Placeholder Rule

See `no-placeholder-enforcement` skill — never write TBD, TODO, or placeholder values to a REQ file.
