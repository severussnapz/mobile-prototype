# SKILL: security-check-authoring
# Phase: P08 Security — Phase 4 (AUTO)

## Security Check Authoring

**Purpose:** Auto-generate the formal security check section for each requirement file.

> 🤖 **AUTO PHASE:** Runs after attack vector checklist passes.

### Security Check Template

```markdown
## Security (Added by Pipeline 08)

### Threat Register

| Threat | STRIDE | CVSS | Status |
|--------|--------|------|--------|
| THR-NNN: {title} | {S|T|R|I|D|E} | {Critical/High/Medium/Low} | Controlled |

### Security Controls

| Control | Type | Mitigates | Implementation |
|---------|------|-----------|---------------|
| SEC-CTRL-NNN | PREVENT | THR-NNN | [Authorize(Policy="{policy}")] |
| SEC-CTRL-NNN | DETECT | THR-NNN | Structured audit log entry |

### OWASP Top 10

| OWASP | Applicable | Control |
|-------|-----------|---------|
| A01 | Yes | Auth policy on all endpoints |
| A07 | Yes | JWT scope validation |

### ASVS Level: {1/2/3}

| ASVS Chapter | Controls applied |
|-------------|----------------|
| V2 Auth | {list} |
| V4 Access Control | {list} |
```
