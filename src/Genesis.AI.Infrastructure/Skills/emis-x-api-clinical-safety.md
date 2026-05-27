---
name: emis-x-api-clinical-safety
description: >
  Clinical safety guardrails and steers for EMIS-X API microservices, derived
  from DCB0129 (Clinical Risk Management: its Application in the Manufacture
  of Health IT Systems). Use this skill when generating, reviewing, or
  modifying API code that handles patient data, clinical records,
  prescriptions, medications, allergies, diagnoses, test results, clinical
  decision support, or any health-related data — even when the user does not
  mention "clinical safety" directly. Rules are prefixed CLIN and must be
  satisfied by all generated code in clinical API services.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-api
    - requirements
    - emis-x-webapp
---

# EMIS-X API Clinical Safety Guardrails and Steers

This skill defines mandatory clinical safety guardrails and steers for EMIS-X
API microservices, grounded in **DCB0129** — the NHS standard for clinical risk
management in the manufacture of health IT systems. All generated code that
touches clinical data **must** satisfy every applicable rule. Apply these
proactively during code generation — not as a post-hoc review.

**Target versions:** ASP.NET Core 10.0, Entity Framework Core 10.0, MediatR
12.x.

**Standard:** DCB0129 — Clinical Risk Management: its Application in the
Manufacture of Health IT Systems (Amd 2020, release 4).

## Applicability

These rules apply to any EMIS-X API microservice that creates, reads, updates,
or deletes **clinical data**. Clinical data includes but is not limited to:

- Patient demographics and identifiers (NHS number, person GUID)
- Clinical records (consultations, encounters, episodes)
- Medications, prescriptions, and dosage information
- Allergies and adverse reactions
- Diagnoses, problems, and conditions
- Test results and investigations
- Referrals and clinical correspondence
- Clinical decision support outputs
- Immunisation and vaccination records

Services that exclusively handle non-clinical administrative data (e.g.,
user management, application configuration) are not subject to these rules,
unless those services gate access to clinical data (see CLIN-003).

## Rules Index

| ID       | Name                                | Type  | Severity | DCB0129 Section |
| -------- | ----------------------------------- | ----- | -------- | --------------- |
| CLIN-003 | Clinical Safety Classification      | Steer | Critical | §4 Hazard Identification |
| CLIN-004 | Fail-Safe Error Handling             | Steer | Critical | §7 Risk Control |
| CLIN-005 | Clinical Data Transactional Integrity | Steer | Critical | §7 Risk Control |
| CLIN-006 | Clinical Audit Trail                | Steer | High     | §8 Hazard Log / §11 Incident Management |
| CLIN-007 | Hazard Log Traceability             | Steer | High     | §4 Hazard Identification / §8 Hazard Log |
| CLIN-008 | Clinical Safety Test Coverage       | Steer | High     | §9 Clinical Safety Case |
| CLIN-009 | Safe Defaults                       | Steer | Critical | §7 Risk Control |
| CLIN-010 | Third-Party Clinical Risk           | Steer | Medium   | §7 Risk Control |

---

## CLIN-003: Clinical Safety Classification

**Type:** Steer

**Requirement:** Every code change to a clinical API service must be classified
for clinical safety impact. The agent must determine whether the change is
**safety-critical** (directly affects clinical data or clinical decision-making)
or **safety-adjacent** (affects infrastructure, access control, or data flow
that clinical features depend on). Non-clinical changes in clinical services
must still be acknowledged as operating within a safety-critical context.

This maps to DCB0129 §4 (Hazard Identification) — the requirement to
systematically identify hazards arising from the manufacture of a health IT
system, including hazards introduced by software changes.

**Severity:** Critical — unclassified changes risk introducing unidentified
hazards into clinical workflows.

**Exceptions:** None.

**Evidence Required:** State the clinical safety classification
(safety-critical, safety-adjacent, or non-clinical). For safety-critical
changes, identify the clinical data or clinical workflow affected. For
safety-adjacent changes, explain the indirect clinical dependency.

### Classification Guide

| Classification     | Criteria                                                   | Example                                              |
| ------------------ | ---------------------------------------------------------- | ---------------------------------------------------- |
| Safety-critical    | Directly creates, modifies, displays, or deletes clinical data | Adding a medication endpoint, modifying allergy logic |
| Safety-adjacent    | Affects authentication, authorisation, data access, or infrastructure that clinical features depend on | Changing auth policies that gate clinical endpoints, modifying database connection handling |
| Non-clinical       | No direct or indirect impact on clinical data or workflows | Updating Swagger documentation, modifying health check endpoints |

### Common Mistakes

| ❌ Wrong | ✅ Correct |
| -------- | --------- |
| Treat authorisation changes as non-clinical | Classify as safety-adjacent — broken auth can expose or block clinical data |
| Assume data migration scripts are non-clinical | Classify as safety-critical if they modify clinical data tables |
| Skip classification for "small" changes | All changes in clinical services require classification regardless of size |

---

## CLIN-004: Fail-Safe Error Handling

**Type:** Steer

**Requirement:** Clinical API endpoints must fail safely. When an error occurs
during a clinical operation, the API must:

1. Return an explicit error response (never a `200 OK` with empty or partial
   clinical data)
2. Never silently swallow exceptions that could hide clinical information
3. Never return partial clinical data sets without clearly indicating
   incompleteness
4. Log the failure with sufficient context for clinical incident investigation
   (without logging the clinical data itself — see SEC-003)

This maps to DCB0129 §7 (Risk Control) — the requirement to implement risk
controls that reduce clinical risk to an acceptable level. Silent failures in
clinical systems are a recognised hazard class because they can lead clinicians
to make decisions based on incomplete information.

**Severity:** Critical — silent failures in clinical APIs can directly lead to
patient harm through omission of critical clinical information.

**Exceptions:** None.

**Evidence Required:** Confirm all error paths return explicit error responses.
State how partial data scenarios are handled (either fail entirely or include a
completeness indicator). Confirm exceptions are logged with correlation context.

✅ **Compliant:**

```csharp
public async Task<IActionResult> GetMedications(
    Identifier<Patient> patientId,
    CancellationToken cancellationToken)
{
    var query = new GetMedicationsQuery(patientId);
    var result = await mediator.Send(query, cancellationToken);

    if (result.IsFailure)
    {
        logger.LogError(
            "Failed to retrieve medications for patient {PatientId}: {Error}",
            patientId,
            result.Error);

        return StatusCode(500, CreateErrorResponse("Unable to retrieve medication records."));
    }

    return Ok(result.Value);
}
```

❌ **Non-compliant:**

```csharp
public async Task<IActionResult> GetMedications(
    Identifier<Patient> patientId,
    CancellationToken cancellationToken)
{
    try
    {
        var query = new GetMedicationsQuery(patientId);
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }
    catch
    {
        return Ok(new List<MedicationDto>());
    }
}
```

### Partial Data Anti-Pattern

Never return a partial clinical record as if it were complete. If some data
sources are unavailable, either fail the entire operation or include metadata
indicating which sections are incomplete:

❌ **Non-compliant — partial data without indicator:**

```csharp
var allergies = await GetAllergiesAsync(patientId, cancellationToken);
var medications = new List<MedicationDto>();
try
{
    medications = await GetMedicationsAsync(patientId, cancellationToken);
}
catch (Exception)
{
    // Silently return empty medications — clinician sees no medications
    // and may incorrectly conclude the patient has none
}

return Ok(new PatientClinicalSummary(allergies, medications));
```

✅ **Compliant — fail entirely or indicate incompleteness:**

```csharp
var allergiesResult = await GetAllergiesAsync(patientId, cancellationToken);
var medicationsResult = await GetMedicationsAsync(patientId, cancellationToken);

if (allergiesResult.IsFailure || medicationsResult.IsFailure)
{
    logger.LogError("Incomplete clinical summary for {PatientId}", patientId);
    return StatusCode(500, CreateErrorResponse(
        "Unable to retrieve complete clinical summary. Some records are unavailable."));
}

return Ok(new PatientClinicalSummary(allergiesResult.Value, medicationsResult.Value));
```

---

## CLIN-005: Clinical Data Transactional Integrity

**Type:** Steer

**Requirement:** Operations that create, modify, or delete clinical data must
execute within a database transaction. Partial updates to clinical records are
unacceptable — either the entire operation succeeds or the entire operation is
rolled back. This includes multi-step operations such as creating a
prescription with its constituent medications, or updating a clinical record
with associated audit entries.

This maps to DCB0129 §7 (Risk Control) — partial writes to clinical data are a
hazard because they can leave records in an inconsistent state, leading to
incorrect clinical decisions.

**Severity:** Critical — partial clinical data writes can lead to incorrect
treatment decisions.

**Exceptions:** Read-only queries do not require explicit transactions.

**Evidence Required:** Confirm the operation uses the `TransactionBehaviour`
MediatR pipeline or explicit `IUnitOfWork.SaveChangesAsync` within a
transactional scope. Confirm no clinical data is persisted outside the
transaction boundary.

✅ **Compliant:**

```csharp
public class CreatePrescriptionCommandHandler(
    IPrescriptionRepository prescriptionRepository,
    IMedicationRepository medicationRepository)
    : IRequestHandler<CreatePrescriptionCommand, Identifier<Prescription>>
{
    public async Task<Identifier<Prescription>> Handle(
        CreatePrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var prescription = new Prescription(request.PatientId, request.PrescriberId);

        foreach (var item in request.Items)
        {
            prescription.AddMedication(item.DrugCode, item.Dosage, item.Frequency);
        }

        await prescriptionRepository.AddAsync(prescription, cancellationToken);

        return prescription.Id;
    }
}
```

The `TransactionBehaviour` in the MediatR pipeline ensures the entire operation
is committed or rolled back as a unit.

❌ **Non-compliant:**

```csharp
public async Task<Identifier<Prescription>> Handle(
    CreatePrescriptionCommand request,
    CancellationToken cancellationToken)
{
    var prescription = new Prescription(request.PatientId, request.PrescriberId);
    await prescriptionRepository.AddAsync(prescription, cancellationToken);
    await prescriptionRepository.SaveChangesAsync(cancellationToken);

    foreach (var item in request.Items)
    {
        var medication = new Medication(prescription.Id, item.DrugCode, item.Dosage);
        await medicationRepository.AddAsync(medication, cancellationToken);
        await medicationRepository.SaveChangesAsync(cancellationToken);
    }

    return prescription.Id;
}
```

---

## CLIN-006: Clinical Audit Trail

**Type:** Steer

**Requirement:** All operations that create, modify, or delete clinical data
must produce an immutable audit event. The audit event must capture:

- **Who** — the authenticated user identity (user ID, organisation context)
- **What** — the action performed (created, updated, deleted) and the affected
  entity type
- **When** — the UTC timestamp of the operation
- **Which record** — the identifier of the affected clinical record
- **Change detail** — for updates, the before and after state or the fields
  changed

Audit events must be raised as domain events within the aggregate, ensuring
they are captured within the same transaction as the clinical data change.

This maps to DCB0129 §8 (Hazard Log) and §11 (Incident Management) — the
requirement to maintain records that support incident investigation and
traceability of changes to clinical data.

**Severity:** High — missing audit trails prevent investigation of clinical
incidents and breach regulatory requirements.

**Exceptions:** None for mutable operations. Read-only queries do not require
audit events unless they access restricted clinical data (e.g., sexual health,
mental health records).

**Evidence Required:** Confirm a domain event is raised for each clinical data
mutation. State the audit event type and the data it captures. Confirm the
audit event is raised within the aggregate (inside the transaction boundary).

✅ **Compliant:**

```csharp
public class Prescription : Entity<Identifier<Prescription>>, IAggregateRoot
{
    public void Cancel(string reason)
    {
        Status = PrescriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;

        AddDomainEvent(new PrescriptionCancelledEvent(Id, reason));
    }
}
```

❌ **Non-compliant:**

```csharp
public class Prescription : Entity<Identifier<Prescription>>, IAggregateRoot
{
    public void Cancel(string reason)
    {
        Status = PrescriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        // No domain event — cancellation is unauditable
    }
}
```

---

## CLIN-007: Hazard Log Traceability

**Type:** Steer

**Requirement:** Changes to safety-critical code paths (classified as
safety-critical per CLIN-003) must include a reference to the relevant hazard
log entry in the commit message body or PR description. The format is
`HAZ-NNNN` (e.g., `HAZ-0042`). If no existing hazard log entry covers the
change, a new hazard must be raised with the Clinical Safety Officer (CSO)
before the change is merged.

This maps to DCB0129 §4 (Hazard Identification) and §8 (Hazard Log) — the
requirement to maintain traceability between identified hazards and the risk
controls implemented in code.

**Severity:** High — untraced changes to safety-critical code undermine the
integrity of the clinical safety case.

**Exceptions:** Non-clinical and safety-adjacent changes (as classified by
CLIN-003) do not require hazard log references, though safety-adjacent changes
should note the clinical dependency in the PR description.

**Evidence Required:** State the hazard log reference (`HAZ-NNNN`) for each
safety-critical change. If the change introduces a new potential hazard, state
that a new hazard log entry is required and describe the hazard for CSO review.

### Commit Message Example

```
feat(prescriptions): add cancellation endpoint

Implements prescription cancellation with reason capture and audit trail.

HAZ-0042: Prescription cancellation without clinical review
Risk control: Cancellation requires authenticated prescriber role and
mandatory reason field. Audit event raised on cancellation.
```

---

## CLIN-008: Clinical Safety Test Coverage

**Type:** Steer

**Requirement:** Safety-critical features (as classified by CLIN-003) must
include test cases that specifically validate clinical safety scenarios. At
minimum, safety-critical code must have tests covering:

1. **Input boundary conditions** — edge cases in clinical values (e.g., dosage
   limits, date ranges, NHS number validation)
2. **Error state handling** — verify fail-safe behaviour per CLIN-004 (errors
   produce explicit failures, not silent empty results)
3. **Data validation** — invalid clinical data is rejected before persistence
4. **Transactional integrity** — multi-step clinical operations roll back
   completely on failure
5. **Authorisation enforcement** — clinical endpoints enforce the correct
   authorisation policies

This maps to DCB0129 §9 (Clinical Safety Case) — the requirement to
demonstrate through testing that identified hazards have been adequately
mitigated by risk controls.

**Severity:** High — untested safety-critical code cannot be included in the
clinical safety case as evidence of risk mitigation.

**Exceptions:** None for safety-critical features.

**Evidence Required:** List the safety-specific test scenarios implemented.
Confirm coverage of boundary conditions, error states, data validation,
transactional integrity, and authorisation. State which hazard(s) the tests
provide evidence for.

✅ **Compliant test names:**

```csharp
// Boundary condition
public class CancelPrescription_WithExpiredPrescription_ReturnsValidationError { }

// Error state — fail-safe
public class GetMedications_WhenRepositoryThrows_ReturnsServerError { }

// Data validation
public class CreatePrescription_WithInvalidDosage_ReturnsValidationError { }

// Transactional integrity
public class CreatePrescription_WhenMedicationFails_DoesNotPersistPrescription { }

// Authorisation
public class CancelPrescription_WithoutPrescriberScope_ReturnsUnauthorised { }
```

---

## CLIN-009: Safe Defaults

**Type:** Steer

**Requirement:** When clinical configuration values are missing, invalid, or
ambiguous, the system must default to the **safest** behaviour — defined as the
behaviour least likely to lead to patient harm through action or omission. This
principle applies to:

- **Feature flags** — if a flag controlling a clinical safety feature (e.g.,
  allergy checking, interaction warnings) cannot be read, default to
  **enabled** (safety feature on)
- **Filtering and display** — if a filter configuration is missing, default to
  showing **all** clinical records rather than a filtered subset (preventing
  omission of critical information)
- **Validation thresholds** — if a dosage validation threshold cannot be loaded,
  default to the **strictest** threshold (reject rather than accept uncertain
  values)
- **Access control** — if an authorisation decision cannot be made (policy
  service unavailable), default to **deny access** (prevent unauthorised
  viewing of clinical data)

This maps to DCB0129 §7 (Risk Control) — the requirement that risk controls
reduce risk to an acceptable level. Unsafe defaults are a hazard because they
silently degrade safety when configuration is unavailable.

**Severity:** Critical — unsafe defaults can systematically suppress safety
features across the system.

**Exceptions:** None.

**Evidence Required:** For each configuration-dependent clinical safety
behaviour, state the default value and confirm it represents the safest option.
Justify any default that is not the most restrictive by explaining why the
alternative is safer in context.

✅ **Compliant:**

```csharp
public bool IsAllergyCheckingEnabled(string organisationId)
{
    var configValue = configuration.GetValue<bool?>(
        $"ClinicalSafety:AllergyChecking:{organisationId}");

    return configValue ?? true;
}
```

❌ **Non-compliant:**

```csharp
public bool IsAllergyCheckingEnabled(string organisationId)
{
    var configValue = configuration.GetValue<bool?>(
        $"ClinicalSafety:AllergyChecking:{organisationId}");

    return configValue ?? false;
}
```

---

## CLIN-010: Third-Party Clinical Risk

**Type:** Steer

**Requirement:** Introduction of a new third-party component (NuGet package,
external API, or shared library) into a safety-critical code path requires a
documented clinical risk consideration. The assessment must cover:

1. **What clinical data flows through or is processed by the component?**
2. **What happens if the component fails, returns incorrect results, or becomes
   unavailable?**
3. **Is the component maintained and does it receive security updates?**
4. **Does the component introduce any new hazards not covered by the existing
   hazard log?**

This maps to DCB0129 §7 (Risk Control) — the requirement to consider risks
arising from third-party components used within the health IT system.

**Severity:** Medium — third-party components in clinical paths are a known
source of latent hazards.

**Exceptions:** Third-party components used exclusively in non-clinical code
paths (build tooling, test frameworks, logging infrastructure) do not require
clinical risk assessment.

**Evidence Required:** Name the third-party component, state its role in the
clinical code path, and answer the four assessment questions above. If the
component introduces new hazards, state that a hazard log update is required.

---

## Gotchas

- **Patient identifiers are clinical data.** Even a service that only handles
  NHS numbers, person GUIDs, or patient demographic lookups is handling
  clinical data. Misrouting a patient identifier can lead to the wrong clinical
  record being accessed — this is a critical patient safety hazard.

- **Empty results are not the same as errors.** An empty allergy list means
  "this patient has no recorded allergies." A failed allergy lookup means "we
  don't know this patient's allergies." These have very different clinical
  implications. Never conflate the two.

- **Authorisation failures in clinical services are safety events.** A `403`
  from a clinical API is not just a security event — it may indicate that a
  clinician is unable to access information needed for patient care. Log these
  with clinical context, not just security context.

- **Database migrations on clinical tables are safety-critical changes.** A
  Flyway migration that modifies a clinical data table (adding columns,
  changing constraints, migrating data) requires CLIN-003 classification as
  safety-critical, even though no application code is changing.

- **Soft deletes vs hard deletes matter clinically.** Clinical records must
  never be hard-deleted. Regulatory requirements mandate retention of clinical
  records. Always use soft deletes (`deleted_at` timestamp) for clinical
  entities, and always include the soft-delete filter in queries to prevent
  displaying retracted records.

- **Timezone handling in clinical timestamps.** Clinical event timestamps must
  be stored in UTC and converted to local time only at the presentation layer.
  A medication administered "at 14:00" in one timezone is a different clinical
  event than "at 14:00" in another. Use `DateTime.UtcNow`, never
  `DateTime.Now`.

## Compliance Summary Template

When generating code for a clinical API service, include a compliance summary
covering each applicable rule:

```
Clinical Safety Compliance:
- CLIN-003: [safety-critical | safety-adjacent | non-clinical] — [justification]
- CLIN-004: [Fail-safe handling confirmed | N/A — read-only query]
- CLIN-005: [Transactional via TransactionBehaviour | N/A — read-only]
- CLIN-006: [Domain event raised: EventName | N/A — read-only query]
- CLIN-007: [HAZ-NNNN | New hazard required: description | N/A — non-clinical]
- CLIN-008: [Safety test scenarios: list | N/A — non-clinical]
- CLIN-009: [Safe defaults confirmed for: list | N/A — no config dependencies]
- CLIN-010: [No new third-party components | Assessment: component name]
```
