# SKILL: operations-monitoring
# Phase: P03 Architecture — Phase 8

## Operations & Monitoring

**Purpose:** Define deployment pipeline, logging, monitoring, and alerting.

### Questions

1. "Deployment pipeline?" → GitHub Actions workflow, stages
2. "Deployment strategy?" → Blue/green, canary, rolling (from Phase 6)
3. "Logs?" → Application (CloudWatch), access (ALB), audit (CloudTrail)
4. "Metrics?" → Business metrics, technical metrics, custom (OTEL)
5. "Critical alerts?" → Error rate, latency, availability thresholds
6. "Alert destinations?" → PagerDuty, Slack, email
7. "Runbooks?" → Documented procedures for incidents

### Operations Template

```markdown
### Operations

**Deployment:**
- Pipeline: {GitHub Actions stages}
- Strategy: {Blue/green / canary / rolling}

**Logging:**
- Application: CloudWatch Logs — {log groups}
- Access: ALB access logs
- Audit: CloudTrail — {events captured}

**Monitoring:**
- Business: {KPIs tracked}
- Technical: {CPU, memory, latency, error rate}
- Custom: OTEL spans — {service name}

**Alerting:**
- Error rate > {X}% → PagerDuty P2
- p99 latency > {Xms} → Slack #alerts
- Availability < {99.9}% → PagerDuty P1

**Runbooks:** {Incident procedures location}
```

### Validation Format

```
"Operations:
- Deployment: {Pipeline, strategy}
- Logging: {Application, access, audit}
- Monitoring: {Business, technical, OTEL}
- Alerting: {Thresholds, destinations}
- Runbooks: {Documented}

Correct?"
```
