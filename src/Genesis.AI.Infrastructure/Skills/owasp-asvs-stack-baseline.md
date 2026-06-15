# SKILL: owasp-asvs-stack-baseline
# Phase: P08 Security — Phase 0

## OWASP ASVS Stack Baseline

**Purpose:** Pre-load the OWASP ASVS level and EMIS-X security guardrails relevant to the technology stack.

### ASVS Level Selection (from P03 Service Classification)

| Service type | Default ASVS Level |
|-------------|-------------------|
| NHS clinical (patient data) | Level 3 (strict) |
| NHS administrative | Level 2 (standard) |
| Internal tooling / developer tooling | Level 1 (opportunistic) |

### EMIS-X Security Guardrails (Pre-loaded)

Critical guardrails that MUST be satisfied — from `emis-x-api-security`:

| Ref | Rule |
|-----|------|
| SEC-001 | Every endpoint must have `[Authorize(Policy = "...")]` |
| SEC-002 | Parameterised queries only — no string concatenation SQL |
| SEC-003 | No PHI/PII in log statements |
| SEC-004 | All configuration from environment variables — no hardcoded secrets |

From `emis-x-webapp-security`:

| Ref | Rule |
|-----|------|
| WSEC-001 | No PHI in localStorage/sessionStorage/URLs |
| WSEC-002 | Access tokens from `getAccessToken()` — never stored in state |
| WSEC-003 | `dangerouslySetInnerHTML` prohibited |
| WSEC-004 | Inputs must be sanitised before display |

### Stack-Specific Baseline

Log: "Security baseline loaded. ASVS Level: {1/2/3}. Stack: {API type / Frontend type}."
