# GitOps — Genesis EKS onboarding

This directory holds the GitOps artefacts that deploy `genesis-ai-requirements-api`
onto the Genesis v2 EKS platform (`genesis-a`, `eu-west-2`, account `442042551704`)
via Argo CD, following the [Genesis go-live contract][golive].

## Layout

```
gitops/
├── base/
│   ├── values.yaml                     # Shared structural Helm values (emisx-service chart)
│   └── manifests/
│       └── application-stack.yaml      # KRO ApplicationStack — S3, RDS, Bedrock, ECR, IAM
└── overlays/
    └── dev/
        ├── application.yaml            # Argo CD Application (multi-source)
        └── values.yaml                 # Dev image tag, replicas, full env (incl. RDS_*)
```

Only `dev` is onboarded at this stage.

> **Database auth.** The API connects to Aurora using **RDS IAM database
> authentication** — no password, so there is no `ExternalSecret`. When
> `RDS_HOST` is present the API builds a passwordless connection string and
> signs short-lived IAM tokens via the EKS Pod Identity role
> (`DependencyInjection.AddPersistence`). Locally it falls back to
> `ConnectionStrings:DefaultConnection`. The DB login role `genesis_ai_app` and
> its `rds_iam` grant are created by Flyway migration `V13`.

## What lives where

- **Workload** — the shared Helm chart `centraluk.jfrog.io/genesis-helm-rel-loc/emisx-service`,
  pinned to `0.3.6`. We ship values, not a bespoke chart.
- **AWS resources** — a single KRO `ApplicationStack` instance
  ([base/manifests/application-stack.yaml](base/manifests/application-stack.yaml)):
  S3 artefact bucket, Aurora PostgreSQL Serverless v2, a scoped Bedrock invoke
  policy, the ECR repository, and the pod-identity IAM role.
- **Platform side (separate PR into `emisgroup/genesis`)** — the `AppProject`,
  the governed namespace `genesis-ai-requirements-api-ns`, and the gitops-token
  repository registration.

## CI/CD (`.github/workflows/`)

Mirrors the `genesis-hello-world` reference; all jobs delegate to shared EMIS-X
templates in `emisgroup/emisx-platform-engineering`.

- **`build.yml`** — on push to `main` under `src/**`, builds the production
  image and pushes it to ECR as `:latest` for dev (OIDC → ECR, arm64).
- **`gitops-deploy-dev.yaml`** — on push to `main` under `gitops/**`, reconciles
  the dev Argo CD Application.
- **`gitops.yaml`** — manual `workflow_dispatch` escape hatch (reconcile / drift
  check / force restart).

Deferred until int/stg/prd overlays are added: `build-release.yml` (release →
int/stg images) and the int-stg/prd gitops-deploy workflows.

## ⚠️ Outstanding dependencies / TODOs

These must be resolved before the stack syncs cleanly:

1. **KRO RDS branch dependency (blocking).** `ApplicationStack` support for
   `enableRDS` + the `rds` block (Aurora PostgreSQL) lives on the **unmerged**
   `emisx-platform-engineering` branch `feat/kro-rds-definition`. It must be
   merged and the ApplicationStack RGD re-published before this manifest
   reconciles. (`enableBedrock` is already merged — PR #1951.)

2. **DB IAM wiring.** RDS uses IAM database authentication, so the pod-identity
   role provisioned by the ApplicationStack needs the `rds-db:connect` action
   for db user `genesis_ai_app` (a responsibility of the KRO RDS composite on
   `feat/kro-rds-definition`). `RDS_HOST` in
   [overlays/dev/values.yaml](overlays/dev/values.yaml) is the Aurora writer
   endpoint (`rdsClusterEndpoint` output) and stays `REPLACE_WITH_...` until the
   stack provisions.

3. **Dev OIDC config.** `Authentication__Authority` / `Authentication__Audience`
   in [overlays/dev/values.yaml](overlays/dev/values.yaml) are placeholders.

4. **S3 bucket name.** `S3__ArtefactBucketName` in [base/values.yaml](base/values.yaml)
   assumes the composite derives the bucket name from the stack `instance`
   (`genesis-ai-requirements-api`). Confirm the actual provisioned bucket name.

5. **Owning AD group.** The `AppProject` `developer` role currently grants
   `emisgroup:emisx-platform-engineering`; add the owning team's AD group once
   confirmed.

6. **Chart value validation.** Validate the value keys in `base/values.yaml`
   against the pinned `emisx-service` chart `0.3.6`.

7. **ApplicationStack `apiVersion`.** This manifest uses `genesis.io/v1alpha1`,
   but the `genesis-hello-world` `feat/kro` reference branch uses
   `kro.run/v1alpha1`. Confirm which group the **published** RGD serves before
   the stack will pass admission.

[golive]: https://github.com/emisgroup/emisx-engineering/blob/main/docs/03_internal-developer-platforms/01_genesis/04_golive.mdx
