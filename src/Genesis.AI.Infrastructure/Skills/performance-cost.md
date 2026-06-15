# SKILL: performance-cost
# Phase: P03 Architecture — Phase 9

## Performance & Cost

**Purpose:** Define SLOs and estimate AWS costs.

### Performance Questions

1. "Latency targets?" → p50, p95, p99 (from Phase 6)
2. "Throughput?" → Requests/second, concurrent users
3. "Scaling strategy?" → Auto-scale based on what metric

### Cost Estimation

Estimate AWS costs for eu-west-2:

```
For EACH major service:
- {Service}: {Component} | {Usage} | ${Cost/month}

Total: ${X,XXX}/month

Within budget?
```

### Performance Template

```markdown
### Performance & Cost

**SLOs:**
- p50: < {X}ms
- p95: < {Y}ms
- p99: < {Z}ms
- Throughput: {N} req/s avg, {M} req/s peak
- Scaling: Auto-scale on {CPU/request count/custom metric}

**Cost Estimate (eu-west-2):**
| Service | Component | Usage | Cost/month |
|---------|-----------|-------|-----------|
| ECS Fargate | 2 vCPU, 4GB | 24/7 | ${XXX} |
| RDS Postgres | db.t3.medium | 24/7 | ${XXX} |
| Total | | | ${X,XXX} |

Budget: ${Y,YYY}/month ✅
```

### Validation Format

```
"Performance & Cost:
- Latency: p50 <{X}ms, p95 <{Y}ms, p99 <{Z}ms
- Throughput: {N} req/s avg, {M} req/s peak
- Scaling: Auto-scale on {metric}
- Cost: ${X,XXX}/month (budget: ${Y,YYY}/month)

Correct?"
```
