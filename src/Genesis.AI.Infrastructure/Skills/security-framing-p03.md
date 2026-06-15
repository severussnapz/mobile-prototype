# SKILL: security-framing-p03
# Phase: P03 Architecture — Phase 10

## Security Architecture Framing

**Purpose:** Define the security requirements, trust boundaries, and evidence expectations before implementation begins. This phase is MANDATORY per requirement — never skipped regardless of service_scope. Security framing is requirement-specific and cannot be pre-filled.

### Security Framing Questions (Per Requirement)

Ask these per requirement (one at a time):

1. "What data is handled, and what is the trust boundary?"
2. "Which actors and roles need least-privilege access?"
3. "What authentication and authorisation model applies?"
4. "Where are secrets, tokens, keys, and credentials stored and rotated?"
5. "What input surfaces exist, and what validation/encoding rules apply?"
6. "What is the safe failure mode if auth, validation, or downstream calls fail?"
7. "What encryption is required in transit and at rest, and for which data?"
8. "What logging, audit, and alerting evidence is required for security-significant events?"
9. "What CI/CD, dependency, and supply-chain risks must be blocked?"
10. "What abuse cases and negative tests must exist before Pipeline 08 reviews?"
11. "URL construction standard?" → Any user-supplied value interpolated into a URL path or query string MUST be wrapped with `encodeURIComponent()`. Create ADR.

### URL Construction Rule (Non-Negotiable)

```typescript
// ❌ PROHIBITED — raw interpolation of user/API data into URLs
const url = `/api/consultations/${consultationId}/consent`;

// ✅ REQUIRED — encodeURIComponent() on all user-supplied values
const url = `/api/consultations/${encodeURIComponent(consultationId)}/consent`;

// ✅ ALLOWED — UPPER_SNAKE_CASE constants are exempt (compile-time)
// ✅ ALLOWED — import.meta.env / process.env are exempt
```

### Security Template

```markdown
### Security

**Trust boundary:** {Description}
**Auth:** Users ({CIS2/Azure AD}), Services (IAM roles)
**Encryption:** Rest (KMS for {data}), Transit (TLS 1.2+)
**Network:** VPC, private subnets, security groups
**Controls:** WAF ✅, Shield ✅, GuardDuty ✅
**Secrets:** approved secret store ✅, rotation ✅, no hardcoded secrets ✅
**Negative tests required:** authz denial ✅, IDOR ✅, injection ✅, audit evidence ✅
**Security framing notes:** {Per-requirement specifics}
```

### Validation Format

```
"Security for REQ-{NNN}:
- Auth: Users ({CIS2/Azure AD}), Services (IAM roles)
- Encryption: Rest (KMS for {data}), Transit (TLS 1.2+)
- Network: VPC, private subnets, security groups
- Tests: authz denial ✅, IDOR ✅, injection ✅, audit evidence ✅

Correct?"
```
