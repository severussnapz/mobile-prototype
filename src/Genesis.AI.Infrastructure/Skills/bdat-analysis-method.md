# SKILL: bdat-analysis-method
# Phase: P03 Architecture — Phase 2

## BDAT Analysis Method (Per Requirement)

Analyse Business, Data, Application, and Technology dimensions for each requirement in scope.

### For EACH requirement:

Announce: "Analysing **REQ-{NNN}: {Name}**"

#### Business
1. "How does this support business processes?"
2. "Who are the primary users?"

#### Data
1. "What data does this read/write?"
2. "Relational or NoSQL?" → If relational: "Which tables?" If NoSQL: "Access pattern?"
3. "FHIR resources involved?" → If yes: "Which profiles?"
4. "Data flow?" → Source → Transform → Destination

> 🔴 **IG-003 GATE:** See `ig003-gate-p03` skill — applies to every requirement involving patient or clinical data.

#### Application
1. "Which service owns this?" → New or existing?
2. "API pattern?" → Sync (REST/JSON:API), Async (events), Real-time (WebSocket)?
3. "Main operations?" → List 2–5 endpoints
4. "How do other services interact?"
5. "Does this requirement produce backend tasks, frontend tasks, or both?"
   - **Backend only** → coding agent: `EMIS-X_API_ENGINEER` → guardrail prefixes: `SEC, ARCH, API, ENG, CS, DATA, PG, OBS, AUTH, TEST`
   - **Frontend only** → coding agent: `EMIS-X_WEBAPP_ENGINEER` → guardrail prefixes: `DS, WSEC, A11Y, WA, WCS, AD, CLIN, HTTP, WTEST`
   - **Both** → Split at Pipeline 08 layer boundary; record `v3_agents` assignment in the REQ file.

#### Technology
1. "AWS services for this requirement?" → Compute, database, storage
2. "Network architecture?" → Public/private subnets, ALB?

### Validation Format

```
"BDAT for REQ-{NNN}:
- Business: {Process, users}
- Data: {Types, database, FHIR, flow}
- Application: {Service, pattern, operations, integration, v3_agents}
- Technology: {AWS services, network}

Correct?"
```

### Immediate Write Rule

See `immediate-write-protocol` skill — write the Architecture section to the REQ file immediately after the user confirms "Correct" for each requirement.

### Repeat for All Requirements
