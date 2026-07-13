# Skill: Legacy Migration Discipline — Strangler Fig Execution

**Apply whenever:** planning or executing the migration of any EMIS Web capability to EMIS-X, designing the seam between old and new, verifying feature parity for a migrated clinical behaviour, or deciding decommission timing. The strangler fig is named in the strategy; this is the execution discipline that makes it safe.

---

## The strangler contract: three rules that never bend

1. **One system of record per data item at any moment.** During any capability's migration, exactly one side owns each piece of data — the other reads through or is a verified replica. Dual-write without a single owner is how patient records fork. The Genesis approach (EMIS-X FE over EMIS Web data layer, cloud-native storage introduced *for new data structures*) encodes this: old data keeps its old owner until a capability's cutover moves it.
2. **The seam is explicit and reversible.** Every migrated capability sits behind a routing decision (facade, flag, or gateway rule) that can send traffic back to the legacy path *instantly and without data loss* for its rollback window. A migration you can't reverse in minutes is a bet, not an engineering step.
3. **Capabilities migrate one at a time, smallest coherent unit first.** The unit is a user-meaningful behaviour with its data dependencies understood — not a code module. Each migrated capability must be independently valuable and independently reversible; a "big slice because it's all coupled" is a signal to decouple first, not to slice bigger.

## Characterise before you migrate (the legacy system is the spec)

A 25-year-old system's true behaviour is what it *does*, not what its documentation says — including the bugs practices have workflows built around:

- **Write characterisation tests against the legacy behaviour first**: capture what EMIS Web actually returns/does for the capability's real usage patterns, including edge cases and oddities. These tests are the parity contract the new implementation must meet — this is precisely the traceability Genesis exists to provide ("EMIS Web behaviour → deployed EMIS-X capability").
- **Bug-for-bug is a decision, not a default.** Each discovered legacy oddity gets an explicit ruling: preserve (workflows depend on it), fix (with a CHANGE record and user communication), or escalate (clinical-safety-relevant → the P06 route). A silently "fixed" behaviour that a practice depended on is a regression wearing a halo.
- **Mine the real usage**: which fields, codes, and paths are actually exercised in production data shapes the migration order and the test set. Migrating to the documented interface instead of the used interface is how parity gaps ship.

## Parity verification — trust arrives with evidence

- **Shadow/parallel run where feasible**: route real (or replayed) traffic to both implementations, compare outputs, investigate every divergence — each one is either a new-side bug, a characterisation gap, or a legacy oddity needing a ruling. Divergence rate trending to zero is the cutover evidence.
- **Cutover is gradual and observed**: pilot practices first (the P00 pilot/deployment process field exists for this), watched against the capability's error and correction rates, expanding on evidence. Big-bang cutover of a clinical capability is an unforced error.
- **Clinical safety rides the whole journey**: a migrated capability's DCB0129 case must cover the *transition states* (mid-migration, rollback, dual-running), not just the end state — the interim window is a real operating mode, and safety-by-arrangement caveats (design-integrity.md) apply with full force.

## Decommission is a phase, not an assumption

The strangler isn't done until the strangled branch is dead: legacy path traffic at zero (measured, over a full business cycle including period-end oddities), data ownership fully moved, the routing seam removed, and the legacy capability formally retired. Skipping decommission accretes a permanent dual-maintenance tax — the "migrated but never removed" capability is the most expensive kind. Track a decommission backlog with the same seriousness as the migration backlog.

## The compounding loop

Every migrated capability must feed the context graph (behaviour captured, decisions recorded, characterisation tests as executable documentation) so the next migration starts richer — the velocity-compounding claim is only true if this feedback step is treated as part of "done" for each capability, not as optional hygiene.
