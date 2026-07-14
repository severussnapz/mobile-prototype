# Loop Engineering & the Code Swarm Strategy

## What loop engineering is (precise definition)

Loop engineering is the shift from being the person who prompts the agent turn-by-turn to designing a system — the loop — that discovers work, hands tasks to agents (often sub-agents), verifies results, persists state, and decides the next action, on a schedule or until a goal is met.

A loop is not a prompt. It is a recurring process with memory, verification, and boundaries. Reasoning alone does not close the loop — an agent that can only suggest is not running a loop. An agent that can run code, observe the result, fix it, and run again is. That inner cycle (reason → act → observe → repeat) is what every outer loop depends on.

*(Concept credited to Addy Osmani and Cobus Greyling, June 2026.)*

### The three layers

| Layer | Solves | Question |
|-------|--------|----------|
| **Context engineering** | What the agent knows | What goes in the context window? |
| **Harness engineering** | What one agent run can do | What can a single run touch and execute? |
| **Loop engineering** | The system that runs agents for you | What discovers work, dispatches, verifies, persists, decides next? |

The harness equips a single agent run. The loop keeps poking agents on a schedule, spawning helpers, and feeding itself.

---

## The two debts loop engineering must control

**Comprehension debt — the new technical debt.** When agents write the code, the risk is code nobody understands. An Anthropic 52-engineer study found a 17% comprehension gap. For a regulated medtech codebase handling NHS patient records, this is the central risk: code you cannot explain is DCB0129 liability you cannot defend. The loop must control comprehension debt through fresh-agent review, TDD discipline, and human comprehension checkpoints — not just "does it pass tests" but "can a human explain what it does and why."

**Cost / boundary debt.** An autonomous loop running on a schedule can burn tokens or touch things it should not. A real-world case reported a $4,200 overnight bill. The loop needs hard boundaries: what it is allowed to touch, spend limits, and rejection hooks (e.g. Claude Code's PreToolUse hook) that block dangerous actions before they execute. Especially true for a loop operating anywhere near NHS data.

---

## The swarm IS a loop

The EMIS Web → EMIS-X migration cannot be prompted turn-by-turn across 550 engineers' worth of work. The code swarm (Workstream E) is a loop in the precise sense:

| Loop function | In our system |
|---------------|---------------|
| **Discovers work** | Reads the migration backlog from the context graph — which EMIS Web capabilities are not yet on EMIS-X — via the `graph_get_migration_status` query. Picks the next capability. |
| **Dispatches to sub-agents** | Each capability goes to a scoped agent with graph-injected context, test-first. Bounded context per agent. |
| **Verifies results** | Inner loop: agent runs code, observes, fixes, re-runs. Outer loop: fresh-agent review gate + test suite + guardrails + human comprehension check. |
| **Persists state** | Migration status written back to the graph. The graph is the loop's memory — it knows what is done and what is next across iterations. |
| **Decides next action** | Continue to next capability, or hand off to a human at a structured decision point (AskUserQuestion) it cannot resolve. |

The context graph is simultaneously the loop's knowledge source, its work queue, and its persistent memory. That triple role is why the graph is the moat — it is not just context, it is what makes the loop able to run at all.

---

## Phased autonomy — the loop earns trust

**Full autonomy without proven guardrails is not sensible.** With NHS patient data and DCB0129 liability, the loop does not start autonomous. It starts heavily human-gated and earns autonomy only as each guardrail is validated in production. Autonomy is ratcheted up deliberately, never assumed.

### Phase 0 — Manual loop (human prompts every step)
Where we are now. Human drives each task, reviews each output. No autonomy. Purpose: prove the disciplines (sub-agent scoping, fresh review, TDD, two-branch) on single features. **Plan 4 is Phase 0.**

### Phase 1 — Assisted loop (human dispatches, agent executes, human verifies every output)
The loop dispatches one task at a time. The agent runs the inner cycle (code, test, fix). The human verifies every single output before it integrates. Guardrails being validated: fresh-agent review catches real issues; TDD discipline holds; evaluation harness scores correlate with actual quality. No work proceeds without human sign-off. **Workstream D (TDD agent) is Phase 1.**

### Phase 2 — Supervised loop (agent runs, human approves at gates, not every output)
The loop runs multiple tasks. The human approves at defined gates (capability complete, PR ready) rather than every output. Structured decisions surface automatically. Guardrails being validated: the verification gates catch what humans would have caught; comprehension stays defensible; cost stays bounded. Requires Phase 1 guardrails proven reliable. **Early Workstream E is Phase 2.**

### Phase 3 — Autonomous loop within boundaries (scheduled, human handles exceptions)
The loop runs on a schedule, discovers work from the graph, migrates capabilities, and only surfaces to a human on a structured decision or a guardrail failure. Hard boundaries enforced: spend limits, rejection hooks, what it may touch. Guardrails being validated: the loop's own verification is trusted because it has been proven across Phases 1-2. **Full Workstream E is Phase 3 — and only reached when every guardrail below is proven.**

### Gate to advance a phase
A phase advances only when its guardrails are proven, not on a schedule:
- Fresh-agent review demonstrably catches issues humans would have caught
- TDD discipline holds with zero suppressed/faked tests
- Comprehension checks pass — humans can explain agent output
- Evaluation harness scores correlate with real quality
- Cost stays within bounds; rejection hooks block dangerous actions
- Clinical safety guardrails (DCB0129) validated by a practitioner

No guardrail proven → no autonomy granted. The loop does not graduate on optimism.

---

## The loop's required disciplines (consolidated)

| Discipline | Purpose | Proven in |
|-----------|---------|-----------|
| Sub-agent scoping | Bounded context per task | Plan 4 (Phase 0) |
| Context graph injection | EMIS-specific, grounded output; work queue; memory | Workstream C |
| AskUserQuestion | Humans control key decisions; loop hands off cleanly | Plan 4, all stages |
| Fresh agent review | Catches normalised errors; controls comprehension debt | Plan 4, Workstream D |
| TDD test-first | Verifiable parallel output; the contract | Workstream D (Phase 1) |
| Two-branch isolation | Clean integration, no slop to main | Plan 4 |
| Evaluation harness | Prompt quality measured, not eyeballed | Plan 4 |
| Comprehension checkpoint | Human can explain agent output (DCB0129 defensible) | Workstream D onward |
| Cost / boundary hooks | Spend limits, rejection hooks, scope limits | Phase 2 onward |
| Ponytail discipline | Minimal, correct generation | All code work |

---

## Sequencing — prove the loop small, run it wide

```
Plan 4         Phase 0  — manual loop, one feature, disciplines proven
   ↓
Workstream D   Phase 1  — assisted loop, two-agent TDD, verify every output
   ↓
Workstream C   (graph)  — knowledge source + work queue + memory for the loop
   ↓
Workstream E   Phase 2  — supervised loop, approve at gates
   ↓
Workstream E   Phase 3  — autonomous within boundaries, human handles exceptions
```

The loop is the unit that scales. Each discipline is proven on a smaller workstream before the loop is granted more autonomy. The swarm is not many agents with checkpoints — it is an autonomous migration loop, with the context graph as its memory and verification gates as its boundaries, that earns its autonomy phase by phase as the guardrails prove themselves.

---

*A swarm without loop engineering is faster slop. A swarm with loop engineering, phased deliberately behind proven guardrails, is a migration force multiplier you can defend to a regulator. The loop is the product. Prove it on Plan 4. Grant it autonomy only as it earns trust.*

---

## Cost Architecture — the economics that make the loop viable

Anthropic's demonstrations (54 sub-agents, 6.4M tokens, 10.5 hours) are showcase economics, proving capability. They are not production economics. A real migration loop running continuously across hundreds of capabilities cannot run on frontier-model pricing for every call. The cited $4,200 overnight bill is the failure mode this architecture exists to prevent.

### The core inversion

The expensive model touches the low-frequency work. The cheap models touch the high-frequency work.

You plan a capability once. You generate, test, and edit its code hundreds of times. So:

| Tier | Work | Frequency | Pace | Cost control |
|------|------|-----------|------|--------------|
| **Frontier (Bedrock Claude)** | Planning, interrogation, hardest reasoning, critical-path review | Low | Human-paced | Naturally throttled — a human is in the loop, thinking |
| **Open (Qwen2.5-Coder, local inference)** | Generation, mechanical edits, autocomplete, the execution loop | High | Machine-paced | Near-zero per-token on own inference |

Planning is manageable precisely because it is bounded and human-paced — a few hours, a few dollars, once per capability. There is no runaway risk in planning because a human is in it. The runaway token risk lives in the autonomous execution loop — which is exactly where the open models and cost boundaries sit.

### The danger is only ever the reverse

- Frontier model in the high-volume execution loop → runaway cost
- Open model attempting the planning it is not capable of → bad output

The routing layer prevents both. This is the role of LiteLLM + RouteLLM/vLLM semantic router: classify task complexity, route cheap work to cheap models, expensive work to expensive models.

### Small tasks are a cost strategy, not just a quality one

Decomposed, bounded tasks with precise graph-injected context (~800 tokens) are cheaper per unit and more controllable than large tasks with huge context windows. The "throw the whole repo in the context window" frontier approach is the expensive anti-pattern. The architecture is already built for small-context economics — per-requirement windowing, foundation prefix caching, graph-ranked context injection.

### Phased validation of open models

Open models carry volume only after they are proven to hold quality:
- Phase 1: open model output A/B'd against frontier on the evaluation harness
- Open model carries a task type only when its harness scores match frontier within tolerance
- Frontier retained for any task type where open models measurably underperform
- Routing thresholds tuned from real evaluation data, not assumption

### The bet

Frontier for the thinking. Open for the doing. Small tasks throughout. Routing to enforce the split. Proven before scaled. This is the opposite of showcase economics — it is an architecture that has to pay the bill every month at 550-engineer scale, and is designed to.

---

## kagent — the agent runtime for the swarm

### What kagent is

kagent is the runtime that manages AI agents as first-class Kubernetes workloads. It does not discover work, build plans, or decide what to do next — that intelligence comes from outside it. What it provides is the infrastructure layer: starting agents, stopping them, scaling them, controlling what they can access, observing everything they do, and enforcing human approval gates at the right points.

Think of it as the control tower. The agents are the planes. kagent is air traffic control — it doesn't fly the planes, it knows where they all are, keeps them from colliding, and lands them safely.

### Why it fits

EMIS-X runs on Kubernetes on AWS (EKS). kagent runs on Kubernetes. The dependency question is already answered — kagent is not a future platform decision, it is another workload on the cluster you already run. Agents deploy alongside application workloads, managed with the same GitOps, same RBAC, same observability stack already in place.

### What kagent provides for the swarm

- **Agent lifecycle** — starts agents when there is work, stops them when done, restarts on crash
- **Identity and RBAC** — each agent has exactly the permissions it needs and nothing more. Critical for NHS-adjacent infrastructure
- **Human-in-the-loop gates** — `requireApproval` on any tool gates destructive operations before they execute. This is the phased autonomy model enforced at the infrastructure level, not just in the prompt
- **AskUserQuestion built in** — agents can surface structured decisions to humans when they need clarification, natively supported
- **MCP tool integration** — your context graph exposed as an MCP server connects directly to kagent agents. Agents call the graph for context the same way they call any other tool
- **Agent-to-agent via MCP** — kagent exposes running agents as an MCP server, so a planning agent can invoke coding sub-agents as tool calls. The swarm coordination is MCP-native
- **OpenTelemetry tracing** — every agent turn, every tool call, every token spent is observable in your existing monitoring stack
- **Multi-model support** — Bedrock (Claude, frontier) for planning and critical reasoning; Ollama/Qwen on local G5 for execution volume. kagent routes to both, enforcing your cost strategy at the runtime level
- **GitOps** — agent definitions are Kubernetes CRDs, versioned in Git, reviewed in PRs, rolled back with a git revert. Agents get the same governance as application code

### What kagent does not provide — you build these

| Component | What it does | Where it lives |
|-----------|-------------|----------------|
| **Planner** | Reads migration backlog from graph, produces structured task plans | Frontier model (Bedrock Claude), your planning pipeline |
| **Work queue** | Holds task plans between planning and execution | RDS or SQS — your existing AWS estate |
| **Context graph** | Knowledge source, work queue, persistent memory for the loop | Workstream C — exposed as MCP server on EKS |
| **Loop logic** | Decides what's next — continue, hand off, escalate | Your loop design, enforced by kagent gates |

### The swarm architecture on EKS

```
Context Graph (MCP server on EKS — Workstream C)
         ↓
Planning Agent (Bedrock Claude — frontier, low volume)
produces structured task plan per capability
         ↓
Work Queue (RDS/SQS — AWS native)
         ↓
kagent (on EKS) picks up task, invokes coding agent
         ↓
Coding Agent (Qwen on G5 via Ollama — high volume, near-zero cost)
reads graph via MCP → writes code → runs tests → loops until done
         ↓
requireApproval gates (Phase 1-2) or autonomous within boundaries (Phase 3)
         ↓
AskUserQuestion at structured decision points — human resolves
         ↓
Output cherry-picked to PR branch → fresh agent review → merged
```

Everything stays AWS-native. Bedrock PrivateLink for frontier calls. Local Qwen on G5 for execution volume. EKS for the agent runtime. Existing RDS for state. SQS or DB table for the work queue. No new platforms, no new vendors beyond kagent itself which is open source and CNCF Sandbox.

### One open item before Phase 3

Validate that kagent's Bedrock provider configuration routes through PrivateLink, not the public Anthropic endpoint. kagent supports custom model endpoints and proxy configuration so this should be straightforward — but for a medtech org with NHS-adjacent data it is a confirm-before-production item, not an assumption. This is the only open gate between now and Phase 3 infrastructure planning.

### When to evaluate kagent

Phase 3 — when the swarm is running autonomous migration agents at scale and you need production-grade identity, observability, governance, and human approval gates enforced at the infrastructure level rather than in code. Not now. The immediate loop engineering problems (Plan 4, Workstream D) are solved by your existing AWS stack. kagent becomes the right tool when the swarm outgrows manual orchestration and needs a managed runtime that Shantanu's team can operate without owning bespoke agent management code.

Raise it with Shantanu as a Phase 3 infrastructure evaluation item: "EKS confirmed, kagent CNCF Sandbox, fits our Kubernetes estate, evaluate for Workstream E Phase 3 agent runtime."

---

## Loop control — goal and hard stop

### The only two controls that matter for your migration loop

Every loop needs a stop condition designed before the trigger fires. If the stop condition is subjective, the loop is a money pit.

**Goal** — binary acceptance criteria the loop checks after every iteration. Not "looks good." Binary. The loop knows exactly when it is done.

**Hard stop** — max iterations reached without meeting the goal. Loop escalates to a human and stops. Never retries indefinitely. Never burns tokens chasing a goal it cannot reach.

Everything else — heartbeat, cron, hook — are trigger patterns for loops where planning happens at the point the trigger fires. Your migration loop separates planning from execution by design. The plan already exists before any trigger fires. Goal is the only trigger type that applies.

### All work is planned work

The distinction is not planned vs unplanned — it is when planning happens.

A customer support loop plans dynamically at trigger time. A ticket arrives, the loop reads it, plans the response, executes. The work was not known in advance but it is still planned — just planned at the point the trigger fires.

Your migration loop plans upfront, before the loop starts. The planning agent produces the task plan and the binary acceptance criteria before a single execution token is generated. The work is known, sequenced, and contracted in advance.

Both are planned. The shape of the planning is different, not the principle. The four trigger types describe when and what initiates planning — not whether the work is planned.

### The planning agent's contract

The planning agent produces two things before the execution loop starts:

1. **The task plan** — what capability to migrate, in what order, with what dependencies, with what graph context injected
2. **The binary acceptance criteria** — the exact conditions the execution loop checks to know it is done

Both must exist before the loop starts. A task plan without acceptance criteria has no stop condition. A stop condition without a task plan has nothing to execute against. One without the other is incomplete and the loop becomes a money pit.

Example task plan contract:

```
Capability: Document Filing
Graph context: EMIS Web filing flow, DCB0129 HAZ-DOC-021, GP2GP integration pattern
Acceptance criteria:
  - All tests pass (zero failures)
  - Guardrails 95/95
  - DCB0129 hazard log generated
  - No hardcoded NHS numbers in output
  - Fresh agent review approved
Hard stop: 5 iterations
Escalate to: human — structured decision via AskUserQuestion
```

The loop checks all five criteria after every iteration. Passes all five — done. Hits five iterations without passing — escalates to human, stops. Never burns beyond the contract.

### The five requirements every loop needs

From the post by Addy Osmani — universally correct, applies to every loop in your system:

- **Checkable goal** — binary outcome, not "looks good"
- **Hard stop** — max attempts or a time ceiling (budget protection)
- **Right tools** — real verification, not self-assessment
- **Memory** — the agent tracks what it has already tried (your context graph)
- **Separate checker** — the agent that builds cannot be the one that grades (your fresh agent review + test suite)

These are the floor. Your regulated medtech architecture builds on top of them — phased autonomy, cost routing, graph-as-memory, kagent runtime, DCB0129 acceptance criteria. Neither is complete without the other.

### Token burn prevention

The stop condition is the primary token burn control. A binary stop condition means the loop never iterates beyond what is necessary to meet the contract. Combined with:

- Small tasks (~800 token graph-injected context per agent) — structural cost control
- Hard iteration cap — never burns beyond N attempts regardless of outcome
- Model routing — Qwen local for execution volume, frontier only for planning
- Prompt caching — stable context cached across iterations, ~10x cheaper on cache hits
- kagent budget caps — token limit per agent invocation enforced at runtime level
- Cost anomaly alerts — OpenTelemetry per-agent token data, alert on cost anomalies per capability type

The combination of binary stop condition plus small tasks plus model routing is what makes the loop economically viable at migration scale. No single control is sufficient — all five work together.

### Environment separation

The loop never touches production. GitHub Actions environment protection rules enforce this:

- **Experiment branch** — loop runs freely, full automation, agents push, tests and fresh agent review fire automatically
- **PR branch** — cherry-picked proven commits only, CI runs on PR open, human reviews diff before merge
- **Main** — human merges after review, no agent pushes directly
- **Production** — separate environment, named human approver required for every deployment, no automation bypasses it

The loop has full autonomy in the experiment branch. The human gates live at PR and main merge. Production is always a human decision. Auditable and defensible to a regulator.

---

## Knowledge Graph Hardening — Loading Validation & Ontology Layer

Two gaps surfaced in the Context Graph (Workstream C) design that must be closed before the graph is trustworthy enough to feed an autonomous loop. Neither is a new workstream — both are hardening steps inside Workstream C, sequenced before Workstream E reaches Phase 2/3 autonomy.

### Why this matters for the loop

The phased autonomy model depends on the loop's "checkable goal" and "right tools" criteria being trustworthy. If the graph feeds an agent unvalidated or malformed context, the binary acceptance criteria in the task plan are built on unreliable input — and a confidently wrong answer from bad graph data is indistinguishable from a confidently wrong answer from a bad model. The graph must be hardened before any loop is trusted to run with reduced human oversight.

### Gap 1 — Loading validation layer

Document ingestion failures are silent. None of the following throw an error — the pipeline runs fine and quietly hands broken context to every agent downstream, until the system confidently gives a wrong answer:

| Failure mode | Risk in our system |
|--------------|---------------------|
| Two-column PDF extracts out of order, jumping mid-sentence | DCB0129 hazard documents, NHS contracts, EMIS manuals are frequently multi-column. A garbled hazard entry is a patient safety risk, not a quality issue. |
| Scraped web/Confluence content includes nav chrome, ads, cookie banners | Noise pollutes embeddings, degrades retrieval quality silently across all internal wiki/Confluence sources. |
| Structured data (CSV, DB schema, migration status) embeds as disconnected tokens | The embedding model needs natural language, not raw rows, to reason about schema and status data. |
| Scanned PDFs have zero selectable text without OCR | 25 years of EMIS history will include scanned documents. UnOCR'd documents embed as blank or garbled. |
| Missing metadata loses source traceability | For DCB0129, every piece of context the graph serves must be traceable to a source document, version, and date. Untraceable context is an audit failure. |

**Fix — a loading validation layer, not a custom build.** Sits between raw source ingestion and graph insertion:

```python
doc = unstructured.load(path)        # handles PDF layout, OCR, column order
content = doc.clean()                 # strips nav/ad/cookie chrome
sentences = doc.to_sentences()        # structured data → natural language
metadata = extract_metadata(doc)      # source, version, date, author
assert metadata.is_complete()         # fails fast if missing — never silent
graph.insert(sentences, metadata)
```

Tooling: `unstructured` (PDF layout detection, column ordering, OCR mode) and `trafilatura` (web/Confluence main-content extraction). No custom parsers, no custom OCR engine — proven open source libraries doing the heavy lifting.

**Loading test suite:** one good/bad test case per document type (two-column PDF, scanned PDF, Confluence page, CSV/schema data) — same discipline as the prompt evaluation harness. The loading pipeline does not go to production until every test case passes.

**Highest validation standard:** DCB0129 hazard logs, clinical safety cases, and hazard analysis records require human spot-check on a sample after loading, and mandatory metadata including document version and approval date. These are the documents where a loading failure has the most serious consequence.

### Gap 2 — Ontology / validation layer

A knowledge graph without an ontology stores facts but enforces no meaning. GraphRAG-style retrieval — query the graph, hand nodes to the LLM, let it interpret — does not prevent the model from combining structurally unrelated graph nodes into a plausible-sounding but invalid answer, and does not stop it from answering confidently when retrieved facts are incomplete.

For a regulated platform, the dangerous failure mode is an agent confidently asserting a hazard mitigation that does not actually apply to the capability being migrated, because the graph retrieval returned adjacent-but-unrelated nodes and nothing enforced the semantic boundary between them.

**What an ontology actually is here — not RDF/OWL, not new infrastructure:** three things written down once, enforced as a validation step.

**Step 1 — entity types** (drawn from the eight existing graphs):
```
Hazard (DCB0129 graph)
Requirement (Requirements graph)
CodeEntity (Repo graph — class, method, endpoint)
Capability (Capability Catalogue)
TestCase (Repo graph)
IntegrationPoint (Repo graph — Spine, eRS, EPS, GP2GP, MESH)
SecurityControl (Security graph)
DataFlow (IG graph)
```

**Step 2 — legal relationships** (anything not listed is rejected):
```
Hazard --mitigates--> Requirement
Hazard --applies_to--> Capability
Requirement --implemented_by--> CodeEntity
CodeEntity --tested_by--> TestCase
CodeEntity --integrates_with--> IntegrationPoint
SecurityControl --protects--> DataFlow
```

**Step 3 — constraints** (instance-level validity, not just type-level):
```
Hazard.applies_to(Capability) valid only if Capability.status != deprecated
Requirement.implemented_by(CodeEntity) requires CodeEntity.is_tested == true
IntegrationPoint must be one of: Spine, eRS, EPS, GP2GP, MESH (closed list, not free text)
```

**Step 4 — enforcement** — a validation function sitting between graph query and agent context injection:
```python
def validate_retrieved_context(entities, relationships):
    for rel in relationships:
        if not is_legal_relationship(rel.source_type, rel.predicate, rel.target_type):
            reject(rel)  # never reaches the agent
        if not satisfies_constraints(rel):
            reject(rel)
    return clean_context
```

**Scope honestly:** a lookup table of legal triples plus a handful of constraint functions — roughly 200 lines of code. No RDF triple stores, no OWL reasoners, no SPARQL. The PostgreSQL-based graph design stays exactly as architected; this is a guard function, not a new technology stack.

**Note on framing:** this is not purely an LLM-specific workaround. Defining legal relationships between a Hazard and a Capability, and ensuring a deprecated capability cannot carry an active hazard mitigation, is basic data integrity — needed even without an LLM in the loop, given eight different source systems feed the graph. It happens to also protect against LLM misuse of structurally invalid data. It is the same "LLM describes intent, deterministic layer executes/validates precisely" pattern already used for DOM edits, applied at the graph layer.

### Where both fit in the build sequence

```
Workstream C — current: architecture designed, build starting
   ↓
Phase 1 — build the eight graphs + loading validation layer (ingestion-time)
   ↓
Phase 2 — define entity types, legal relationships, constraints (requires graphs to exist first)
          write and wire the validation function (query-time, before MCP exposure)
   ↓
Phase 3 — expose via MCP server, every pipeline agent queries through the validated path
   ↓
Workstream E gate — swarm autonomy phases 2/3 require this hardening proven first
```

Owner: Darren's team (Workstream C), as an addition to existing scope — not a new owner, not a new workstream. The eight-graph architecture is correct as designed; this is what makes its output trustworthy enough to hand to an autonomous loop with reduced human oversight.

### Addition to the consolidated discipline table

| Discipline | Purpose | Where proven |
|-----------|---------|---------------|
| Loading validation layer | Silent ingestion failures caught before they reach the graph | Workstream C, Phase 1 |
| Ontology / validation layer | Structurally invalid graph combinations rejected before reaching an agent | Workstream C, Phase 2 — gate for Workstream E Phase 2/3 |

---

## Architectural Drift & Server-Side Enforcement

### The threat model

Drift in a long AI-assisted project is not one big mistake. It is hundreds of small, locally-justified deviations. Each individual commit looks reasonable on its own. Taken together, they slowly unmake the architecture. Better prompts and better copilot instructions do not fix this — they work for one session, not for a five-month-plus migration programme. Prompt-level discipline degrades as a defence over time because no single deviation ever looks wrong enough to reject in the moment.

This is the gap behind Ponytail and the copilot instructions: necessary, but not sufficient on their own across a long programme. They need a structural backstop that does not depend on the agent — or whoever is driving it — choosing correctly every single time.

### Three components that keep architecture coherent over time

**1. Agent rules** — copilot instructions, Ponytail, the pipeline prompts. Define how agents operate. *(Already in place.)*

**2. Architecture** — the documents that define what agents are allowed to produce: API contracts, coding standards, architecture decisions, the ontology/relationship rules. *(Already in place, see Workstream C addition above.)*

**3. Independent validators** — separate agents that check every implementation against the rules in 1 and 2, with no access to the reasoning that produced the implementation. *(Gap — add below.)*

### Server-side enforcement, not prompt-level trust

A GitHub repository ruleset enforces components 1 and 2 at the server, not in the prompt:

- Protected files locked at the repository level: architecture decision records, API contracts, coding standards, DCB0129/clinical safety documents, migration contracts
- The AI agent's token is excluded from the bypass list — it physically cannot push to a locked file, regardless of what it decides
- Only a human-controlled admin account is on the bypass list
- Any push to a locked file from the agent's token is rejected at the server, not caught by review after the fact
- When the agent needs a locked file changed, it surfaces a structured Product Decision (AskUserQuestion) — a human decides, then makes the change under their own account

This is the same principle already applied to DOM edits — the LLM describes intent, a deterministic system enforces it precisely — applied to the repository's own protected files. It converts "the agent should not touch this" from a prompt instruction into something the agent cannot do.

### Validator context isolation — a hard rule

The independent validator (fresh agent review) must see only the implemented diff and the locked rules it is checked against. Nothing else. No planning conversation, no implementation reasoning.

This is a precise, enforceable correction to the existing fresh-agent-review principle, which currently states "no prior context" without specifying the mechanism. The reason this must be enforced exactly, not just intended: if the validator sees the reasoning behind a change, it tends to start agreeing with the reasoning rather than checking the output strictly against the rule. That is rubber-stamping wearing the appearance of validation.

**Concrete rule for every review pipeline in this system:** the reviewer's context is constructed from the diff plus the rule files only. The planning conversation, the implementation chat history, and any "for background" framing are explicitly excluded from what is passed to the validator agent — by construction, not by instruction.

### Why this matters more than it looks

This is what makes the architecture self-sustaining rather than dependent on continuous human vigilance: the system relies on the server rejecting the push and validator agents that cannot be talked into agreement, not on a human catching every drift in review. Over a short session a human can hold the whole architecture in their head. Over a multi-month migration programme they cannot — the server and the isolated validators carry that load instead.

### Where this fits

Applies across every workstream that uses the two-branch strategy and fresh-agent-review pattern — Plan 4 onward, and is a prerequisite for Workstream E autonomy in the same way the graph hardening section above is: an autonomous loop is only as trustworthy as the enforcement it cannot talk its way around.

**Build items:**
- GitHub ruleset: define locked file/path list (architecture docs, contracts, DCB0129 documents, migration contracts), separate agent token excluded from bypass, admin-only bypass
- Fresh-agent-review pipeline: explicit context construction step that passes only diff + rules, with the planning conversation structurally excluded — not just told not to use it
