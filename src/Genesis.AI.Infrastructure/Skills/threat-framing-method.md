# SKILL: threat-framing-method
# Phase: P08 Security — Phase 1

## Threat Framing Method

**Purpose:** Identify and prioritise security threats using STRIDE for each requirement.

### Fast-Track Rule

If ROUTING CONTEXT `security_framing_present: true` AND P03 included STRIDE analysis for this requirement: delta only — identify NEW threats introduced since P03.

### STRIDE Threat Categories

| Category | Threat | Question |
|---------|--------|---------|
| Spoofing | Identity forgery | "Can an attacker impersonate a legitimate user or system?" |
| Tampering | Data modification | "Can an attacker modify data in transit or at rest?" |
| Repudiation | Deny actions | "Can a user deny performing an action?" |
| Info Disclosure | Data leak | "Can an attacker access data they should not?" |
| Denial of Service | Availability | "Can an attacker make the service unavailable?" |
| Elevation of Privilege | Unauthorised access | "Can an attacker gain higher permissions than authorised?" |

### For Each Requirement

Ask all 6 STRIDE questions. For each "Yes":
1. "What is the attack vector?" (how would this happen?)
2. "What is the impact?" (what data/function is affected?)
3. "CVSS severity estimate?" (Critical/High/Medium/Low)

### Threat Template

```markdown
**THR-{NNN}** — {Threat title}

**STRIDE category:** {S|T|R|I|D|E}
**Attack vector:** {How attacker would exploit this}
**Impact:** {What is compromised}
**CVSS estimate:** {Critical | High | Medium | Low}
**Requirement:** REQ-{NNN}
```
