# SKILL: failure-modes-method
# Phase: P03 Architecture — Phase 4

## Failure Modes & Resilience Analysis

**Purpose:** Identify failure scenarios and specify resilience patterns per requirement or service.

### Questions (For EACH requirement or service)

1. "Critical failure scenarios?" → Database down, API timeout, service unavailable
2. "For each failure, resilience pattern?" → Circuit breaker, retry, fallback, graceful degradation
3. "Recovery procedure?" → Automatic, manual, alerting
4. "SLA/SLO targets?" → Availability %, error rate, recovery time

### Document 3–5 Critical Failures Per Requirement

### Failure Modes Template

```markdown
### Failure Modes & Resilience

| Scenario | Pattern | Recovery | SLO Impact |
|----------|---------|----------|------------|
| DB unavailable | Circuit breaker (3 fails → open 30s) | Auto-recovery → 503 | p99 affected |
| External API timeout | Retry exponential backoff (3x) → cached fallback (5min TTL) | Automatic | Degraded |
| Service unavailable | Graceful degradation → queued retry | Manual alert → PagerDuty | SLO breach at 99.9% |
```

### Validation Format

```
"REQ-{NNN} failure modes:
- DB unavailable: Circuit breaker (3 fails → open 30s) → 503 response → Auto-recovery
- External API timeout: Retry exponential backoff (3x) → Cached fallback (5min TTL)
- {More scenarios...}

Correct?"
```
