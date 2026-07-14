# Skill: Performance & Capacity Engineering

**Apply whenever:** designing anything that will face production scale (35M patients, 3,500 practices, ~550 engineers' worth of pipeline throughput), reviewing a query or loop that touches unbounded data, setting latency expectations, or deciding whether to optimise. The skill is knowing when performance is a requirement, when it's a risk, and when it's a distraction.

---

## Budgets before benchmarks

Performance work without a target is wandering. For every user-facing operation, set a budget: "p95 under N ms at M concurrent users / K rows." The budget comes from the user's context (a GP mid-consultation tolerates far less latency than a batch report), gets written into the REQ as a binary AC (see requirements-discipline.md), and is what you test against. "Make it fast" is not a requirement; "practice search p95 < 500ms against 3,500 practices" is.

Percentiles, not averages: an average hides the tail, and the tail is what users remember. p95/p99 are the numbers that matter; a fine average with a terrible p99 means someone's consultation hangs every twentieth click.

## Measure before touching — always

- **Profile, don't intuit.** Engineering folklore about where time goes is wrong often enough that acting on it wastes more than it saves. One profile/EXPLAIN beats an afternoon of guessed optimisation.
- **The counter-discipline:** never optimise without a measurement showing the cost, and never *dismiss* a concern without a measurement either — "it's probably fine" and "it's probably slow" are the same sin. Cheap estimate first: rows × bytes × frequency on a napkin decides most arguments.
- Premature optimisation and premature pessimisation are both failures. Choosing the obviously-right structure up front (an index on the column every query filters by; streaming instead of buffering a 12MB file) is not premature optimisation — it's design. Contorting code for speed nobody measured is.

## Database judgement (the usual scene of the crime)

- **Every query against a table that grows with patients/practices/artefacts must have a named index that serves it.** "It's fast in dev" with 200 rows says nothing about 35M. Check the plan (EXPLAIN) for anything new touching a growth table.
- **N+1 is the classic silent killer**: a loop issuing a query per item works in the demo and dies at scale. EF Core makes this easy to write by accident — review any navigation-property access inside iteration; batch or Include deliberately.
- **Fetch what you need**: targeted projections and targeted updates (the Ponytail DB rule — `ExecuteSqlAsync` for a single-field update rather than loading a full aggregate) are performance rules as much as style rules.
- **Unbounded result sets are bugs**: every list endpoint paginates; every internal query that could return "all of them" has a limit or a documented reason it can't grow.
- **Know the growth shape of every table you add** (per-project? per-artefact-version? per-message?) and state it in the design. A per-message table at pipeline scale is a very different animal from a per-project one.

## AI-pipeline-specific capacity

- **Tokens are the metered resource**: cost and latency both scale with context size. The small-decomposed-calls architecture (~hundreds of graph-injected tokens per call) is a capacity decision, not just a quality one — defend it against convenience-driven context bloat.
- **Prompt caching is a designed structure**: the stable/mutable prompt split exists so the large stable part is cached and cheap. Anything volatile added to the stable part silently destroys the cache hit — changes to prompt assembly must state which part they touch and why.
- **Budgets on agent loops** (read budgets, turn limits) are capacity guards as much as behaviour guards — an agent loop without a hard cap is an unbounded query wearing a different hat.
- **Inference concurrency and rate limits are capacity planning inputs**: Bedrock throughput per model, retry/backoff behaviour under throttling, and queueing behaviour when 550 engineers' pipelines contend — designed, not discovered.

## Load characterisation before launch

Before any capability goes to real users: name the expected load shape (requests/min, concurrency, payload sizes, peak-vs-average ratio), test at ~2× expected peak, and watch the p99 and the error rate — not just "did it fall over." The output is either confidence with numbers or a named bottleneck with a plan. "We never load-tested it" is an incident scheduled for later.

## When someone reports "it's slow"

Treat as a diagnosable defect, not an ambiance: get the correlation ID, find the actual request, decompose its time (network / app / DB / inference), and fix the largest slice first. The decomposition almost always surprises; that's why you measure.
