# SKILL: ig-check-authoring
# Phase: P07 Information Governance — Phase 4

## IG Check Authoring

**Purpose:** Write formal IG checks into requirement files.

### IG Check Template

For each requirement, write:

```markdown
### Information Governance Check — REQ-{NNN}

| Check | Result | Evidence |
|-------|--------|---------|
| Lawful basis confirmed | {Yes / No — {basis}} | {DPIA reference / DPO confirmation} |
| Data classification | {Special Category / Personal / Pseudonymous / Anonymous} | {Routing context / P06 assessment} |
| Data minimisation | {Passed / {fields removed}} | See data minimisation section |
| Retention period | {N years / {period}} | {RMCOP reference / DPO confirmation} |
| Deletion mechanism | {Scheduled job / User-triggered / Anonymisation} | P04 design |
| Access controls | {Role-based — {policy names}} | P04 auth design |
| Encryption | {At rest: AES-256 / In transit: TLS 1.3} | Infrastructure / P03 ADR |
| Audit logging | {Yes — all personal data access/modification} | P04 / P03 design |
| Third-party transfers | {None / {parties} — DSA required} | {DSA reference or parking lot} |
```
