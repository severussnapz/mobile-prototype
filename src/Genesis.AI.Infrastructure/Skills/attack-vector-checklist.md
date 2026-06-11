# SKILL: attack-vector-checklist
# Phase: P08 Security — Phase 3.6 (AUTO)

## Attack Vector Checklist

**Purpose:** Auto-verify that no common attack vectors are missed per API endpoint.

> 🤖 **AUTO PHASE:** Runs after ASVS enrichment. No user input required.

### Per-Endpoint Attack Vector Checklist

For each endpoint in the API contract (P04):

| Vector | Check | Control |
|--------|-------|---------|
| Missing auth | `[Authorize]` attribute present? | SEC-001 |
| Wrong scope | Correct policy with right scopes? | AUTH-004 |
| IDOR | TenantId / user ownership check in handler? | SEC-001 (A01) |
| Mass assignment | `[BindNever]` or allowlist on request model? | Input validation |
| Injection | EF Core parameterised? No raw SQL? | SEC-002 |
| PHI in logs | No patient identifiers in log statements? | SEC-003 |
| PHI in error response | Error messages don't expose PHI? | A02/SEC-003 |
| Verbose errors in prod | No stack traces in API responses? | OBS-003 |
| Rate limiting | Endpoint has rate limit for public/unauthenticated paths? | A04/A05 |

### Auto-Log

```
"Attack vector checklist for REQ-{NNN}: {N}/{M} checks passed.
Issues: {list or 'None'}"
```

If issues found: create HIGH parking lot item per issue.
