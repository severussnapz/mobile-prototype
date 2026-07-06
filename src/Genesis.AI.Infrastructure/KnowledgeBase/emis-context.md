# Genesis AI — EMIS Context

## The Mission

EMIS Web to EMIS-X is a complex migration — not because of scale alone, but because of constraints:

- **35 million patients** — data integrity non-negotiable
- **3,500+ GP practices** — zero disruption to clinical care
- **DCB0129 / DCB0160** — clinical safety compliance at every step
- **NHS data standards** — SNOMED, FHIR, HL7, dm+d
- **NHS integrations** — Spine, eRS, EPS, GP2GP, MESH
- **NHS contracts** — NPfIT obligations, GP IT framework
- **25+ years of clinical workflow** — understood, replicated, improved

Genesis AI is the multiplier that makes this migration achievable at pace. It is not the migration programme — it makes the programme faster, safer, and more traceable.

---

## The 2026 Delivery Commitment

Core GP capabilities delivered on EMIS-X by end of 2026.

**Approach: Strangler Fig**
- EMIS-X FE sits in front of EMIS Web data layer
- Capabilities migrated one at a time
- EMIS Web remains system of record until each capability proven
- No big bang migration — rollback always possible

**Data architecture:**
- EMIS Web storage: system of record for all existing patient data — unchanged
- Cloud-native storage: new data structures EMIS-X needs that EMIS Web never had (event streaming, workflow state, FHIR R4)
- Both work in tandem via abstraction layer
- Abstraction layer can combine data from both (service orchestration) — this is another team's responsibility, not Genesis AI

---

## The 30-Year IP Asset

EMIS has 30 years of clinical knowledge embedded in EMIS Web:
- Every decision support rule
- Every clinical workflow
- Every SNOMED mapping and synonym
- Every prescribing safety check
- Every patient record structure
- Every NHS integration behaviour
- Every edge case discovered in production

This is the moat. No competitor can replicate it. Genesis AI's context graph is how this IP becomes machine-readable and usable to accelerate the migration.

**The context graph is EMIS Web** — every screen, workflow, data model, clinical rule, and NHS integration dependency captured once and used forever.

---

## EMIS Group Structure

- **EMIS Group** — TPG Capital portfolio company (~59% UK GP market share)
- **EMIS Web** — legacy GP system, 3,500+ practices, 35M patients
- **EMIS-X** — next generation platform, cloud-native, modern UX
- **ProScript** — pharmacy system
- **PharmOutcomes** — pharmacy clinical services
- **ProxEmis** — independent EMIS entity (pharma clinical intelligence)
- **Engineering** — ~550 engineers across UK and India

---

## NHS Regulatory Framework

**DCB0129** — Clinical Risk Management: Manufacture of Health IT
- Hazard identification, severity, probability, mitigations, residual risk
- Genesis AI generates DCB0129 analysis by exception from existing hazard log
- Human clinical safety officer approves all outputs

**DCB0160** — Clinical Risk Management: Deployment and Use of Health IT
- Deployment-time safety requirements
- Applies to every practice deployment

**Data Standards:**
- SNOMED CT — clinical terminology (EMIS has unique 25-year synonym curation = MKB term blob)
- FHIR R4 — interoperability standard for EMIS-X
- HL7 — legacy messaging standard (EMIS Web)
- dm+d — drug and medicines dictionary

**NHS Integrations:**
- Spine — national NHS infrastructure (PDS, SCR, etc.)
- eRS — NHS e-Referral Service
- EPS — Electronic Prescription Service
- GP2GP — patient record transfer between practices
- MESH — Message Exchange for Social Care and Health

---

## Competitive Context

- **OneAdvanced + NVIDIA** — sovereign NHS LLM announced, active competitive threat
- **Doctolib-Medicus** — European clinical AI entrant
- **EMIS competitive advantage** — MKB term blob (25 years NHS-specific synonym curation), not the algorithm

---

## Key Principles for NHS AI

- Human always in the loop — non-negotiable
- Clinical safety compliance is not optional
- Data sovereignty — NHS patient data boundary enforced
- Deterministic-first — AI suggests, deterministic API executes
- Auditability — every change traceable from conversation to deployed code
