# SKILL: ig-control-mapping
# Phase: P07 Information Governance — Phase 4

## IG Control Mapping

**Purpose:** Map personal data processing activities to technical and organisational controls.

### Standard IG Controls (Apply to All Clinical Systems)

| Control | Type | Description |
|---------|------|-------------|
| IG-CTRL-001 | Technical | Access control — role-based, least privilege |
| IG-CTRL-002 | Technical | Data in transit — TLS 1.2+ minimum |
| IG-CTRL-003 | Technical | Data at rest — encryption (AES-256) |
| IG-CTRL-004 | Technical | Audit logging — all access/modification of personal data |
| IG-CTRL-005 | Technical | Pseudonymisation — where re-identification is not needed |
| IG-CTRL-006 | Organisational | Data sharing agreements — for all third-party transfers |
| IG-CTRL-007 | Organisational | Staff training — annual GDPR/Caldicott training |
| IG-CTRL-008 | Technical | Breach detection — monitoring and alerting |
| IG-CTRL-009 | Technical | Data subject rights — export and deletion tooling |
| IG-CTRL-010 | Organisational | Controllership allocation — controller/processor roles are explicit and evidenced |

### Control Mapping Template

```markdown
### IG Control Mapping

| Processing Activity | Required Controls | Status |
|--------------------|------------------|--------|
| Store patient record | IG-CTRL-001, -002, -003, -004 | Designed in P04 |
| Share data with {party} | IG-CTRL-006 | DSA required — parking lot item added |
| Process NHS number | IG-CTRL-001, -003, -005 | Pseudonymise where possible |
| Assign controller/processor roles | IG-CTRL-010 | Controller = provider organisation, processor = supplier (unless legal evidence states otherwise) |
```
