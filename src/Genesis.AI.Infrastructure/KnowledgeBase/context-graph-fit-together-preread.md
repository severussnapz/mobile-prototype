# Context Graph — Architecture Fit-Together Session

**Pre-read | Attendees: Idris, Luke, graph team | ~45 min**
**Purpose:** confirm the Roslyn + Neo4j repo graph stack fits the wider Genesis AI architecture before further build.

---

## Where we are

The team has tested graph construction options against EMIS Web. Findings:

- **Graphify — rejected for .NET.** Tested poorly against EMIS Web. Root cause is architectural: its C# support is tree-sitter (syntactic structure only), not semantic. For a codebase EMIS Web's age and size, that is not enough. Recorded as ADR-C2. Graphify is retained only for document graphs (manuals, Confluence).
- **Roslyn + Neo4j — validated, in progress.** Roslyn compiles the solution and resolves the full semantic model. Loading into Neo4j. Giving decent initial results. Teething issues being worked through — notably Neo4j default query/row limits needing override for EMIS Web scale.

This session is not to relitigate the tooling choice — that is settled. It is to confirm the stack fits the wider architecture and to close the open integration questions before we scale.

---

## Six questions to close

**1. Neo4j deployment model and sovereign boundary**
Self-managed on EC2, or AuraDB? Does the instance sit inside the VPC on the sovereign boundary? EMIS Web source code is EMIS IP — even though it is not patient data, it should not sit outside the boundary. Confirm the deployment posture and whether it meets the same data-residency bar as the rest of the stack.

**2. Roslyn extraction schema**
Is there a defined node/edge schema for what Roslyn extracts, or is it emergent from the extractor? This matters because the EMIS-specific enrichment layers — clinical namespace tagging, strangler fig classification, Designer co-change tracking — attach to that schema. If the schema is emergent it will move under us. We want a stable, documented node/edge contract before enrichment is built on top.

**3. Legacy and non-Roslyn code coverage**
Roslyn covers the C#. EMIS Web has older code — VB6 and potentially other non-Roslyn-parseable parts. How is that handled? This is a coverage gap to name explicitly now, not discover later. Options: separate extractor, structural-only indexing, or accept the gap and document it. Whatever the answer, the coverage metric per namespace (a design principle) must distinguish "not clinical" from "not indexed."

**4. Incremental update model**
Full nightly re-extract, or diff-based incremental? At EMIS Web size a full compile-and-re-parse may be too slow for a nightly cadence. If diff-based, how is change detection scoped, and how do we guarantee the graph never silently drifts from HEAD? The `generated_at` freshness guarantee depends on this.

**5. Neo4j at scale**
The query/row limit override is the immediate teething issue. Beyond that: index strategy on the most-traversed node labels, pagination for large result sets, and bounding on variable-length path queries so a deep traversal cannot blow up. What is the current thinking, and what has already bitten us?

**6. Fit with the wider Genesis AI architecture**
Three integration points to confirm:
- **MCP serving surface over Neo4j** — this is our build (not a generic graph-serving tool), because it must enforce Tier 1 constraints, attach confidence/coverage metadata, and emit the node IDs that feed decision provenance.
- **Provenance capture** — the MCP layer must return the node IDs that grounded each query so the pipeline can snapshot them onto artefacts and write the `Genesis-Graph-Nodes` commit trailer. Confirm the Neo4j query surface can return stable node identifiers.
- **Enrichment attachment** — where and how the clinical/strangler/security enrichment layers attach to the Roslyn-extracted graph in Neo4j.

---

## What good looks like coming out of this session

- Deployment model confirmed and on the sovereign boundary.
- A documented, stable Roslyn node/edge schema the enrichment layers can build on.
- Legacy code coverage gap named and a position taken.
- Incremental update cadence decided, with a freshness guarantee.
- Neo4j scale issues understood, not blocking.
- The three integration points (MCP serving, provenance, enrichment) agreed at a design level.

None of this blocks on TPG or Plans 3c/3d. It is internal architecture confirmation that lets the repo graph build proceed on a sound foundation.

---

## Reminder — what is not in scope for this session

The moat is not the graph construction. It is the EMIS-specific enrichment, decision provenance, Tier 1 certification, and pipeline-by-exception integration built on top. This session is about making sure the foundation (Roslyn + Neo4j) is sound so that build can proceed — not about redesigning the layers above it.

---

*Pre-read owner: Idris Issa | Companion to workstream-c-design.md v7.0*
