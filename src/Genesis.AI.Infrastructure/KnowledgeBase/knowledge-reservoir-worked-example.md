**KNOWLEDGE RESERVOIR**

**Worked Example**

From Raw Data to Runtime Query

*Scenario: Meridian Capital — Commercial Lending Platform*

Extract · Graph · Store · Query · Inject

*All data, organisations, and individuals in this document are entirely fictional.*

# The Scenario — Meridian Capital

Meridian Capital is a fictional mid-size UK commercial lender with £4.2bn in assets under management. They process approximately 800 commercial loan applications per month across four product lines: term loans, revolving credit facilities, trade finance, and asset-backed lending.

Their technology estate consists of:

- LoanOS — a C#/.NET loan origination platform, 8 years old, 340,000 lines of code across 12 services

- RiskEngine — a Python credit risk assessment service with proprietary scoring models

- SupportDesk — a Zendesk-based customer support system with 47,000 resolved tickets over 5 years

- ComplianceVault — Excel-based FCA conduct rule evidence and Basel III capital adequacy records

- UnderwritingHub — PDF underwriting guidelines and credit policy documents

- Credit Knowledge Base (CKB) — a proprietary SQL database of lending rules, sector risk classifications, covenant definitions, and regulatory thresholds. 180 tables. Maintained by the Credit Risk team.

Meridian is building a new Trade Finance capability on their platform. They need to wire in their existing credit assessment logic, comply with FCA conduct rules, and reuse their existing covenant checking service rather than rebuilding it.

This document shows exactly how the Knowledge Reservoir makes that happen — tracing five raw data assets through extraction, graph storage, and runtime query injection.

| **HOW TO READ** | *Each section shows: (1) the raw data as it exists today, (2) the extraction process, (3) the resulting graph nodes and edges stored in the graph database, and (4) how those nodes are queried at runtime when the Trade Finance pipeline runs.* |
| --- | --- |

# Asset 1 — Credit Knowledge Base (CKB)

The CKB is a structured relational SQL database. It is Meridian's highest-confidence knowledge asset — maintained by qualified credit analysts, version-controlled via database migrations, and treated as the authoritative source for lending rules.

| **STEP** **1** | **Raw Data — What it looks like today** *A SQL table row from the CKB sector_risk_classifications table* |
| --- | --- |

The CKB contains a table called sector_risk_classifications with one row per UK SIC code sector. A representative row:

| -- Table: sector_risk_classifications -- Database: meridian_ckb (PostgreSQL 15) SELECT * FROM sector_risk_classifications WHERE sic_division = 'K'; sic_division     │ K division_name    │ Financial and Insurance Activities risk_tier        │ 2 max_ltv_pct      │ 65.00 max_exposure_gbp │ 25000000 covenant_set_id  │ CS-FIN-001 fca_conduct_flag │ true basel3_rwa_pct   │ 100.00 last_reviewed    │ 2026-03-15 reviewed_by      │ sarah.chen@meridiancapital.co.uk review_notes     │ Post-SVB review. LTV ceiling reduced from 70% to 65%. |
| --- |

| **STEP** **2** | **Extraction — How it leaves the CKB and enters the graph** *Direct SQL ingestion via schema introspection — no LLM involved* |
| --- | --- |

The CKB connector runs a schema introspection query to discover all tables and their foreign key relationships. It then ingests each table row as a structured graph node. No LLM is involved — this is deterministic SQL-to-graph mapping. Every relationship is tagged EXTRACTED (highest confidence).

| -- Introspection query (runs once on connector setup) SELECT table_name, column_name, data_type, is_nullable FROM information_schema.columns WHERE table_schema = 'public' ORDER BY table_name, ordinal_position; -- Row ingestion (runs on schedule / trigger) SELECT src.*, dst.covenant_name, dst.covenant_category FROM sector_risk_classifications src JOIN covenant_sets dst ON src.covenant_set_id = dst.set_id WHERE src.last_reviewed >= :last_extraction_timestamp; -- Output: structured JSON for graph node creation -- confidence: EXTRACTED -- source: meridian_ckb.sector_risk_classifications -- version: row hash SHA-256 |
| --- |

| **STEP** **3** | **Graph Storage — What gets stored and where** *FalkorDB (self-hosted on Kubernetes) — graph database nodes and edges* |
| --- | --- |

The extracted row becomes two graph nodes connected by a typed edge. The graph database is FalkorDB running on Meridian's Kubernetes cluster — entirely within their private network, nothing leaves the VPC.

**  NODE 1 — SectorRiskProfile  **

| **node_id** | SRP:K:v20260315 |
| --- | --- |
| **node_type** | SectorRiskProfile |
| **sic_division** | K |
| **division_name** | Financial and Insurance Activities |
| **risk_tier** | 2 |
| **max_ltv_pct** | 65.00 |
| **max_exposure_gbp** | 25000000 |
| **fca_conduct_flag** | true |
| **basel3_rwa_pct** | 100.00 |
| **last_reviewed** | 2026-03-15 |
| **reviewed_by** | sarah.chen@meridiancapital.co.uk |
| **confidence** | EXTRACTED |
| **source** | meridian_ckb.sector_risk_classifications |
| **version_hash** | sha256:a3f8c2... |

**  NODE 2 — CovenantSet  **

| **node_id** | CS:FIN-001:v20260101 |
| --- | --- |
| **node_type** | CovenantSet |
| **set_id** | CS-FIN-001 |
| **covenant_name** | Financial Sector Standard Covenants |
| **covenant_category** | SECTOR_SPECIFIC |
| **covenants_count** | 7 |
| **confidence** | EXTRACTED |
| **source** | meridian_ckb.covenant_sets |

**  EDGES  **

| **From Node** | **Edge Type** | **To Node** | **Properties** |
| --- | --- | --- | --- |
| SRP:K:v20260315 | REQUIRES_COVENANT_SET | CS:FIN-001:v20260101 | mandatory: true |
| SRP:K:v20260315 | REVIEWED_BY | Person:sarah.chen | date: 2026-03-15 |
| SRP:K:v20260315 | SUPERSEDES | SRP:K:v20250901 | reason: post-SVB LTV review |

| **STEP** **4** | **Runtime Query — How Trade Finance pipeline uses this** *Graph traversal at Stage 01 Requirements Discovery and Stage 06 Compliance* |
| --- | --- |

When the Trade Finance pipeline runs Stage 01 Requirements Discovery, the pre-flight capability query asks the graph: "What sector risk profiles apply to trade finance transactions?" Trade finance typically involves financial services counterparties (SIC K) and import/export businesses (SIC G). The graph returns:

| -- Cypher query (FalkorDB) MATCH (srp:SectorRiskProfile)-[:REQUIRES_COVENANT_SET]->(cs:CovenantSet) WHERE srp.sic_division IN ['K', 'G', 'H'] AND srp.fca_conduct_flag = true RETURN srp, cs ORDER BY srp.risk_tier ASC; -- Injected into Stage 01 prompt as structured context: -- "SIC Division K (Financial): Risk Tier 2, max LTV 65%, --  max exposure £25M, FCA conduct rules apply, --  Basel III RWA 100%, covenant set CS-FIN-001 mandatory. --  Last reviewed 2026-03-15 post-SVB. Previous LTV was 70%." |
| --- |

| **RESULT** | *The requirements agent knows the lending constraints before the BA writes a single requirement. The Trade Finance REQ-001 is generated with correct LTV ceilings, exposure limits, and covenant references pre-populated — not invented.* |
| --- | --- |

# Asset 2 — Support Ticket with Resolution

SupportDesk contains 47,000 resolved tickets. Each ticket encodes a real failure mode — something that broke in production, the customer impact, and how it was fixed. This is Meridian's failure history, and it is one of the most valuable inputs to the Knowledge Reservoir because it records what actually goes wrong, not what might go wrong.

| **STEP** **1** | **Raw Data — A resolved support ticket** *Zendesk ticket export — structured fields with free-text description and resolution* |
| --- | --- |

| {   "ticket_id": "ZD-48821",   "created_at": "2025-11-03T09:14:22Z",   "resolved_at": "2025-11-03T14:47:09Z",   "category": "Loan Origination",   "sub_category": "Credit Assessment",   "priority": "HIGH",   "reporter": "james.okafor@blueharbourcapital.co.uk",   "subject": "Credit score not refreshed before final approval — loan approved at wrong risk tier",   "description": "Submitted loan application MCL-2024-8821 for Blue Harbour Capital     on 2025-11-02. Credit assessment ran on 2025-10-28 (6 days prior). Between those     dates the borrower's credit profile changed materially. The approval email shows     risk tier 3 but the CKB now classifies this exposure as tier 2 (lower). We should     have been offered better terms. This is a conduct risk issue.",   "resolution": "Confirmed: credit assessment results older than 72 hours are not     being invalidated when the loan reaches final approval stage. CreditAssessmentService     v2.3.1 caches results with no TTL check at approval. Fixed in v2.3.2: TTL enforced     at final approval gate. All in-flight loans with stale assessments re-assessed.     FCA conduct rule COBS 2.1.1 cited — obligation to act in client best interests.",   "linked_code_change": "PR-4421 — enforce 72h TTL on credit assessment cache at approval",   "fca_conduct_flag": true,   "recurrence_count": 3 } |
| --- |

| **STEP** **2** | **Extraction — API export and structured ingestion** *Zendesk API → JSON → structured node ingestion. LLM semantic pass for resolution summary.* |
| --- | --- |

The Zendesk API exports tickets as JSON. Structured fields (ticket_id, category, priority, dates, fca_conduct_flag) are ingested deterministically — tagged EXTRACTED. The free-text description and resolution fields go through a lightweight LLM semantic pass to extract: the failure mode, the root cause, the fix, and the regulatory citation. These relationships are tagged INFERRED and surfaced for review before becoming graph edges.

| // Extraction pipeline (simplified) // Step 1: Structured field ingestion (EXTRACTED) ticket_node = {   node_id: "TICKET:ZD-48821",   ticket_id: "ZD-48821",   category: "Credit Assessment",   priority: "HIGH",   fca_conduct_flag: true,   resolution_time_hours: 5.55,   recurrence_count: 3,   confidence: "EXTRACTED" } // Step 2: LLM semantic pass on description + resolution (INFERRED) // Prompt: "Extract failure_mode, root_cause, fix, regulatory_citation" // Response: {   "failure_mode": "Stale credit assessment used at final approval",   "root_cause": "No TTL enforcement on assessment cache at approval gate",   "fix": "72-hour TTL enforced in CreditAssessmentService v2.3.2",   "regulatory_citation": "FCA COBS 2.1.1 — client best interests",   "confidence": "INFERRED" } |
| --- |

| **STEP** **3** | **Graph Storage** *Three nodes, four edges — failure pattern encoded in the graph* |
| --- | --- |

**  NODE 1 — SupportTicket  **

| **node_id** | TICKET:ZD-48821 |
| --- | --- |
| **node_type** | SupportTicket |
| **category** | Credit Assessment |
| **priority** | HIGH |
| **fca_conduct_flag** | true |
| **recurrence_count** | 3 |
| **resolution_hours** | 5.55 |
| **confidence** | EXTRACTED |

**  NODE 2 — FailurePattern  **

| **node_id** | FP:STALE-ASSESSMENT-APPROVAL |
| --- | --- |
| **node_type** | FailurePattern |
| **failure_mode** | Stale credit assessment used at final approval gate |
| **root_cause** | No TTL enforcement on assessment cache at approval stage |
| **fix** | 72-hour TTL enforced at approval gate |
| **recurrence_count** | 3 |
| **confidence** | INFERRED |
| **review_status** | CONFIRMED — reviewed by Credit Risk team 2025-11-10 |

**  NODE 3 — RegulatoryConstraint  **

| **node_id** | REG:FCA-COBS-2.1.1 |
| --- | --- |
| **node_type** | RegulatoryConstraint |
| **citation** | FCA COBS 2.1.1 |
| **description** | Obligation to act in client best interests |
| **applies_to** | Any automated decision that affects client terms |
| **confidence** | EXTRACTED |
| **source** | FCA Handbook — verified |

**  EDGES  **

| **From Node** | **Edge Type** | **To Node** | **Properties** |
| --- | --- | --- | --- |
| TICKET:ZD-48821 | EXHIBITS | FP:STALE-ASSESSMENT-APPROVAL | confidence: INFERRED |
| FP:STALE-ASSESSMENT-APPROVAL | VIOLATES | REG:FCA-COBS-2.1.1 | confidence: INFERRED, reviewed: true |
| FP:STALE-ASSESSMENT-APPROVAL | FIXED_BY | PR:4421 | confidence: EXTRACTED |
| TICKET:ZD-48821 | LINKED_TO | Service:CreditAssessmentService | confidence: EXTRACTED |

| **STEP** **4** | **Runtime Query — How Trade Finance pipeline uses this** *Stage 08 Security and Stage 06 Compliance pre-flight* |
| --- | --- |

| -- Query: known failure patterns in Credit Assessment domain MATCH (fp:FailurePattern)<-[:EXHIBITS]-(t:SupportTicket) WHERE t.category = "Credit Assessment" AND t.fca_conduct_flag = true AND fp.review_status STARTS WITH "CONFIRMED" RETURN fp ORDER BY t.recurrence_count DESC; -- Injected into Stage 06 Compliance prompt: -- "Known failure pattern: stale credit assessment at approval gate. --  Recurred 3 times. Root cause: no TTL on assessment cache. --  FCA COBS 2.1.1 violation. Required mitigation: enforce 72h TTL --  at final approval gate. See PR-4421 for implementation reference." |
| --- |

| **RESULT** | *The compliance agent does not need to hypothesise that stale assessments might be a risk. It knows this is a confirmed, recurring failure pattern that has already triggered an FCA conduct concern. The Trade Finance compliance record is generated with this mitigation pre-included — not discovered after launch.* |
| --- | --- |

# Asset 3 — Codebase (LoanOS Service)

The LoanOS codebase contains 340,000 lines of C# across 12 microservices. The credit assessment integration is in CreditAssessmentService. Graphify extracts the structural graph via AST analysis — zero LLM calls on source code.

| **STEP** **1** | **Raw Data — A C# service file** *CreditAssessmentService.cs — the service that validates loan applications against the CKB* |
| --- | --- |

| // CreditAssessmentService.cs // LoanOS / src / Services / CreditAssessment namespace LoanOS.Services.CreditAssessment {     public class CreditAssessmentService : ICreditAssessmentService     {         private readonly ICkbRepository _ckbRepository;         private readonly ILoanApplicationRepository _loanRepo;         private readonly IMemoryCache _cache;         private const int AssessmentTtlHours = 72; // PR-4421         public async Task<AssessmentResult> AssessAsync(             Guid loanApplicationId,             CancellationToken cancellationToken)         {             var loan = await _loanRepo.GetByIdAsync(loanApplicationId);             var sectorProfile = await _ckbRepository                 .GetSectorProfileAsync(loan.BorrowerSicCode);             if (loan.ExposureGbp > sectorProfile.MaxExposureGbp)                 return AssessmentResult.Reject("Exposure exceeds sector limit");             if (loan.RequestedLtv > sectorProfile.MaxLtvPct)                 return AssessmentResult.Reject("LTV exceeds sector ceiling");             return AssessmentResult.Approve(                 riskTier: sectorProfile.RiskTier,                 covenantSetId: sectorProfile.CovenantSetId);         }     } } |
| --- |

| **STEP** **2** | **Extraction — Graphify AST extraction** *Tree-sitter C# grammar — zero LLM calls. Entirely on-device.* |
| --- | --- |

Graphify runs tree-sitter against CreditAssessmentService.cs. It extracts: class definitions, interface implementations, method signatures, constructor injections (dependencies), and call graph edges. The source code never leaves the machine.

| // Graphify output (simplified) — graph.json entry {   "node_id": "Class:LoanOS.CreditAssessmentService",   "node_type": "Class",   "name": "CreditAssessmentService",   "namespace": "LoanOS.Services.CreditAssessment",   "implements": ["ICreditAssessmentService"],   "dependencies": [     "ICkbRepository",     "ILoanApplicationRepository",     "IMemoryCache"   ],   "methods": ["AssessAsync"],   "constants": [{"name": "AssessmentTtlHours", "value": "72"}],   "source_file": "src/Services/CreditAssessment/CreditAssessmentService.cs",   "git_commit": "a7f3c21",   "confidence": "EXTRACTED" } |
| --- |

| **STEP** **3** | **Graph Storage** *Service node with dependency edges and call graph* |
| --- | --- |

**  NODE — CodeService  **

| **node_id** | Class:LoanOS.CreditAssessmentService |
| --- | --- |
| **node_type** | CodeService |
| **name** | CreditAssessmentService |
| **namespace** | LoanOS.Services.CreditAssessment |
| **implements** | ICreditAssessmentService |
| **language** | C# |
| **assessment_ttl_hours** | 72 |
| **git_commit** | a7f3c21 |
| **confidence** | EXTRACTED |
| **source** | Graphify AST extraction |

**  EDGES  **

| **From Node** | **Edge Type** | **To Node** | **Properties** |
| --- | --- | --- | --- |
| Class:LoanOS.CreditAssessmentService | DEPENDS_ON | Interface:ICkbRepository | injection: constructor |
| Class:LoanOS.CreditAssessmentService | DEPENDS_ON | Interface:ILoanApplicationRepository | injection: constructor |
| Class:LoanOS.CreditAssessmentService | IMPLEMENTS | Interface:ICreditAssessmentService | confidence: EXTRACTED |
| Class:LoanOS.CreditAssessmentService | CALLS | CkbRepository.GetSectorProfileAsync | method: AssessAsync |
| Class:LoanOS.CreditAssessmentService | FIXED_BY | PR:4421 | field: AssessmentTtlHours |

| **STEP** **4** | **Runtime Query — Blast radius and reuse at Stage 04 Design** *What breaks if we change CreditAssessmentService? Who can reuse it?* |
| --- | --- |

| -- Blast radius query: what depends on CreditAssessmentService? MATCH (dependent)-[:DEPENDS_ON*1..3]->(svc:CodeService) WHERE svc.name = "CreditAssessmentService" RETURN dependent.name, dependent.node_type ORDER BY dependent.node_type; -- Returns: LoanApprovalOrchestrator, UnderwritingWorkflow, --          RiskReportingService (3 direct, 2 indirect dependents) -- Injected into Stage 04 Design prompt for Trade Finance: -- "CreditAssessmentService (ICreditAssessmentService) is available --  for reuse. Accepts: loanApplicationId (Guid). --  Returns: AssessmentResult with riskTier and covenantSetId. --  TTL: 72h. 5 existing dependents — changes have broad blast radius. --  Recommend: consume via interface, do not fork." |
| --- |

| **RESULT** | *The Trade Finance design agent knows the service exists, knows its interface contract, knows its TTL behaviour, and knows that 5 other services depend on it. It recommends consuming via the interface rather than forking — which is the architecturally correct answer.* |
| --- | --- |

# Asset 4 — Compliance Record (FCA Conduct Rule Evidence)

ComplianceVault contains Basel III and FCA conduct rule evidence in Excel format. These are formally reviewed records — high confidence because they have been through an internal audit process.

| **STEP** **1** | **Raw Data — An Excel compliance record row** *FCA_Conduct_Rules_Evidence_2026.xlsx — one row per conduct rule* |
| --- | --- |

| Sheet: COBS_Conduct_Rules Row 14: Rule Reference  │ COBS 2.1.1 Rule Name       │ Client Best Interests Description     │ A firm must act honestly, fairly and professionally in                 │ accordance with the best interests of its client. Applies To      │ All automated credit decisions affecting client terms Evidence Type   │ Policy + System Control Evidence Ref    │ CreditPolicy-v4.2, PR-4421 (system enforcement) Last Audit      │ 2026-01-20 Auditor         │ PwC Internal Audit Compliant       │ YES Next Review     │ 2027-01-20 Risk Rating     │ HIGH Notes           │ TTL enforcement added Nov 2025 following ZD-48821.                 │ Validated in Q4 2025 audit. |
| --- |

| **STEP** **2** | **Extraction — Template-aware Excel parsing** *Openpyxl reads known column structure. EXTRACTED for structured fields. No LLM needed.* |
| --- | --- |

ComplianceVault follows a consistent Excel template. The extraction connector knows the column structure and reads each row deterministically. No LLM is used — the template is well-defined. Every field is tagged EXTRACTED. The Notes field is passed through a lightweight LLM summarisation to extract the key event reference (ZD-48821 connection) — tagged INFERRED.

| **STEP** **3** | **Graph Storage** *Regulatory constraint node with audit and evidence edges* |
| --- | --- |

**  NODE — RegulatoryConstraint (enriched)  **

| **node_id** | REG:FCA-COBS-2.1.1 |
| --- | --- |
| **node_type** | RegulatoryConstraint |
| **citation** | FCA COBS 2.1.1 |
| **description** | A firm must act honestly, fairly and professionally in accordance with the best interests of its client |
| **applies_to** | All automated credit decisions affecting client terms |
| **compliant** | YES |
| **risk_rating** | HIGH |
| **last_audit** | 2026-01-20 |
| **auditor** | PwC Internal Audit |
| **next_review** | 2027-01-20 |
| **confidence** | EXTRACTED |
| **source** | ComplianceVault / FCA_Conduct_Rules_Evidence_2026.xlsx |

**  EDGES  **

| **From Node** | **Edge Type** | **To Node** | **Properties** |
| --- | --- | --- | --- |
| REG:FCA-COBS-2.1.1 | EVIDENCED_BY | Doc:CreditPolicy-v4.2 | type: policy |
| REG:FCA-COBS-2.1.1 | EVIDENCED_BY | PR:4421 | type: system_control |
| REG:FCA-COBS-2.1.1 | AUDITED_BY | PwC Internal Audit | date: 2026-01-20 |
| REG:FCA-COBS-2.1.1 | TRIGGERED_BY | TICKET:ZD-48821 | confidence: INFERRED |

| **ENRICHMENT** | *Note the TRIGGERED_BY edge connecting the regulatory constraint back to the support ticket that caused the compliance review. This cross-source relationship — ticket → regulatory constraint — is the graph**'**s unique value. Neither source alone would surface this connection. The graph makes it traversable.* |
| --- | --- |

# The Capability Catalogue — Credit Assessment Entry

The Capability Catalogue is a structured Markdown file stored in the LoanOS repository, versioned in git, and ingested by Graphify natively. It registers reusable capabilities and cross-cutting concerns so the pipeline can reference them rather than rebuild them.

Below is the full capability catalogue entry for Credit Assessment — a cross-cutting concern used by Term Loans, Revolving Credit, and now Trade Finance.

| # Capability: Credit Assessment # File: /capability-catalogue/cross-cutting/credit-assessment.md # Last updated: 2026-03-20 by platform-team ## Identity capability_id: CAP-CREDIT-001 name: Credit Assessment type: cross-cutting status: live version: 2.3.2 ## What it does Assesses a loan application against the Credit Knowledge Base (CKB). Returns: risk tier, covenant set ID, approval/rejection with reason. Enforces: sector exposure limits, LTV ceilings, TTL on assessment results. ## Interface Contract service: CreditAssessmentService interface: ICreditAssessmentService method: AssessAsync(loanApplicationId: Guid) -> AssessmentResult openapi_spec: /specs/credit-assessment/v2.3.2/openapi.yaml ## Key Constraints assessment_ttl_hours: 72 ttl_enforced_at: final_approval_gate ttl_added: PR-4421 (2025-11-03) ## Regulatory fca_conduct_rules: [COBS 2.1.1] compliance_evidence: ComplianceVault/FCA_Conduct_Rules_Evidence_2026.xlsx row 14 audit_status: COMPLIANT (PwC, 2026-01-20) ## CKB Dependency ckb_tables: [sector_risk_classifications, covenant_sets] ckb_query_pattern: GetSectorProfileAsync(sicCode) ## Known Failure Patterns failure_patterns: [FP:STALE-ASSESSMENT-APPROVAL] recurrence: 3 incidents (ZD-48821 and 2 prior) mitigation: TTL enforced at approval gate ## Consumers consumers: [LoanApprovalOrchestrator, UnderwritingWorkflow, RiskReportingService] ## Dependencies depends_on: [CAP-CKB-001, CAP-LOAN-REPO-001] |
| --- |

This file is stored in git alongside the codebase. Graphify ingests it as a document node on every commit. The structured fields (capability_id, status, version, openapi_spec) are extracted deterministically. The interface contract links to the OpenAPI spec which is ingested as a separate document node with its own endpoints and schema nodes.

# Putting It Together — Trade Finance Pipeline at Runtime

The Trade Finance pipeline starts. Before Stage 01 Requirements Discovery begins, the pre-flight capability query runs. Here is exactly what happens, in order.

| **STEP** **1** | **Pre-flight query** *Pipeline asks: what does Trade Finance depend on?* |
| --- | --- |

| -- Pre-flight query at pipeline start MATCH (cap:Capability) WHERE cap.name IN ["Credit Assessment", "Covenant Checking",                    "FCA Conduct Rules", "Loan Application"] OPTIONAL MATCH (cap)-[:HAS_FAILURE_PATTERN]->(fp:FailurePattern) OPTIONAL MATCH (cap)-[:CONSTRAINED_BY]->(reg:RegulatoryConstraint) OPTIONAL MATCH (cap)-[:IMPLEMENTED_BY]->(svc:CodeService) RETURN cap, fp, reg, svc; |
| --- |

| **STEP** **2** | **Graph returns** *Four connected subgraphs — one per dependency* |
| --- | --- |

The graph returns four subgraphs in ~12ms:

- Credit Assessment — status: live v2.3.2, interface contract, TTL constraint, FCA COBS 2.1.1 compliance status, known failure pattern with confirmed mitigation, CKB sector profiles for relevant SIC codes

- Covenant Checking — status: live v1.4.1, interface contract, covenant set CS-FIN-001 applicable

- FCA Conduct Rules — COBS 2.1.1 applies to automated decisions, compliant, last audited 2026-01-20

- Loan Application — status: live v3.1.0, repository interface, application data model

| **STEP** **3** | **Context injection** *~800 tokens injected into each relevant stage prompt* |
| --- | --- |

| // Context block injected at Stage 01 Requirements Discovery CROSS-CUTTING DEPENDENCIES IDENTIFIED: 1. Credit Assessment (CAP-CREDIT-001) — LIVE v2.3.2    Interface: ICreditAssessmentService.AssessAsync(loanApplicationId)    Returns: AssessmentResult { riskTier, covenantSetId, approved, reason }    Constraint: Assessment results expire after 72 hours.               Must re-assess if >72h elapsed before final approval.               (FCA COBS 2.1.1 — client best interests)    Known failure: Stale assessment approved at wrong tier (3x incidents).               Mitigation confirmed active in v2.3.2. 2. Trade Finance SIC Exposure Limits (from CKB):    SIC K (Financial): Risk Tier 2, max LTV 65%, max exposure £25M    SIC G (Wholesale trade): Risk Tier 3, max LTV 75%, max exposure £15M    All require covenant set CS-FIN-001. 3. FCA COBS 2.1.1 — COMPLIANT (PwC audit 2026-01-20)    Applies to all automated decisions affecting client terms.    Evidence: CreditPolicy-v4.2 + PR-4421 system control. RECOMMENDATION: Reuse ICreditAssessmentService. Do not fork. 5 existing consumers. Interface is stable. |
| --- |

| **STEP** **4** | **Stage outcomes** *What each stage produces differently because of the graph* |
| --- | --- |

The impact across the 10 stages:

- Stage 01 Requirements — BA does not specify credit assessment from scratch. REQ-001 references ICreditAssessmentService. LTV and exposure limits are pre-populated from CKB graph nodes. The 72h TTL requirement appears in REQ-003 without the BA needing to know the history.

- Stage 04 Design — API contract for Trade Finance references ICreditAssessmentService.AssessAsync directly. The OpenAPI spec for Trade Finance imports the AssessmentResult schema rather than redefining it.

- Stage 06 Compliance — FCA COBS 2.1.1 evidence is pre-populated. The known stale-assessment failure pattern appears as a required mitigation with confirmation that it is already addressed in v2.3.2. The compliance record is a delta, not a full exercise.

- Stage 09 Normalisation — Generated code consumes ICreditAssessmentService via dependency injection. It does not reimplement credit assessment. Guardrails confirm the interface contract is respected.

| **NET RESULT** | *The Trade Finance capability is built referencing four confirmed, live, compliant capabilities rather than rebuilding any of them. The known failure pattern is documented in the compliance record as a confirmed mitigation rather than discovered after launch. The FCA conduct rule evidence is pre-attached. The design agent produces a correct API contract on the first pass.* |
| --- | --- |

# Storage Summary — Where Everything Lives

The Knowledge Reservoir at Meridian Capital uses three storage systems. Each is chosen for what it does best.

| **System** | **Technology** | **What it stores** | **Why this system** |
| --- | --- | --- | --- |
| **Graph Database** | FalkorDB (self-hosted, Kubernetes) | All graph nodes and typed edges. Capability catalogue entries. Code structure. Regulatory constraints. Failure patterns. Ticket nodes. CKB-derived nodes. | Native graph traversal. Cypher queries. Blast radius, shortest path, community detection all native. Sub-20ms for typical pipeline queries. |
| **Vector Index** | pgvector on PostgreSQL (existing estate) | Semantic embeddings of node descriptions and document content. Used for similarity ranking within graph neighbourhoods. | Existing infrastructure. No new managed service. Sufficient at Meridian's scale. Upgrade to Qdrant when hybrid search at scale is needed. |
| **Document Store** | S3 (AWS, private bucket) | Raw source documents (PDFs, Excel files, OpenAPI specs). Graphify output files (graph.json, GRAPH_REPORT.md). Capability catalogue Markdown files. | Immutable audit trail of source documents. Graph nodes reference S3 keys for provenance. Documents never modified after ingestion — append only. |

The graph database is the primary query surface. The vector index handles semantic similarity within graph results — it is secondary, not primary. The document store holds the source evidence that graph nodes reference — it is the audit trail that proves where every node came from.

| **NOTHING LEAVES** | *All three storage systems run within Meridian**'**s private AWS VPC. FalkorDB runs on their EKS cluster. PostgreSQL is their existing managed RDS instance. S3 uses a private bucket with no public access. The graph traversal API (MCP server) is internal-only. No graph data, no source documents, and no extracted knowledge is transmitted outside the VPC boundary.* |
| --- | --- |

# Summary

This worked example traced five raw data assets through the complete Knowledge Reservoir pipeline:

- CKB sector risk classification → graph node → Cypher query → context injected at Stage 01 with correct LTV ceiling and exposure limit

- Support ticket ZD-48821 → failure pattern node → confirmed FCA conduct link → injected at Stage 06 as required mitigation with evidence that it is already fixed

- CreditAssessmentService.cs → code service node → blast radius edges → injected at Stage 04 as reusable interface contract

- FCA COBS 2.1.1 Excel row → regulatory constraint node → cross-linked to ticket and code fix → injected with audit evidence pre-attached

- Capability catalogue entry → structured graph node → queried at pipeline start → Trade Finance reuses Credit Assessment rather than rebuilding it

The Trade Finance pipeline never reinvents credit assessment. It never hypothesises that stale assessments might be a risk — it knows they have been a problem three times and knows the mitigation is confirmed active. The FCA conduct evidence is pre-attached before the compliance agent begins work.

That is the Knowledge Reservoir in practice. Not a search index. A compounding graph of institutional knowledge that makes every subsequent capability build faster, more accurate, and more compliant than the last.

**KNOWLEDGE RESERVOIR — WORKED EXAMPLE**

Meridian Capital — Fictional Scenario — 2026