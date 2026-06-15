# SKILL: control-strategy-method
# Phase: P08 Security — Phase 2

## Control Strategy Method

**Purpose:** Design security controls for each identified threat.

### Control Categories

| Type | Description | Example |
|------|-------------|---------|
| PREVENT | Stops the attack | Auth policy, input validation, parameterised queries |
| DETECT | Identifies the attack | Audit logging, anomaly detection, rate limiting alerts |
| RESPOND | Limits damage | Circuit breaker, revoke token, quarantine |
| RECOVER | Restores normal operation | Backup restore, replay from event log |

### Required Controls for EMIS-X API

**For every endpoint (SEC-001):**
```csharp
[Authorize(Policy = "{PolicyName}")]
```

**For every database query (SEC-002):**
```csharp
// Parameterised via EF Core — no raw SQL interpolation
await _dbContext.{Table}.Where(entity => entity.Id == entityId).ToListAsync(ct);
```

**For every log statement (SEC-003):**
```csharp
// NEVER: _logger.LogInformation("Patient {NhsNumber} accessed", nhsNumber);
// ALWAYS: _logger.LogInformation("Patient record accessed. PatientId: {PatientId}", patientId);
```

### Control Template

```markdown
**SEC-CTRL-{NNN}** — {Control title}

**Type:** {PREVENT | DETECT | RESPOND | RECOVER}
**Mitigates:** THR-{NNN}
**Implementation:** {Exact code pattern or system configuration}
**EMIS guardrail:** {SEC-NNN / WSEC-NNN / none}
```
