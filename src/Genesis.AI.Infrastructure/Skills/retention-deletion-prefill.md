# SKILL: retention-deletion-prefill
# Phase: P07 Information Governance — Phase 3

## Retention and Deletion — Pre-fill

**Purpose:** Define retention periods and deletion mechanisms for all personal data.

### NHS Retention Schedule Reference

| Data Type | Minimum Retention | Maximum Retention | Legal basis |
|-----------|-----------------|-----------------|-------------|
| Adult clinical records | 8 years after last contact | Indefinite (clinical need) | Records Management Code of Practice (NHSE 2021) |
| Children's records | Until age 26 (or 8 years after death) | As above | RMCOP |
| Research data | Per ethics approval | Per ethics approval | Research governance |
| Administrative data | 7 years | 7 years | Finance Act |
| System audit logs | 7 years | 10 years | Clinical governance |

> ⚠️ **RESTRICTION:** Do NOT shorten NHS retention periods based on GDPR storage limitation unless DPO has explicitly confirmed an exception. NHS records retention is governed by the Records Management Code of Practice, not general GDPR minimisation rules.

### Deletion Mechanism Design

For each data type requiring deletion:
1. Specify the trigger: "Delete when {condition} (e.g. retention period expires)"
2. Specify the mechanism: soft delete (flag), hard delete, anonymisation
3. Specify the cascade: "Also delete {related records}"

### Retention Template

```markdown
### Retention and Deletion

| Data Type | Retention Period | Trigger | Mechanism | Cascade |
|-----------|----------------|---------|-----------|---------|
| {Clinical record} | 8 years post-last-contact | Scheduled job | Soft delete | Related attachments |
| {Audit log} | 7 years | Scheduled job | Archive to cold storage | None |
| {User session} | 24 hours | On logout / expiry | Hard delete | None |
```
