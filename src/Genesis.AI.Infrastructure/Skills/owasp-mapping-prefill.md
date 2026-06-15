# SKILL: owasp-mapping-prefill
# Phase: P08 Security — Phase 3

## OWASP Top 10 Pre-fill Mapping

**Purpose:** Pre-populate OWASP Top 10 mapping for each requirement based on technology stack.

### OWASP Top 10 (2021) — NHS Clinical System Relevance

| OWASP | Category | Relevance to NHS clinical | Key control |
|-------|---------|--------------------------|-------------|
| A01 | Broken Access Control | CRITICAL — IDOR on patient records | Auth policy on all endpoints (SEC-001) |
| A02 | Cryptographic Failures | HIGH — PHI at rest / in transit | TLS 1.3, AES-256 at rest |
| A03 | Injection | HIGH — SQL, NoSQL, command injection | Parameterised queries (SEC-002) |
| A04 | Insecure Design | MEDIUM — design-time decisions | STRIDE per requirement |
| A05 | Security Misconfiguration | HIGH — exposed endpoints, verbose errors | Headers middleware, no debug in prod |
| A06 | Vulnerable Components | MEDIUM — outdated NuGet/npm packages | Dependency scanning in CI |
| A07 | Auth Failures | CRITICAL — patient data access | JWT scope validation (AUTH-004) |
| A08 | Software Integrity | MEDIUM — supply chain | SC-NNN (supply chain guardrails) |
| A09 | Logging Failures | HIGH — audit trail | Structured logging (OBS-002), no PHI (SEC-003) |
| A10 | SSRF | MEDIUM — internal service calls | Validate URLs, allowlist internal services |

### Pre-fill Output

For each requirement, auto-assess which OWASP categories apply based on its API contract and data types:

```markdown
### OWASP Top 10 Mapping

| OWASP | Applicable | Control designed |
|-------|-----------|-----------------|
| A01 Broken Access Control | Yes | SEC-001 + {policy name} |
| A02 Cryptographic Failures | {Yes/No} | {AES-256 at rest / TLS 1.3 / N/A} |
| A03 Injection | {Yes/No} | {SEC-002 parameterised queries / N/A} |
| A07 Auth Failures | Yes | AUTH-004 + {scope name} |
| A09 Logging Failures | Yes | SEC-003 no PHI + OBS-002 structured |
```
