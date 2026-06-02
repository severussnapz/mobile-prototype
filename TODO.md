# TODO

## High Priority

- [ ] **Deploy to Genesis platform** — Push into the genesis platform infrastructure. Needs: Terraform/IaC, ECS task definition, RDS PostgreSQL, S3 bucket, secrets in Secrets Manager, ALB target group, DNS entry, CI/CD pipeline.

- [ ] **CI/CD pipeline** — No GitHub Actions workflows yet. Need: build + test, guardrail analyser, Docker image publish to JFrog, deploy to dev/staging/prod.

## Medium Priority

- [ ] **API test coverage for scope restrictions** — 23 API tests skipped (`ExcludeFromScopeTest`) that verify per-scope auth (arch, pxd, clin). Need per-scope token request support in test framework.

- [ ] **ConversationStreamController complexity** — Suppressed ENG-009. The SSE streaming + tool-loop orchestration is ~350 lines. Consider extracting tool execution into a separate service class.

- [ ] **BedrockAiService complexity** — Suppressed ENG-009. `StreamWithToolsAsync` handles the multi-turn tool loop with `yield return`. Difficult to decompose but could benefit from a tool dispatcher pattern.

- [ ] **Second seed project** — Only one seed project exists (Prototype in-progress). Consider adding a second project with more stages complete to test downstream flows.

## Low Priority / Tech Debt

- [ ] **No JSON:API** — Project uses standard REST JSON (API-001 suppressed). JSON:API doesn't align with SSE streaming responses. Revisit if non-streaming endpoints need to integrate with JSON:API-consuming frontends.

- [ ] **No distributed tracing** — EMIS-Request-Id middleware suppressed (API-006). Fine for single-service, but needed if the platform grows to multiple services.

- [ ] **No pagination on artefact endpoints** — Suppressed API-011. No hard limit enforced; may need pagination if artefact counts grow significantly.

- [ ] **`docker compose up` without `--build` runs stale code** — Developer footgun. Consider adding a `Makefile` or documenting that `--build` is always needed after code changes.

- [ ] **Stage skip doesn't validate artefacts** — `SkipStageCommandHandler` doesn't check whether skipping a stage breaks downstream dependencies. Currently safe because skip sets status to Complete, but semantically questionable.

- [ ] **Token cost estimation** — `ProjectTokenUsageResponse` includes `estimatedCost` but the pricing model is hardcoded. Should be configurable or fetched from AWS pricing API.
