# SKILL: aws-well-architected
# Phase: P03 Architecture — Phase 6

## AWS Well-Architected Framework Validation (6 Pillars)

**Purpose:** Validate all 6 pillars of the AWS Well-Architected Framework.

### Questions (1–2 per pillar)

**Operational Excellence:**
1. "Deployment strategy?" → Blue/green, canary, rolling
2. "Infrastructure as Code?" → AWS CDK, CloudFormation

**Security:**
1. "Service-to-service auth?" → IAM roles
2. "Data encryption?" → At rest (KMS), in transit (TLS)

**Reliability:**
1. "Availability target?" → 99.9%, 99.99%
2. "Multi-AZ?" → Yes/No

**Performance Efficiency:**
1. "Latency SLOs?" → p50, p95, p99
2. "Caching strategy?" → CDN, app cache, DB cache

**Cost Optimisation:**
1. "Monthly budget?" → Hard limit or soft target
2. "Auto-scaling?" → Based on CPU, request count

**Sustainability:**
1. "AWS region?" → If NOT eu-west-2: "Why?"
2. "Data lifecycle?" → Retention, archiving

### WAF Template

```markdown
### AWS Well-Architected

| Pillar | Status | Notes |
|--------|--------|-------|
| Operational Excellence | ✅ | {Deployment strategy, IaC} |
| Security | ✅ | {Auth, encryption, isolation} |
| Reliability | ✅ | {Availability %, multi-AZ, DR} |
| Performance Efficiency | ✅ | {Latency SLOs, caching} |
| Cost Optimisation | ✅ | {Budget, scaling} |
| Sustainability | ✅ | {Region eu-west-2, data lifecycle} |
```

### Validation Format

```
"WAF validation:
- Operational Excellence: ✅ {Strategy, monitoring, IaC}
- Security: ✅ {Auth, encryption, isolation}
- Reliability: ✅ {Availability, multi-AZ, DR}
- Performance: ✅ {Latency SLOs, caching}
- Cost: ✅ {Budget, scaling}
- Sustainability: ✅ {Region, lifecycle}

Correct?"
```
