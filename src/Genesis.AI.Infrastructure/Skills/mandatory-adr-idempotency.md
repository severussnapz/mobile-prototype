# SKILL: mandatory-adr-idempotency
# Phase: P03 Architecture — Phase 1

## Mandatory Idempotency ADR

This ADR is required for every project that uses SQS for command handling. Create it in Phase 1 without asking — it is non-optional.

### Create This ADR

```
ADR-{NNN}: SQS Command Handler Idempotency — Mandatory

- Context: SQS FIFO queues prevent duplicate delivery at the queue level but NOT at
  the handler level. Without application-level idempotency, duplicate processing
  can occur after network failures, Lambda retries, or ECS restarts.
- Decision: Every SQS-consumed command handler must implement idempotency at the
  application layer using one of:
  (1) idempotency_key UUID NOT NULL column on the target entity table with UNIQUE constraint, or
  (2) dedicated processed_messages (message_id UUID PRIMARY KEY, processed_at TIMESTAMPTZ) table.
  The idempotency key source must be documented: SQS MessageDeduplicationId, request body field,
  or generated at dispatch.
- Alternatives: Rely on FIFO queue deduplication only.
- Rationale: Application-layer idempotency is the only guarantee. FIFO deduplication
  window is 5 minutes; Lambda/ECS retries can occur outside this window.
- Consequences: Pipeline 04 must include idempotency_key in all SQS handler schemas.
  Handlers must check-and-reject duplicate keys before processing.
- EMIS Principle: Principle 8 (AWS Well-Architected — Reliability)
- Guardrail: ENG-005
```

### Enforcement in Pipeline 04

All SQS command handler DDL must include idempotency_key. Flag any handler missing it.
