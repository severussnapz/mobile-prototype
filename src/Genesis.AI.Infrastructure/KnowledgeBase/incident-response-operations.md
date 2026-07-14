# Skill: Incident Response & Production Operations

**Apply whenever:** anything is wrong in production or a shared environment — an outage, a data anomaly, a failed deployment, a clinical-safety-relevant defect report — or when designing the operational posture (runbooks, alerts, rollback paths) *before* production. Genesis is pre-production; the cheapest time to apply this skill is now, in design.

---

## The incident state machine (in order, never skipped)

1. **Stabilise** — stop the bleeding before understanding it. Rollback, feature-flag off, or isolate. Diagnosis comes after the user impact stops.
2. **Communicate** — one owner (incident lead), one channel, a first status within minutes ("we know, we're on it, impact is X, next update at T"). In a clinical context, the communication duty may include formal notification obligations — know the DCB0129/incident-reporting thresholds *before* the incident.
3. **Diagnose** — hypothesis-first, same as development debugging: state what you believe and what evidence would confirm/refute, then look. Log-spelunking without a hypothesis is motion, not progress.
4. **Fix forward or roll back** — decided by one question: *is the fix smaller and better-understood than the rollback?* A one-line, well-understood fix forward beats a rollback that loses a day of data; anything uncertain rolls back. Never fix forward under pressure with a change you wouldn't merge in daylight.
5. **Verify** — the incident is over when the *user-visible symptom* is confirmed gone and the metric that caught it is back to baseline, not when the fix is deployed.
6. **Learn** — postmortem within days, while memory is fresh.

## Rollback is a designed capability, not an aspiration

- Every deployment must have a *tested* reverse path: previous container image retained and startable, and — the hard part — schema compatibility. This is why migrations must be backward-compatible one version in each direction (see data-modelling skill): a rollback blocked by an irreversible migration converts a 5-minute incident into an hours-long one.
- Feature flags are the cheapest rollback: risky behaviour ships dark, enables gradually, disables instantly. The `PrototypeSingleFileEnabled`-style flag gating both prompt and tool selection *atomically* is the model — a flag that half-disables a feature is worse than none.
- **Data incidents don't roll back.** Corrupted or wrongly-written data needs forward repair with an audit trail of the repair itself. In a system of record for 35M patients, every repair script is reviewed, tested against a copy, and logged like a migration — never run ad hoc against production.

## Blameless postmortems that actually change anything

- **Blameless means the system, not the person**: "the deploy process allowed an untested migration" not "X didn't test the migration." A person's mistake reaching production is by definition a missing guard — name the guard.
- **Five-whys until you hit a system cause** — stop at a process/design cause you can change, not at a human action. "Why did the agent's shortcut ship?" → "because the diff wasn't audited" → "because the audit wasn't a required step" → *actionable*.
- **Every action item has an owner and a date, and the list is short.** Three completed actions beat twelve aspirational ones. The standing meta-rule applies: a new *class* of failure gets a new guard/test-type in its family, not just an instance fix (see seam-testing.md).
- **Feed the pipeline**: incidents caused by generated code become Review Agent rules and eval cases; incidents caused by requirement gaps become P01 interview-engine probes. The postmortem's output slots into machinery that already exists.

## Observability designed before it's needed

- **Every error path logs with correlation** — a request/conversation/pipeline-run ID that ties the user's report to the server's story. An incident where you can't find the failing request in logs is two incidents.
- **Log the decision, not just the action**: "push skipped: budget exhausted (5/5)" is diagnosable; "push skipped" is not. Structured fields over prose.
- **Alert on user-visible symptoms** (error rate, latency, queue age, push_failure_log unresolved count) rather than internal causes — causes are many, symptoms are few, and symptom alerts catch the causes you didn't predict.
- **Never log what you can't afford to leak** — the NHS-data absolutes bind hardest in incident logging, exactly when the temptation to "just dump the payload" peaks. Pre-build sanitised diagnostic paths so the 2am responder doesn't have to improvise one.

## Runbooks

For every operational action someone might perform under pressure (restart, flag-flip, credential rotation, failed-push replay): a runbook stating preconditions, exact commands, expected output, and how to verify success. Written when calm, executed when not. A runbook that's never been rehearsed is a hypothesis — walk each one once in a non-prod environment.
