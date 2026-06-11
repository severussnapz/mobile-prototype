# SKILL: emis-principles-validation
# Phase: P03 Architecture — Phase 7

## EMIS Principles Validation (9 Principles)

**Purpose:** Validate the 9 EMIS architectural principles.

### Questions (One per principle)

1. **User Needs First:** "Does the architecture serve users?" → Product team validated?
2. **Public Cloud:** "Why AWS/Azure?" → Justification
3. **Internet First:** "Internet accessible?" → Or VPN-only with justification
4. **Web Based:** "Modern browsers?" → Support matrix
5. **Managed Services:** "Using managed vs self-hosted?" → Why self-host if applicable
6. **Native Cloud:** "AWS native vs third-party?" → Justification for third-party
7. **Reuse:** "Checked Architectural Landscape?" → Already covered in Phase 5
8. **AWS WAF:** "Meets pillars?" → Already covered in Phase 6
9. **Documentation:** "Per EMIS standards?" → OpenAPI, diagrams, runbooks

### Principles Template

```markdown
### EMIS Principles

| Principle | Status | Notes |
|-----------|--------|-------|
| 1. User Needs First | ✅ | {Evidence} |
| 2. Public Cloud | ✅ | AWS eu-west-2 |
| 3. Internet First | ✅ | {Public/internal with justification} |
| 4. Web Based | ✅ | {Browser matrix} |
| 5. Managed Services | ✅ | {ECS, RDS, SQS} |
| 6. Native Cloud | ✅ | {AWS-native vs exceptions} |
| 7. Reuse | ✅ | {Integrations from Phase 5} |
| 8. AWS WAF | ✅ | From Phase 6 |
| 9. Documentation | ✅ | OpenAPI, Mermaid, runbooks |
```

### Validation Format

```
"EMIS Principles: {9/9 ✅} or {X/9 with justified exceptions}

Correct?"
```
