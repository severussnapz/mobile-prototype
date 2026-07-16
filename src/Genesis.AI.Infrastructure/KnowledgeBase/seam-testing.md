# Skill: Seam Testing — Killing the Silent-Seam Failure Class

**Apply whenever:** adding any producer→consumer handoff — a new result field, a new command, a new artefact type, a new tool, a new endpoint, a new pinning/versioning mechanism. Apply during design review to ask "which seams does this change introduce?" and during code review to ask "does every introduced seam have a seam test?"

---

## The failure class

The most dangerous recurring defect in agent-built (and human-built) systems is not a bug in a component — it is a missing connection between two correct components:

- A handler computes a field → the result carries it → the response DTO omits it → the HTTP body silently drops it. **Green build, 900+ passing tests, field never reaches the client.**
- An artefact is generated, stored, and pushed → nothing ever reads it back. **A write-only artefact wearing a feature's name.**
- A command and handler are built → the controller route is never added. **"Unknown tool call" in production logs.**

The common shape: **producer and consumer are built in separate places (often separate sessions), each is internally correct and independently tested, and nothing tests the seam between them.** Unit tests pass on both sides because each side is coherent alone. The failure lives in the join — exactly what falls between two definitions of "done".

## Seam tests, not more unit tests

A seam test asserts a specific handoff completes end to end. It only passes if the *connection* holds. The five established seam types:

1. **Result → HTTP body completeness.** For every command/query result field, assert it appears in the serialised HTTP response. Reflection over the result type makes this a class-level guard: any new field with no DTO mapping fails automatically.
2. **Command → route existence.** For every command/query type, assert a controller route dispatches to it.
3. **Artefact write → read-back (stronger form).** For every re-consumed artefact type: an integration test that *writes the artefact, resumes the consuming context, and asserts the content is present where it should be* (e.g. in the rebuilt system prompt). The weak form — "a read path exists" — can pass while the read path is broken in ways it never exercises. Use the strong form. The set of re-consumed types is a hard-coded list, grown by hand (no registry until scale demands one).
4. **Tool registration → wiring.** Every tool in the tool definitions has a wiring test proving the execution path handles it.
5. **Pin → resolution.** Where a version-pinning mechanism exists, assert a pinned consumer receives the *pinned* version, not latest.


6. **API client verb → controller HTTP verb match.** For every API client method (`apiClient.get/post/put/patch/delete`), assert the controller action uses the matching HTTP verb attribute (`[HttpGet]/[HttpPost]/[HttpPut]/[HttpPatch]/[HttpDelete]`). Unit tests on either side cannot catch this mismatch — the client compiles against the TypeScript interface, and the controller compiles against C# attributes, with nothing verifying they agree. Only a real HTTP integration test making the call and asserting a non-405 response will catch it. Proved live: `projectNotesApi.update` and `projectDecisionsApi.update` used `PUT` but controllers used `[HttpPatch]` — 405 in production, zero unit test failures.

7. **Scope-level load → ownership-level mutation identity match.** When data is loaded at a broader scope than the mutation endpoint (e.g. project-level load of parking lot items, conversation-level delete), the item's owning identifier must be carried through to the mutation call. If the mutation uses the current context's identifier instead of the item's own identifier, the mutation will fail for any item not created in the current context. Proved live: parking lot items loaded at project level (`GET /projects/{id}/parking-lot`) but deleted using current `conversationId` — 404 for any item from a prior session. Fix: mutation receives the full resource object and uses `item.conversationId`, not the current conversation ID.

## Rules that keep the guard honest

- **Opt-outs are governed.** Reflection-based completeness tests need escape hatches for genuinely internal fields — and an ungoverned escape hatch becomes the new silent gap. Every opt-out requires a reason string and is itself reviewed.
- **A seam test only catches enumerated seams.** This set closes the known classes; it does not catch a novel seam type. The standing rule: **discovering a new class of seam failure means adding a new seam-test type to the family — never just fixing the instance.**
- **New feature = seam inventory first.** Before implementing, list the seams the change introduces (producer, consumer, what crosses). Each one gets a test in the RED phase.

## Design-time application

When reviewing a design, hunt for write-only artefacts and orphaned producers: anything that is "generated", "stored", "recorded", or "committed" must have a named consumer and a named moment of consumption. "Who reads this, and when?" is the question that found a real production gap (an artefact generated on session close and never re-injected on resume). If the design cannot answer it, the design has a seam hole.
