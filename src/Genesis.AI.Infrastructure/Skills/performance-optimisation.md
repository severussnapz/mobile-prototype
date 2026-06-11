# SKILL: performance-optimisation
# Phase: P04 Design — Phase 10

## Performance Optimisation

**Purpose:** Design caching, indexing, and query optimisation for this requirement.

### Questions

1. "Are any queries likely to be hot paths?" → Identify high-frequency read endpoints
2. "Is caching appropriate?" → Cache TTL, invalidation strategy, what to cache
3. "Are the indexes from Phase 2 sufficient for the expected query volume?"
4. "Are there any N+1 query risks?" → Use `Include()` judiciously; prefer joins over separate queries

### Query Performance Rules (DATA-004)

- Read queries MUST use `AsNoTracking()` — never tracked for reads
- No N+1 patterns — use `Include()` only where the related entity is needed in the response
- Pagination MUST be applied to all list endpoints — never return unbounded result sets
- Prefer database-side filtering over in-memory filtering

### Caching Patterns

**In-memory cache** (for reference data, short TTL):
```csharp
services.AddMemoryCache();
// Usage: IMemoryCache with absolute expiry
```

**Distributed cache** (DynamoDB or Redis for session/user-specific data):
```csharp
services.AddDistributedCache();
```

### Performance Template

```markdown
### Performance Optimisation

**Hot paths:** {list of high-frequency endpoints}
**Caching:** {What is cached, TTL, invalidation trigger}
**Index review:** {Confirm indexes from Phase 2 cover all query patterns}
**N+1 risk:** {None identified | Addressed by Include() on {relationship}}
**Pagination:** {Page size default, max page size}
```
