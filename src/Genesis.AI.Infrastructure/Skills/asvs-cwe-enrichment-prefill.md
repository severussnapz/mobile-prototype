# SKILL: asvs-cwe-enrichment-prefill
# Phase: P08 Security — Phase 3.5 (AUTO)

## ASVS and CWE Enrichment Pre-fill

**Purpose:** Auto-map threats to ASVS controls and CWE identifiers for formal security documentation.

> 🤖 **AUTO PHASE:** Runs after OWASP mapping is confirmed. No user input required per requirement.

### ASVS Mapping (Level 2/3)

| Threat type | ASVS Chapter | Key controls |
|------------|-------------|-------------|
| Authentication | V2 | 2.1 (passwords), 2.2 (MFA), 2.7 (OOB verifiers) |
| Session | V3 | 3.3 (logout), 3.4 (cookie security) |
| Access Control | V4 | 4.1 (design), 4.2 (operation) |
| Input Validation | V5 | 5.1 (input validation), 5.3 (output encoding) |
| Cryptography | V6 | 6.2 (algorithms), 6.3 (key management) |
| Error Handling | V7 | 7.1 (log content), 7.4 (error handling) |
| Data Protection | V8 | 8.1 (data classification), 8.3 (sensitive private data) |
| API | V13 | 13.1 (generic security), 13.2 (RESTful) |

### CWE Cross-Reference (Common)

| CWE | Description |
|-----|-------------|
| CWE-89 | SQL Injection |
| CWE-79 | XSS |
| CWE-639 | IDOR |
| CWE-311 | Missing encryption |
| CWE-532 | PHI in logs |
| CWE-284 | Improper access control |

### Auto-Log

```
"ASVS/CWE enrichment for REQ-{NNN}: {N} ASVS controls mapped, {M} CWEs referenced."
```
