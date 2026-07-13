# Skill: Security Engineering — Threat Modelling as Method

**Apply whenever:** designing or reviewing anything that crosses a trust boundary — a new endpoint, a new integration (GitHub, Figma, Bedrock), a new secret, a new data flow, or the P08 security stage itself. Apply at design time; a threat model after implementation is an audit, not a design input.

---

## The method (lightweight STRIDE, scaled to the change)

For the change under review, draw the data flow in your head or on paper: actors → entry points → processes → stores → external services. Then walk each element against the six threat classes:

- **Spoofing** — can a caller pretend to be someone else? (Who validates identity at this entry point? Genesis: the GitHub App JWT, RBAC on pipeline actions.)
- **Tampering** — can data be modified in flight or at rest by someone who shouldn't? (Integrity of artefacts between S3 and GitHub push; immutability of approved versions.)
- **Repudiation** — can an actor deny having done something? (This is why RBAC + Git = non-repudiable audit trail is a design principle, not decoration. Every approval must bind identity + timestamp + version.)
- **Information disclosure** — what leaks, and through which side channel? Logs, error messages, URLs, timing. (The NHS-data absolutes are this class's checklist; the *skill* is hunting the non-obvious channel — e.g. a masked-secret hint that reveals length, an error message that confirms an account exists.)
- **Denial of service** — what happens under abusive or accidental load? (Token caches with expiry margins, retry with backoff — Polly — rather than hot loops, 12MB file guards, read budgets on tool loops.)
- **Elevation of privilege** — can a lower-privileged path reach a higher-privileged capability? (Can a pipeline agent trigger an action reserved for a role-holder? Tool interception layers are the enforcement point.)

Scale the depth to blast radius: a new internal helper needs a glance; a new external integration or a new secret needs the full walk.

## Genesis-specific standing threats — check on every relevant change

1. **The sovereign boundary.** All inference through Bedrock via PrivateLink; nothing calls external APIs directly — including from CI. Any change introducing an outbound call must name its route through the boundary or be rejected.
2. **The agent as confused deputy.** A pipeline agent holds capabilities (save artefacts, push to GitHub) that a prompt-injected instruction could try to invoke. Untrusted content (uploaded files, fetched pages, artefact bodies) must never be able to *direct* tool use — tool interception and human gates are the mitigations; any new tool must state which one covers it.
3. **Secrets lifecycle.** Env vars / Secrets Manager only; never config files, never logs, never GET responses beyond a masked hint; write-only replacement fields; one-time plaintext on set. Any new secret follows the `AesSecretEncryptionService` pattern — no bespoke crypto.
4. **Dependency and supply chain.** A Dependabot count is triage input, not a verdict: classify each as prod-bundle vs dev/build tooling, exploitable-in-our-usage vs theoretical, and record the classification. "28 vulns, all dev tooling, none in prod bundle" is a real security judgement; "28 vulns" alone is noise. New dependencies require the ponytail challenge (can stdlib/existing deps do it?) *plus* a maintenance-health glance (maintained? typosquat-checked? licence?).

## What the P08 reviewer actually does (the role, not the checklist)

The Security reviewer's job at P08 is: confirm the threat model exists and matches the design; verify each identified threat has a named mitigation or an explicit, owned acceptance; ratify the SEC tags on elements the mechanical pass flagged (review-beats-add, same as CSO/IG); and check the mitigations are *testable* — a mitigation with no way to verify it is a hope. Their sign-off binds their identity to that judgement — the audit trail must show it.

## Review heuristics that catch real issues

- Follow every piece of user-controlled input from entry to storage/output — every hop either validates, encodes, or is explicitly trusted with a reason.
- Read every new error path and log statement asking "what does this reveal to someone probing?"
- Any authentication/authorisation check inside a loop or behind a cache: verify the cached decision can't outlive a revocation.
- Anything that parses (JSON, YAML, HTML, file uploads): confirm limits (size, depth, count) exist before the parse, not after.
- A guard/filter whose own error message contains an example matching the blocked pattern defeats itself — check guards for self-defeat.
