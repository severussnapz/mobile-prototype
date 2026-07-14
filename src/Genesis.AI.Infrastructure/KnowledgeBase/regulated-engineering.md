# Skill: Regulated Engineering Judgement — Clinical/NHS Context

**Apply whenever:** designing, implementing, or reviewing anything in Genesis AI or any system touching NHS clinical data, DCB0129 obligations, or safety-relevant behaviour. Apply during trade-off decisions — this skill changes which option wins.

---

## The context that changes the maths

35 million patients, 3,500 GP practices, DCB0129 clinical safety obligations, UK GDPR Article 9, NHS data sovereignty (AWS Bedrock via PrivateLink — nothing leaves the VPC, no direct external API calls anywhere in the stack, including CI). In this context a silent drift between a safety artefact and implemented behaviour is not a bug — it is a patient safety incident. That reweights every engineering trade-off below.

## Decision rules that differ from ordinary engineering

### Severity-over-probability
Never gamble on the frequency of a low-probability event whose severity is a broken safety-audit link or clinical harm. A rename that silently detaches a clinical-safety tag might be rare — but "untraceable a year and forty refactors later" is unacceptable at any frequency when the mitigation (stable IDs) is cheap. Invest in the durable form when severity is certain, even if frequency isn't.

### Strict-from-day-one gates
Retrofitting a gate onto a system in flight means every artefact produced under the loose gate becomes suspect — at ~500 engineers that is a mass correction exercise across live work, not a code change. When a gate will certainly be needed at scale, carry its complexity from day zero, while there is nothing to correct.

### Machine-readable over parse-the-prose
Any data a safety gate compares (pinned versions, provenance, classification state) lives in structured, queryable fields — never extracted from markdown by a parser. A safety decision must not depend on a regex holding.

### Fully-qualified references
A bare version int whose meaning depends on convention is half a reference. References that safety logic follows must be self-describing (filePath + version together), so no reader's convention-assumption can silently point them at the wrong thing.

## Human-in-the-loop is designed-in, not bolted-on

- The deterministic machinery handles what it can prove; everything it cannot prove routes to a human through channels that **already exist** — the parking lot, the CHANGE system, ratification gates. An ambiguous case (e.g. an unresolvable rename mapping) is not a design flaw needing a bespoke mitigation; it is a routine item for the existing intervention path.
- **Agent proposes, deterministic rule cross-checks (rule can only escalate, never suppress), qualified human ratifies.** The agent never marks its own homework on safety classification; the human is pulled in proportionate to genuine novelty, not raw throughput — otherwise the gate breeds rubber-stamping, which is worse than no gate.
- **Review beats add.** A qualified reviewer confirming a machine-populated list will catch a wrong entry; a reviewer facing a blank page will miss an absent one. Omission is invisible; a bad entry is visible. Always draft mechanically, ratify by the accountable role-holder, as a by-product of assessment they already perform.
- **The improvable layer and the safety floor are different layers.** Let drafts (tagging accuracy, mapping heuristics) be imperfect and tuned from accumulated corrections — because the ratification gate, which never moves, holds the floor. Never trade gate strictness for draft convenience.
- **BLOCKED means human escalation, never autonomous override in either direction.** An automated gate does not commit past a block, and does not silently reject without a human seeing why.

## Data handling absolutes

- No NHS numbers, patient identifiers, or Article 9 special-category data in: logs (any level), client-facing error messages, URLs/query strings, unencrypted DB fields.
- Guard error messages must not defeat themselves: never include an example that matches the pattern being blocked ("use 999 000 0000" inside a guard that rejects that very format creates a rejection loop).
- Test data uses obviously-fake identifiers (`NHS: XXXX`, `Patient-001`) — never real-format plausible values.
- Secrets: environment variables / AWS Secrets Manager only; masked-hint display (`••••a3f9`), write-only replacement fields; one-time plaintext on set, never on GET.

## Audit-trail thinking

Every judgement that matters must be reconstructable later: who classified, who ratified, when, against which version, and — when a machine proposed and a human corrected — **the delta between proposal and decision** (one record, two uses: regulator evidence and tuning corpus). Corrections ride existing feedback machinery; approval chains ride RBAC + Git. If a future investigator could not reconstruct why the system believed something, the design has an audit hole — fix the design, not the investigator's luck.
