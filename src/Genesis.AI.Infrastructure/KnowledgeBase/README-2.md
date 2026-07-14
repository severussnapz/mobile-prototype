# Genesis SDLC Skills — Bundle 2 (Gap Coverage)

Twelve senior-expert SDLC skills covering the gaps identified after Bundle 1. Bundle 1 encoded the lessons *earned* in-session (agent supervision, TDD hardening, seam testing, design integrity, verification, hygiene, regulated judgement); this bundle covers the expert competencies the programme needs that no incident had yet forced into writing.

Same format: each file opens with an **"Apply whenever"** trigger so a model scanning the KB knows when the skill is in force.

## The twelve skills

| File | Gap covered |
|---|---|
| `architecture-design-quality.md` | Judging whether a design is *good* — boundaries, coupling, sync/async, abstraction cost, the one-new-concept ratio, structural smells. |
| `security-engineering.md` | Threat modelling as method (STRIDE walk scaled to blast radius), Genesis standing threats (sovereign boundary, confused-deputy agents, secrets, supply chain), what the P08 reviewer actually does. |
| `ai-pipeline-engineering.md` | Prompt engineering as engineering, eval design for pipeline outputs, drift/degradation detection, correction-mining, RAG retrieval quality. The most Genesis-specific skill. |
| `requirements-discipline.md` | P01 craft: requirement vs solution, testable REQ anatomy, elicitation technique (probe the vacuum, why-chains, quantify-or-park), binary P01 exit. |
| `human-code-review.md` | What only humans catch (right thing, right size, what's absent), feedback that lands, reviewing agent code (findings become prompt rules), reviewer hygiene. |
| `incident-response-operations.md` | The incident state machine, rollback as designed capability, blameless postmortems that feed the pipeline, observability designed before need, runbooks. |
| `performance-capacity.md` | Budgets before benchmarks, measure-before-touching, DB judgement at 35M-patient scale, token/caching capacity, load characterisation. |
| `api-contract-design.md` | The craft of a *good* contract (distinct from contract-layer mechanics): resources, error design, shape rules, compatibility judgement, design-for-NSwag. |
| `data-modelling-migrations.md` | Modelling invariants structurally, snapshot vs reference, expand–migrate–contract zero-downtime evolution, migration immutability, repair-script discipline. |
| `stakeholder-communication.md` | ADR craft for the reader two years out, design reviews that produce decisions, translating trade-offs into consequence language for CPO/CEO/PE audiences, status narratives. |
| `legacy-migration-strangler.md` | Strangler-fig execution: one system of record, reversible seams, characterisation tests as the parity contract, shadow runs, decommission as a real phase. |
| `testing-strategy.md` | Suite shape by risk, contract tests as the two-agent referee, test-quality judgement (a green suite can prove nothing), flaky-test discipline, the regression contract. |

## Relationship to Bundle 1

Bundle 1 = incident-earned process integrity. Bundle 2 = domain competency. They cross-reference: `testing-strategy.md` builds on `seam-testing.md` and `tdd-red-green-discipline.md`; `security-engineering.md` and `incident-response-operations.md` extend `regulated-engineering.md`; `stakeholder-communication.md` operationalises `design-integrity.md` for ADRs and reviews. Adopt both bundles together in the KB.

## Honest provenance note

Unlike Bundle 1, most of this content is expert best practice *applied to* the Genesis context rather than lessons earned from Genesis incidents. Where a rule is grounded in a real Genesis event (the 900-green-tests DTO history in testing-strategy, the missing-PATCH container catch, the stable-ID decision in stakeholder-communication), it says so; the rest should be treated as strong defaults to be tuned as the programme generates its own evidence — per the standing rule, real incidents refine these files.

## Adoption

1. Unzip into `src/Genesis.AI.Infrastructure/KnowledgeBase/` alongside Bundle 1; commit as one docs commit.
2. Add to the Claude project knowledge so both KB and project stay in sync.
3. Highest-leverage for current phase (contract layer → Plan 5 TDD): `testing-strategy.md`, `api-contract-design.md`, `ai-pipeline-engineering.md`, `data-modelling-migrations.md`.
4. Pre-production must-reads before first pilot: `incident-response-operations.md`, `performance-capacity.md`, `legacy-migration-strangler.md`.
