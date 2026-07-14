# Genesis AI Pipeline — Prompt Quality Guide

## Purpose

This document trains teams on how to write effective prompts across the Genesis AI pipeline. It covers every stage where a human provides input — requirements, prototype generation, vibe edits, surgical edits, and pipeline stage conversations. The difference between a good prompt and a bad one is not length or politeness — it is ground truth. Ground truth is specific, verifiable information the model can anchor to. Words without ground truth are noise.

---

## The core principle — ground truth vs words

The model does not know your feature. It knows what you tell it. Every prompt falls somewhere on this spectrum:

**Words only (bad):** The model guesses. It produces something plausible but generic. It cannot be wrong because it had nothing to be wrong about.

**Ground truth (good):** The model has specific facts, constraints, and references to anchor to. It can be right or wrong. You can tell the difference.

Ground truth includes: requirement IDs, screen names, field names, workflow steps, NHS data standards, existing EMIS Web behaviour, acceptance criteria, error conditions, and user roles. It does not include adjectives like "clean", "modern", "intuitive", or "professional".

---

## P01 — Requirements Discovery

### What the model needs
The P01 agent derives requirements from EMIS Web behaviour. The better you describe the existing workflow, the more accurate the requirements will be.

**Bad prompt:**
> "We need a documents inbox for GPs."

The model has no idea what the current workflow looks like, what data is involved, who the users are, or what problems exist. It will produce generic inbox requirements that could apply to any system.

**Good prompt:**
> "GPs currently receive documents via Docman Connect and NHS Mail. Documents arrive as PDFs attached to HL7 v2 messages. The current EMIS Web inbox shows sender, document type, and received date. GPs must match the document to a patient record before filing. Unmatched documents sit in a separate queue. The filing step requires selecting a folder, visibility setting, and optionally a SNOMED code. We want REQ-001 to cover the unified inbox view and REQ-002 to cover the filing flow."

The model now has: data sources, message format, existing UI elements, user workflow steps, edge cases, and explicit requirement scope. It can produce accurate, traceable requirements.

### Ground truth for P01
- Current EMIS Web screen names and field names
- NHS message formats (HL7 v2, FHIR, GP2GP)
- User roles (GP, practice manager, receptionist, pharmacist)
- Existing workflow steps in sequence
- Pain points with the current flow
- Explicit scope — what is in REQ-001, what is in REQ-002

### Anti-patterns
- "Make it better than the current system" — no ground truth
- "Modern UX" — not a requirement
- "It should be intuitive" — not verifiable
- Describing the solution instead of the problem — "add a three-panel layout" is a design decision, not a requirement

---

## P02 — Prototype Demo Builder

### Starting a build — generation prompts

The prototype builder reads your approved requirements automatically. Your generation prompt should add what the requirements cannot capture: priority flows, layout intent, and visual references.

**Bad prompt:**
> "Build a prototype for the inbox."

The model will read the requirements and build something. It may be correct but it will make layout and priority decisions without guidance.

**Good prompt:**
> "Build a clickable prototype for the unified inbound inbox from REQ-001 and REQ-002. Priority flows: (1) GP triages an unmatched document and matches it to a patient, (2) GP files a matched document with a SNOMED code. The inbox should have a list view on the left showing patient name, document type, sender, and received date. Clicking a row opens a document preview panel on the right. Include the three-tab structure: All, Unmatched, Needs Review."

This gives the model: requirement references, priority flows in order, specific UI layout, column names, and tab names. It has ground truth to build from.

### What makes a good generation prompt
- Reference requirement IDs explicitly (REQ-001, REQ-002)
- State the 2-3 priority flows — the model will build these first
- Describe the layout in plain terms — "list on left, preview on right", "three columns", "tabbed navigation"
- Name specific fields and columns if they matter
- Attach a screenshot of the current EMIS Web screen if building a migration prototype
- State what should NOT be in scope — "exclude the audit trail for now"

### What does not help
- "Use good UX" — the model already tries to
- "Make it look like the real thing" — the EMIS-X UI kit is always applied
- Long descriptions of visual style without structural information
- Restating what is already in the requirements

---

## P02 — Vibe Edits

Vibe edits are free-text instructions sent to the model to change the existing prototype. The model reads the current HTML, applies your instruction, and saves the result.

### The ground truth test for vibe edits

Before sending a vibe edit, ask: "If someone else read this instruction, would they produce the same result?" If the answer is no, add more ground truth.

**Bad vibe edit:**
> "Make the inbox look better."

The model has no idea what "better" means. It will make a change — probably something harmless like adjusting padding — but it cannot know what you actually wanted.

**Bad vibe edit:**
> "Improve the filter."

Same problem. Improve how? What is wrong with it? What should it do differently?

**Good vibe edit:**
> "Add a dropdown filter for Document Type above the inbox table. Options: All Types, Referral, Discharge Summary, Lab Result, Clinic Letter, Radiology Report. When a type is selected, only rows matching that type should be visible."

This has: what to add, where to add it, what the options are, and what the behaviour should be. The model can produce this correctly.

**Good vibe edit:**
> "Change the Urgent priority indicator from a red dot to a red badge with the text 'Urgent'. Keep the Soon indicator as an amber dot."

Specific element, specific change, explicit exception for what to leave alone.

### Ground truth elements for vibe edits
- The exact element or section being changed ("the inbox table", "the file button", "the patient match panel")
- The specific change ("change the label from X to Y", "add a column for Z", "move the button to the top right")
- The expected behaviour if it involves interaction ("when clicked, show/hide X", "filter the list to rows where Y matches Z")
- What to leave unchanged if there is a risk of confusion

### Anti-patterns
- Single adjectives: "make it cleaner", "make it faster", "make it simpler"
- Describing feelings: "it feels cluttered", "it doesn't look right"
- Solution without context: "add a modal" — for what? triggered by what? containing what?
- Compound instructions in one message: "change the header colour, add a filter, and fix the table layout" — split these into separate edits

---

## P02 — Surgical Edits

Surgical edits are right-click edits on a specific element. You select the element and type an instruction. The model only sees the selected element — not the full prototype.

### Key difference from vibe edits

Because the model only sees the selected element, you do not need to specify where the change is. You do need to be precise about what to change and how.

**Bad surgical edit:**
> "Make this better."

The model sees one element. It has no idea what "better" means for that element.

**Bad surgical edit:**
> "Wire up the filter."

This is a logic/behaviour instruction. The model cannot wire up interactive behaviour from a single element's HTML — it has no access to the rest of the page. The model will ask for clarification. If it does, answer with a specific behaviour description, not a technical instruction.

**Good surgical edit (on a button):**
> "Change the label from 'File' to 'File to Care Record' and add a tooltip: 'File this document to the patient's care record'."

**Good surgical edit (on a table row):**
> "Add a status badge column after the Priority column. Badge text: Matched (green), Unmatched (red), Needs Review (amber)."

**Good surgical edit (on a heading):**
> "Change this from an h3 to an h2 and add a subtitle underneath: 'Documents awaiting triage and filing'."

### When surgical edit is the wrong tool

Use Start Over instead of surgical edit when:
- You want to change the overall layout
- You want to add a completely new section
- Multiple surgical edits in a row are failing
- The model keeps asking for clarification on the same element

---

## P02 — Start Over with Instructions

Start Over clears the conversation and rebuilds the prototype. If you have typed instructions before clicking Start Over, the model uses them as the brief.

### When to use Start Over
- The prototype structure is wrong and you want to rebuild
- You have done 3-4 vibe edits and the result is drifting from what you want
- You have a new design reference (screenshot, sketch) to work from
- A surgical edit keeps failing

### Good Start Over instructions
Include everything the model needs to rebuild correctly. It does not carry forward memory of the previous version.

**Bad Start Over instruction:**
> "Rebuild it but make it better."

**Good Start Over instruction:**
> "Rebuild the inbox with a three-column layout: navigation on the far left, document list in the centre, document preview on the right. Based on REQ-001, REQ-002, and REQ-003. Keep the four tab structure (All, Unmatched, Needs Review, Urgent) from the previous version. Move the triage action buttons (File, Forward, Match) into the preview column header, not the list rows. The list rows should be clickable to select — not have inline buttons."

This gives the model: layout, requirement scope, what to keep from the previous design, and a specific change from the previous version.

### Using image attachments with Start Over
Attach images before clicking Start Over. They persist across rebuilds — you do not need to re-attach.

Good image references to include in your instruction:
- "Use this screenshot as the layout reference — match the three-column structure but apply the EMIS-X UI kit"
- "The sketch shows the filing dialog — implement this flow as step 2 of the file button interaction"
- "Match the column order from this export"

Do not say "make it look exactly like this" — the prototype uses the EMIS-X UI kit and will not match a Figma design pixel-for-pixel. Reference the structural intent, not the visual detail.

---

## P04 — Design (API/DB)

The P04 agent produces API contracts and database schema. It reads your approved requirements and architecture artefact. Your role is to provide what those documents cannot capture: naming conventions, field-level constraints, and existing patterns to follow.

**Bad P04 prompt:**
> "Design the API for the inbox."

The agent will produce a generic REST API. It will make assumptions about naming, versioning, authentication, and field types that may not match your codebase.

**Good P04 prompt:**
> "The API follows RESTful conventions with kebab-case URL paths and camelCase JSON fields. Authentication is JWT via the existing EMIS-X auth service. The inbox endpoint must support pagination (page + pageSize query params, max 100 per page). NHS number must be validated as a 10-digit string with the standard Luhn check. Document type is an enum — values: Referral, DischargeSummary, LabResult, ClinicLetter, RadiologyReport. Unmatched documents have a null patientId. The filing action must be idempotent — filing the same document twice returns 200 with the existing record, not an error."

This gives the agent: naming conventions, auth model, pagination pattern, field validation rules, enum values, null handling, and idempotency requirement.

### Ground truth for P04
- URL naming conventions used in the existing codebase
- Authentication and authorisation model
- Pagination pattern (cursor vs page/pageSize, max limits)
- Field-level validation rules (NHS number format, SNOMED code structure, date formats)
- Enum values for any typed fields
- Idempotency requirements
- Soft delete vs hard delete convention
- Any existing endpoints that should be extended rather than replaced

### Anti-patterns
- "Design a good API" — the agent already tries to
- "Follow REST best practices" — too vague, every team has different conventions
- Not specifying enum values — the agent will invent plausible ones that may not match your domain model
- Not mentioning existing patterns — the agent cannot see your codebase

---

## P05 — PxD (Product Experience Design)

The P05 agent produces a product experience design review. It works from your approved prototype and requirements. Your role is to flag accessibility constraints, design system gaps, and UX decisions that need explicit sign-off.

**Bad P05 prompt:**
> "Review the design."

The agent will produce a generic UX review. It will not know which EMIS-X components are available, what the accessibility standard is, or what specific interactions need scrutiny.

**Good P05 prompt:**
> "The prototype uses the EMIS-X UI kit. Review against WCAG 2.1 AA. Known gaps: the document preview panel does not have a keyboard shortcut for filing — keyboard-only users must tab through to the File button. The priority badge colours (red/amber/green) must not rely on colour alone for users with colour blindness — check the icon pairing is consistent. The three-step filing dialog has not been tested with a screen reader — flag the modal focus management as a risk. The 'Needs Review' tab count badge needs an aria-label."

This gives the agent: the accessibility standard, specific components to scrutinise, known gaps, and explicit risks to flag.

### Ground truth for P05
- Accessibility standard (WCAG 2.1 AA is the NHS baseline)
- Which EMIS-X UI kit components are available vs custom-built
- Known interaction risks (modals, focus management, keyboard navigation)
- Colour-only indicators that need icon pairing
- Any user research findings about the current EMIS Web screen that should influence the design

---

## P03 — Architecture

The P03 agent reads your approved requirements and produces architecture artefacts. Your role in P03 is to surface constraints and decisions the requirements do not capture.

**Bad P03 prompt:**
> "Generate the architecture."

The model will produce a generic architecture. It will make assumptions about infrastructure, data patterns, and integration points.

**Good P03 prompt:**
> "The patient matching service must use the existing EMIS Web patient index — do not design a new one. Documents are received via Docman Connect API and NHS Mail. All inference runs through AWS Bedrock via PrivateLink — no external API calls. The filing action writes to the EMIS Web document store via the existing IM1 API. Highlight any dependency on the Spine for patient demographics."

This gives the model: hard constraints on existing services, integration points, data sovereignty requirements, and specific questions to address.

### Ground truth for P03
- Existing services that must be reused (not replaced)
- Data sovereignty constraints (NHS IG, IM1, Spine)
- Non-functional requirements (latency, availability, scale)
- Infrastructure constraints (AWS region, VPC, existing RDS clusters)
- Team boundaries — what is owned by another team and must be treated as a dependency

---

## P06 — Clinical Safety (DCB0129)

The P06 agent produces the clinical safety case. It needs ground truth about hazards — not general descriptions of safety.

**Bad P06 prompt:**
> "Check this is clinically safe."

The model will produce a generic safety case. It will not identify hazards specific to your feature.

**Good P06 prompt:**
> "The key hazard scenarios to assess: (1) a document is filed to the wrong patient due to a false-positive match on name and DOB, (2) an unmatched document is filed without a SNOMED code leaving the clinical record incomplete, (3) an urgent document is not actioned because the priority indicator was not noticed. The existing EMIS Web mitigating controls for patient matching are: NHS number mandatory for filing, two-factor confirmation required when confidence score is below 90%."

This gives the model: specific hazard scenarios, existing mitigating controls, and the severity context. It can produce a grounded DCB0129 analysis rather than a generic one.

---

## P07 — Information Governance

**Bad P07 prompt:**
> "Write the DPIA."

**Good P07 prompt:**
> "The feature processes NHS numbers, date of birth, full name, and document content (which may include sensitive clinical data). Data is received from external senders (hospitals, labs, Docman Connect) and stored in the EMIS document store. Retention: documents retained per GP records retention schedule (10 years minimum). Access: GP, practice manager, and designated reception staff only. The IM1 API call includes the EMIS organisation ODS code. Flag any cross-border transfer risk from the Docman Connect integration."

---

## P08 — Security

**Bad P08 prompt:**
> "Do a security review."

**Good P08 prompt:**
> "Authentication: JWT tokens issued by the EMIS-X auth service, 15-minute expiry, refresh token stored in HttpOnly cookie. The document filing endpoint accepts a file upload (PDF, max 10MB). External integrations: Docman Connect API (internet-facing, mutual TLS), NHS Mail (internal NHS network). Patient data (NHS number, DOB, document content) stored in RDS encrypted at rest. No data crosses the VPC boundary — all inference via Bedrock PrivateLink. Rate limiting: 100 requests per minute per practice. Flag the file upload endpoint as a priority — check MIME type validation and file size enforcement."

This gives the agent: auth model, token lifetime, cookie security, file upload constraints, external integration security posture, data storage and encryption, network boundary, rate limiting, and a specific priority area.

### Ground truth for P08
- Authentication mechanism and token lifetime
- Cookie security settings (HttpOnly, Secure, SameSite)
- File upload handling — accepted MIME types, size limits, virus scanning
- External integrations — which are internet-facing vs NHS-internal, and how they authenticate
- Data at rest encryption (RDS encryption, S3 server-side encryption)
- Data in transit — TLS versions, mutual TLS where required
- Network boundaries — what stays in the VPC, what crosses it
- Rate limiting and abuse prevention

---

## Summary — the prompt quality checklist

Before sending any prompt to any pipeline stage, check:

**Does it have ground truth?**
- Specific screen names, field names, or workflow steps
- Requirement IDs (REQ-001, REQ-002)
- Existing system names and integration points
- User roles and their permissions
- Acceptance criteria or expected behaviour

**Is it free of noise?**
- No adjectives without substance (better, cleaner, modern, intuitive)
- No solutions without context (add a modal, use a table)
- No compound instructions — one change per message

**Is it specific enough to be verifiable?**
- Could someone else produce the same result from this instruction?
- Would you know if the model got it wrong?
- Is the expected output described, not just the desired feeling?

---

## Quick reference — bad vs good

| Stage | Bad | Good |
|-------|-----|------|
| P01 | "We need a better inbox" | "GPs receive PDFs via HL7 v2. Current workflow has 4 steps. REQ-001 covers steps 1-2." |
| P02 generation | "Build a prototype for documents" | "Build REQ-001 and REQ-002. Priority flows: triage → match → file. List left, preview right." |
| Vibe edit | "Make the filter work" | "When Document Type dropdown changes, filter table rows to matching type only." |
| Surgical edit | "Fix this" | "Change label from 'File' to 'File to Care Record'. Add tooltip: '...'." |
| Start Over | "Rebuild it better" | "Rebuild with three-column layout. Keep four tabs. Move action buttons to preview header." |
| P03 | "Generate the architecture" | "Must reuse existing patient index. All inference via Bedrock PrivateLink. IM1 API for filing." |
| P04 | "Design the API" | "kebab-case URLs, camelCase JSON, JWT auth, page+pageSize pagination, idempotent filing." |
| P05 | "Review the design" | "WCAG 2.1 AA. Priority badges need icon pairing. Filing dialog modal focus management is a risk." |
| P06 | "Check it's safe" | "Hazard 1: wrong patient match. Mitigating control: NHS number mandatory. Assess residual risk." |
| P07 | "Write the DPIA" | "Data: NHS number, DOB, name, document content. Retention: 10 years. External sender: Docman." |
| P08 | "Do a security review" | "JWT 15min expiry, HttpOnly cookie, file upload PDF only 10MB, Docman mutual TLS, VPC boundary." |
