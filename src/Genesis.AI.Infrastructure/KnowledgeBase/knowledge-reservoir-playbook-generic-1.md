**A PRACTITIONER****'****S PLAYBOOK**

**The Institutional Knowledge**

**Reservoir**

From Raw Organisational Data to a Domain-Optimised LLM

*Extract · Graph · Retrieve · Fine-Tune*

2026

# Introduction

Every organisation accumulates institutional knowledge — in codebases, documents, customer interactions, operational records, and the decisions made by people who may no longer be there. This knowledge is the organisation's most valuable and least leveraged asset. It exists, but it is not accessible at scale.

This playbook describes a four-layer architecture for converting that dormant institutional knowledge into a compounding strategic asset: a Knowledge Reservoir that powers AI applications across the organisation, and ultimately produces a domain-optimised LLM that encodes what the organisation knows at the model weights level.

The pattern is industry-agnostic. The specific assets differ — a law firm's case history differs from a manufacturer's quality records — but the architecture is the same. What changes is the extraction method and the domain grounding, not the structure.

This playbook is written from the perspective of a practitioner who has built this in a regulated industry. The principles are proven. The technology components are mature. The challenge is integration and sequencing, not invention.

| **CORE THESIS** | *The value of a Knowledge Reservoir is not in retrieval alone. It compounds. Every interaction that consumes the graph enriches it. Every capability built on it makes the next one faster. The organisation that builds this first in its sector owns a moat that cannot be purchased — only grown.* |
| --- | --- |

# The Four-Layer Architecture

The Knowledge Reservoir is built in four sequential layers. Each delivers standalone value. Each is a prerequisite for the next. The organisation that implements all four owns a domain-optimised LLM that is genuinely differentiated from any generic model.

| **#** | **Layer** | **What It Does** | **Why It Matters** |
| --- | --- | --- | --- |
| 1 | Extraction | Get institutional knowledge out of wherever it lives into a processable, structured form | Without extraction, the knowledge exists but is inaccessible. This layer makes 25 years of institutional memory queryable. |
| 2 | Graph | Structure extracted artefacts into a traversable knowledge graph with typed relationships — not a flat index | A graph understands relationships. "This API contract was generated against this OpenAPI spec at this commit" is a graph relationship, not a search result. |
| 3 | RAG + Vector | Hybrid retrieval combining semantic similarity (vector) with structural traversal (graph) to inject the right context at the right time | Pure vector RAG loses structure. Pure graph loses semantics. Hybrid gives both — precise structural retrieval grounded by semantic ranking. |
| 4 | Fine-Tuning | Use the accumulated ground truth as training data for PEFT/LoRA fine-tuning of a base model — baking domain knowledge into model weights | Retrieval has latency and context limits. Fine-tuning makes common domain patterns instantaneous — the model knows them without needing to retrieve them. |

These layers are additive. An organisation can stop at Layer 2 and have a powerful graph-based knowledge system. Layer 3 adds AI application value. Layer 4 produces a genuinely differentiated model. Most organisations should start at Layer 1 and prove value incrementally before committing to Layer 4.

# Layer 1 — Extraction

The extraction layer answers one question: where does the organisation's institutional knowledge live, and how do we get it out in a form the graph can consume?

Organisational knowledge exists in three categories, each requiring a different extraction approach:

## Structured Data

Structured data lives in databases, APIs, and systems with defined schemas. It is the highest-confidence source because the structure is already there — extraction is a query, not an inference.

- Relational databases — SQL query → structured node ingestion. Schema introspection gives entity types and relationships automatically.

- APIs with structured responses — REST/GraphQL → JSON → node ingestion. Pagination and rate limiting are the only challenges.

- Git history — commits, diffs, PR descriptions, branch names, authors, timestamps. Fully machine-readable. The organisational decision history in code form.

- Ticketing systems (ServiceNow, Jira, Zendesk) — structured fields give category, resolution, affected system, priority, resolution time.

## Semi-Structured Data

Semi-structured data has partial structure — templates, consistent headings, recurring fields — but not a formal schema. Extraction requires a combination of pattern matching and LLM semantic extraction.

- Spreadsheets following consistent templates (audit logs, risk registers, compliance records)

- Email threads with standard formats (RFI/RFQ responses, contract negotiations)

- Meeting notes following agenda templates

- Configuration files and infrastructure-as-code (Terraform, Kubernetes manifests)

## Unstructured Data

Unstructured data is the largest category and the hardest to extract reliably. Documents, PDFs, presentations, images, and audio have no inherent structure. LLM semantic extraction is required, and every extracted relationship should be tagged as INFERRED rather than EXTRACTED — lower confidence, subject to human review.

- Design documents, architecture reviews, strategy papers

- Customer-facing documentation, user guides, training materials

- Regulatory submissions, compliance evidence, audit reports

- Meeting transcripts, recorded calls, video content (via transcription)

| **Asset Type** | **Common Formats** | **Extraction Method** | **Graph Value** |
| --- | --- | --- | --- |
| Codebase / Repos | Git repositories | AST extraction (Graphify, tree-sitter) — zero LLM calls on source | Call graphs, dependency edges, API contracts, implementation patterns |
| Domain Knowledge Base | Relational SQL database | Direct SQL ingestion with schema introspection | Domain ground truth — the organisation's authoritative knowledge of its subject matter |
| Customer/User Tickets | Structured ticketing API | API export → structured node ingestion with resolution edges | Failure history, resolution patterns, unmet needs, product gaps |
| Compliance Records | Excel / structured templates | Template-aware extraction → high-confidence nodes (already formally reviewed) | Regulatory evidence, risk assessments, formally approved mitigations |
| Process Documentation | PDF / Word documents | Document extraction → LLM semantic pass (INFERRED) | Behavioural specification — what the organisation does and expects |
| Design & Architecture | Confluence / HTML / Markdown | API export → document ingestion | Decision rationale, architectural constraints, rejected alternatives |
| Forward Capture | Pipeline / workflow outputs | Native — generated by AI systems, immediately graph-ready with full provenance | Compounding institutional knowledge with structured decision history |

| **EXTRACTION PRINCIPLE** | *Every extracted relationship must be tagged with its confidence level: EXTRACTED (from source via deterministic parsing — highest confidence), INFERRED (from LLM semantic analysis — medium confidence, surface for human review), or AMBIGUOUS (conflicting signals — flag, do not use for generation without human confirmation). The pipeline must know how much to trust each connection.* |
| --- | --- |

# Layer 2 — Graph

The graph layer is where extracted artefacts become institutional knowledge. A flat index answers "does this exist?" A graph answers "how does this relate to everything else, and what breaks if it changes?"

## Why Graph, Not Vector

Vector databases excel at semantic similarity — finding content that means something similar to a query. They are the wrong primary store for institutional knowledge because they lose structure. "Document Management depends on EMIS-X Auth v2.3, which was generated against OpenAPI spec commit abc123, and three downstream capabilities share that dependency" is a graph relationship. A vector store reduces it to proximity — losing the version, the direction, and the blast radius.

The correct architecture is graph-primary, vector-secondary. The graph holds structure and relationships. The vector index handles semantic similarity search within the graph. Neither alone is sufficient.

## Graph Structure

A Knowledge Reservoir graph has four node types and typed edges between them:

- Knowledge nodes — artefacts: documents, code files, API specs, tickets, records

- Concept nodes — entities: capabilities, services, processes, regulatory requirements

- Decision nodes — choices made: architectural decisions, requirement changes, risk acceptances

- People nodes — authors, reviewers, approvers — with temporal edges showing when they were involved

Edges are typed and directional: DEPENDS_ON, GENERATED_FROM, APPROVED_BY, SUPERSEDES, CONTRADICTS, VERSION_OF. The edge type is as important as the nodes it connects.

## The Capability Catalogue

The Capability Catalogue is a specialised component of the graph — a structured registry of reusable capabilities and cross-cutting concerns. Each entry includes: capability ID, type (cross-cutting or domain), implementation status, links to specifications, and dependency declarations.

The catalogue is the mechanism that makes the graph actionable in the SDLC pipeline. When building a new capability, the pipeline queries the catalogue before the first stage runs — surfacing what already exists and what can be referenced rather than rebuilt.

## Tooling

Two components are needed for the graph layer:

- Structural extraction — Graphify (open source, MIT licence) handles codebase structural graph extraction across 33 languages. It installs as a coding assistant skill, runs on-device with zero LLM calls on source code, and updates automatically via git hooks on every commit.

- Semantic enrichment — a custom Knowledge Graph Service adds the cross-capability relationships, version tracking, and domain ontology that structural extraction cannot derive alone. This is the bespoke build that reflects the organisation's specific domain model.

Graph storage: purpose-built graph databases (FalkorDB self-hosted on Kubernetes, or Neo4j Community Edition) are significantly more capable than relational databases for graph traversal queries. PostgreSQL with pgvector handles the vector similarity component.

# Layer 3 — Hybrid Retrieval (RAG + Vector)

Layer 3 makes the Knowledge Reservoir actionable at runtime. When an AI application needs context — a coding agent, a customer support chatbot, a document generation system — the retrieval layer surfaces the right knowledge at the right time without requiring a human to know where to look.

## Why Hybrid

Pure vector RAG (embed everything, find similar) loses structural relationships and degrades on technical content where exact identifiers matter more than semantic proximity. Pure graph traversal loses semantic similarity — it can only find what is explicitly connected. Hybrid retrieval uses both:

- Graph traversal — start from anchor entities (the capability being built, the ticket category, the regulation being cited), traverse typed edges to surface structurally related knowledge

- Vector similarity — rank the retrieved graph neighbourhood by semantic relevance to the current query, filtering noise and surfacing the most relevant ~800 tokens

The result is context that is both structurally grounded (the right version of the right artefact, with the right relationships) and semantically ranked (the most relevant parts of that artefact).

## Context Injection

The retrieval layer injects context into AI prompts as a structured block — not a raw dump of retrieved documents, but a curated set of typed relationships and artefact summaries ranked by centrality and relevance. A typical injection covers:

- Anchor entity summary — what the current capability/concept is and its status

- Cross-cutting dependencies — what it depends on, with version references

- Relevant precedents — similar capabilities built previously, their outcomes

- Failure evidence — support tickets and resolved issues in the same domain

- Regulatory constraints — applicable compliance requirements already assessed

This context block is typically ~800 tokens — small enough to fit within any model's context window, large enough to ground the output in institutional knowledge rather than generic training data.

## Vector Store Selection

For organisations at scale, Qdrant offers the strongest hybrid retrieval capability: native dense + sparse search (combining semantic vectors with BM25 keyword matching), HNSW with in-index filtering, and self-hosted deployment that satisfies data sovereignty requirements. For organisations starting out, pgvector on an existing PostgreSQL instance handles the vector component adequately and avoids infrastructure complexity.

The decision criterion is scale and query pattern. If the retrieval workload requires sub-second hybrid search across millions of nodes with complex filters (by version, domain, confidence level), Qdrant is worth the operational overhead. For most organisations starting this journey, pgvector is sufficient for 12-18 months.

# Layer 4 — Domain-Optimised LLM

Layers 1-3 produce a retrieval system — a Knowledge Reservoir that AI applications query at runtime. Layer 4 goes further: it uses the accumulated ground truth to fine-tune a base model, baking domain knowledge into the model weights rather than retrieving it on every call.

This is the most powerful and most complex layer. It should be attempted only after Layers 1-3 are operational and the organisation has accumulated sufficient ground truth from real usage.

## What Fine-Tuning Achieves

A fine-tuned domain model differs from a retrieval-augmented generic model in three ways:

- Speed — domain patterns are instantaneous. The model does not need to retrieve what it knows at the weights level.

- Coherence — the model's language, terminology, and reasoning style matches the organisation's domain without prompt engineering every interaction.

- Cost — fewer retrieval calls per interaction reduces both latency and infrastructure cost at scale.

What fine-tuning does NOT replace: retrieval. The model cannot encode every document, every ticket, every decision. Retrieval remains necessary for specific artefact content. Fine-tuning handles patterns and vocabulary; retrieval handles specific facts.

## The Training Data Pipeline

Fine-tuning requires curated instruction-response pairs — not raw graph data. The Knowledge Reservoir is the source, but a data preparation step converts it into training format:

- Pattern extraction — identify recurring question-answer patterns from the Reservoir (what is the auth pattern for X? → answer from capability catalogue)

- Failure mode encoding — encode what not to do from support ticket resolution history and corrected outputs

- Domain vocabulary — encode correct terminology, classification schemes, and regulatory language from compliance records

- Golden dataset curation — human review of extracted pairs to confirm accuracy before training. This is the quality gate that makes the fine-tuned model trustworthy.

## Technical Architecture

PEFT/LoRA (Parameter-Efficient Fine-Tuning / Low-Rank Adaptation) is the correct approach for most organisations. Full fine-tuning of a 70B parameter model requires significant compute and is rarely necessary. LoRA adapters train only a small number of additional parameters on top of a frozen base model — typically achievable on a single A100 GPU or equivalent AWS instance.

- Base model selection — Llama 3, Mistral, or Qwen for open-weight deployment. Choose based on domain language (some base models have stronger domain coverage), licence (for commercial deployment), and size (smaller models are cheaper to serve; larger models have better baseline capability).

- Adapter training — fortnightly cadence is practical for most organisations. More frequent training requires a fully automated pipeline; less frequent training risks the adapter drifting from the live domain.

- Judge model — a separate evaluation model trained on the golden dataset assesses every response. Pass → serve. Fail → block and flag for human review. This is the quality gate that makes the system safe in regulated environments.

- Drift detection — three types: adapter drift (base model updated by provider), domain drift (the organisation's domain changes), distribution drift (query patterns shift). Each triggers a different response: adapter retraining, golden dataset enrichment, or prompt update.

## Governance in Regulated Environments

In regulated industries (healthcare, financial services, legal), the fine-tuned model requires a governance framework that goes beyond standard ML ops:

- No sensitive data in training — the golden dataset contains patterns and anonymised corrections, never raw customer data or personal information

- Professional peer review — domain experts review and challenge model outputs; corrections feed back into the golden dataset. Reputation-weighted to prevent gaming.

- Audit trail — every training run is versioned with the dataset it was trained on, the base model version, and the evaluation results

- Risk-cautious by design — when uncertain, the model escalates rather than guesses. Better to say "I don't know" than to confabulate in a regulated context

| **KEY INSIGHT** | *The fine-tuned model is not the product. The governance layer above it is the product. Anyone can fine-tune a model on domain data. Only an organisation with a professional peer review network, a curated golden dataset, and a continuous drift detection system can make that model trustworthy in a regulated environment. That governance layer is the moat.* |
| --- | --- |

# Applications Beyond the Primary Use Case

The same Knowledge Reservoir built for the primary use case — typically an internal SDLC pipeline, a customer support system, or a knowledge management platform — delivers value across multiple additional applications. The investment in the reservoir is shared across all consumers simultaneously.

| **Use Case** | **Assets Consumed** | **Value Delivered** |
| --- | --- | --- |
| SDLC Acceleration | Capability catalogue, domain knowledge base, compliance records, codebase graph | Each development stage grounded in what already exists. Requirements, design, testing, and code generation reference institutional patterns rather than starting from scratch. |
| Customer Support Chatbot | Support ticket history with resolutions, product documentation, domain knowledge base | Chatbot grounded in real resolution history and actual product behaviour. Deflects L1/L2 tickets. Domain constraints prevent harmful or incorrect advice. |
| Commercial Responses (RFI/RFQ) | Capability catalogue, compliance evidence, delivery history, domain expertise | Commercial responses generated from verified evidence. Consistent claims across responses. Compliance credentials immediately citable with artefact references. |
| Strategic Planning | Support ticket patterns, capability gaps, domain knowledge trends, delivery velocity data | Strategy grounded in evidence of what customers need, what is missing, and how fast the organisation delivers. Pattern recognition across large datasets replaces intuition. |
| Engineering Onboarding | Codebase graph, architectural decisions, capability catalogue, support ticket history | "What does this service do and why was it built this way?" answered from the graph in seconds. Reduces onboarding time from weeks to days. |
| Regulatory Submissions | Compliance records, formally reviewed risk assessments, domain knowledge base, audit trail | Submissions assembled from pre-approved evidence with full provenance. Consistency across submissions. Audit trail demonstrates governance maturity. |

# Implementation Roadmap

The Knowledge Reservoir is built incrementally. Each phase delivers standalone value and is a prerequisite for the next. Do not attempt to build all four layers simultaneously.

| **Phase** | **Name** | **Layers Active** | **Capability** | **Investment** |
| --- | --- | --- | --- | --- |
| Phase 1 | Structural Foundation | Layer 1 (structured data) + Layer 2 (graph) | Queryable codebase graph, capability catalogue seeded, structured data sources connected | Low — existing tooling (Graphify), no new infrastructure beyond graph store |
| Phase 2 | Document Extraction | Layer 1 (unstructured) + Layer 2 (enriched) | Documents, compliance records, and process documentation queryable from graph. On-demand extraction triggered by usage. | Medium — document extraction pipeline, LLM semantic pass costs |
| Phase 3 | Hybrid Retrieval | Layer 3 (RAG + vector) | AI applications grounded by reservoir. Context injection at runtime. Customer support, SDLC pipeline, commercial responses all active. | Medium — vector store, retrieval API, prompt engineering for injection |
| Phase 4 | Domain LLM | Layer 4 (fine-tuning) | Domain-optimised model with governance layer. Fine-tuned adapter, judge model, drift detection, golden dataset pipeline. | High — GPU compute for training, governance infrastructure, expert review programme |

Phase 1 can be operational in 4-6 weeks. Phase 4 requires 6-12 months of reservoir operation to accumulate sufficient ground truth for meaningful fine-tuning. Attempting Phase 4 without the reservoir built by Phases 1-3 produces a generic domain model, not an institutional knowledge model.

## Common Failure Modes

- Big bang extraction — attempting to ingest all historical data before delivering value. Instead: start with structured data, deliver retrieval value, expand extraction on demand.

- Confidence inflation — treating INFERRED graph relationships as EXTRACTED. Always tag confidence. Never use inferred relationships for high-stakes generation without human review.

- Fine-tuning too early — training on insufficient ground truth produces a model that confidently reproduces common patterns but fails on edge cases. Wait until the reservoir has meaningful coverage.

- Ignoring drift — a fine-tuned model degrades silently as the domain evolves. Drift detection is not optional in regulated environments.

- Building without governance — the governance layer (peer review, golden dataset, judge model) is not a feature to add later. It is the foundation that makes the system trustworthy. Build it from Phase 1.

# Conclusion

Every organisation has institutional knowledge that took years to accumulate and is currently locked in formats that AI cannot access at scale. The Knowledge Reservoir architecture unlocks that knowledge in four sequential layers: extract it, structure it as a graph, make it retrievable at runtime, and ultimately bake the most important patterns into model weights.

The result is not just an AI system. It is a compounding asset. Every interaction enriches the reservoir. Every capability built makes the next one faster. Every support ticket resolved adds to the failure history that future applications avoid repeating.

The organisation that builds this first in its sector owns a knowledge moat that cannot be purchased from a vendor, replicated by a competitor, or replaced by a newer foundation model. It is built from what the organisation uniquely knows — and it grows every day the system runs.

| **FINAL PRINCIPLE** | *The foundation model is a commodity. Every competitor has access to the same one. The Knowledge Reservoir — extraction, graph, retrieval, and governance — is the layer that makes your AI applications genuinely different. Build the reservoir. Own the moat.* |
| --- | --- |

**THE INSTITUTIONAL KNOWLEDGE RESERVOIR — A PRACTITIONER****'****S PLAYBOOK**

2026 — Extract · Graph · Retrieve · Fine-Tune