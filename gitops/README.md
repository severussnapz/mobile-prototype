# GitOps — Genesis EKS onboarding

This directory holds the GitOps artefacts that deploy `genesis-ai-requirements-api`
onto the Genesis v2 EKS platform (`genesis-a`, `eu-west-2`, account `442042551704`)
via Argo CD, following the [Genesis go-live contract][golive].

## Layout

```
gitops/
├── base/
│   └── values.yaml                         # Shared structural Helm values (emisx-service chart)
└── overlays/
    ├── dev/                                # Continuous — image :latest, tracks main
    │   ├── application.yaml                # Argo CD Application (multi-source)
    │   ├── values.yaml                     # Dev image tag, replicas, full env (incl. RDS_*)
    │   └── manifests/application-stack.yaml # KRO ApplicationStack — S3, RDS, Bedrock, ECR, IAM
    ├── int/                                # Versioned on release (scaffold — pending infra)
    │   ├── application.yaml
    │   ├── values.yaml
    │   └── manifests/application-stack.yaml
    └── stg/                                # Versioned on release, human-gated (scaffold)
        ├── application.yaml
        ├── values.yaml
        └── manifests/application-stack.yaml
```

`dev` is live. `int` and `stg` overlays are scaffolded (versioned via
release-please) but not yet deployable — their `values.yaml` need real endpoints
once the int/stg ApplicationStacks are provisioned.

> **Database auth.** The API connects to Aurora using **RDS IAM database
> authentication** — no password, so there is no `ExternalSecret`. When
> `RDS_HOST` is present the API builds a passwordless connection string and
> signs short-lived IAM tokens via the EKS Pod Identity role
> (`DependencyInjection.AddPersistence`). Locally it falls back to
> `ConnectionStrings:DefaultConnection`. The DB login role `genesis_ai_app` and
> its `rds_iam` grant are created by Flyway migration `V26`.

> **Schema migrations (EKS).** Flyway runs as an **Argo CD PreSync hook** — the
> shared chart's `flyway-migration` Job (Helm `pre-install,pre-upgrade`) applies
> `db/migrations` to Aurora *before* the Deployment rolls. This replaces the
> legacy Genesis v1 ECS migration task (`db-migration-*` task definition +
> `run-flyway-migration.yml`) — there is no ECS task on the EKS path. The job
> image (`:latest` on the `-flyway` repo) is built by [`build-flyway.yml`](../.github/workflows/build-flyway.yml)
> from [`db/Dockerfile`](../db/Dockerfile). Because IAM auth is not
> usable until `V26` has run, the initial migration connects as the Aurora
> **master** user (`genesis_ai_app`, RDS-managed password), mounted from Secrets
> Manager via the CSI driver using Pod Identity. The `job` block lives in
> [overlays/dev/values.yaml](overlays/dev/values.yaml).

## What lives where

- **Workload** — the shared Helm chart `centraluk.jfrog.io/genesis-helm-rel-loc/emisx-service`,
  pinned to `1.1.0`. We ship values, not a bespoke chart.
- **AWS resources** — a single KRO `ApplicationStack` instance, one per env under
  `gitops/overlays/<env>/manifests`
  ([overlays/dev/manifests/application-stack.yaml](overlays/dev/manifests/application-stack.yaml)):
  S3 artefact bucket, Aurora PostgreSQL Serverless v2, a scoped Bedrock invoke
  policy, the ECR repository, and the pod-identity IAM role.
- **Schema migrations** — the chart's `flyway-migration` Job (configured in
  [overlays/dev/values.yaml](overlays/dev/values.yaml)), run by Argo as a
  PreSync hook from the `:latest` image (on the `-flyway` repo) built by `build-flyway.yml`.
- **Platform side (in `emisgroup/genesis`)** — the `AppProject`, the governed
  namespace `genesis-ai-requirements-api-ns` (with the Dynatrace inject label),
  and repository registration live in `platform/base/service-team-projects` and
  `platform/base/governance`, applied to dev/int/stg by every overlay.

## CI/CD (`.github/workflows/`)

Mirrors the `genesis-hello-world` reference; all jobs delegate to shared EMIS-X
templates in `emisgroup/emisx-platform-engineering`.

**Continuous (dev):**

- **`build.yml`** — on push to `main` under `src/**`: builds the production image,
  pushes it to ECR as `:latest` for dev (OIDC → ECR, arm64), then rolls dev (the
  `deploy-dev` job forces a rollout restart so pods re-pull `:latest`, since the
  mutable tag produces no manifest diff for Argo).
- **`build-flyway.yml`** — on push to `main` under `db/**`: builds the Flyway
  migration image (from [`db/Dockerfile`](../db/Dockerfile)) and pushes it to the
  `-flyway` repo as `:latest`.
- **`gitops-deploy-dev.yaml`** — on push to `main` under `gitops/**`: reconciles
  and restarts the dev Argo CD Application.

**Versioned (int/stg) — driven by release-please:**

- **`release-please.yml`** — on push to `main`: maintains the release PR; merging
  it tags a version and publishes a GitHub release.
- **`build-release.yml`** — on release published: builds immutable release-tagged
  app + flyway images for int and stg.
- **`gitops-deploy-int-stg.yaml`** — on release published: deploys int
  automatically, then stg gated by the `stg` GitHub Environment approval.

**Manual:**

- **`gitops.yaml`** — `workflow_dispatch` escape hatch (reconcile / drift check /
  force restart of any env).

## Status & outstanding work

**dev is live.** The ApplicationStack (S3, Aurora, Bedrock, ECR, pod-identity) is
provisioned and the API is serving. RDS IAM auth, Bedrock invoke, S3 artefact
storage and the Flyway PreSync migration all work.

Remaining before **int/stg** deploy:

1. **Provision the int/stg ApplicationStacks** (RDS, ECR, secret, SG, S3 in the
   int/stg accounts).
2. **Populate the int/stg `values.yaml`** — copy the full `env:` block from
   [overlays/dev/values.yaml](overlays/dev/values.yaml) and substitute the
   int/stg endpoints, secret ARNs, security-group ids, identity
   Authority/Audience and CORS origins. They carry only Dynatrace release tags
   today.
3. **GitHub Environments** `int` and `stg` (required reviewers on `stg`) with the
   per-env `GITOPS_URL` / `GITOPS_TOKEN`.

Housekeeping:

- **Owning AD group** — the `AppProject` `developer` role currently grants
  `emisgroup:emisx-platform-engineering`; swap for the owning team's AD group.
- **Chart value validation** — validate `base/values.yaml` keys against the
  pinned `emisx-service` chart version.

[golive]: https://github.com/emisgroup/emisx-engineering/blob/main/docs/03_internal-developer-platforms/01_genesis/04_golive.mdx
