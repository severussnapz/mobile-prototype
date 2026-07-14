# Flyway migration image for genesis-ai-requirements-api.
#
# Bakes the db/migrations SQL into a Flyway image so a migration runner can
# apply the schema to the Aurora PostgreSQL cluster before the app rolls out.
#
# On the EKS golden path this image is run by the shared emisx-service chart's
# `flyway-migration` Job, which Argo CD executes as a PreSync hook
# (Helm pre-install,pre-upgrade) — see gitops/overlays/dev/values.yaml `job`.
# This replaces the legacy Genesis v1 ECS migration task entirely (no ECS task
# definition, no db-migrations security group).
#
# Built and pushed to ECR as `genesis-ai-requirements-api:flyway-latest` by
# .github/workflows/build-flyway.yml. Connection details (FLYWAY_URL / user /
# password) are injected at run time from the chart job values — never baked in.
#
# 11.20.2-alpine is multi-arch (arm64 + amd64); we build arm64 to match the
# Graviton EKS node pool.
FROM centraluk.jfrog.io/glb-docker-vir/flyway/flyway:12.11.0-alpine

# Copy SQL migration scripts (build context is ./db, so this is db/migrations).
COPY migrations/ /flyway/sql/

# Flyway configuration. BASELINE_ON_MIGRATE lets Flyway adopt an existing
# (non-empty) schema at baseline version 0 rather than failing.
ENV FLYWAY_LOCATIONS=filesystem:/flyway/sql
ENV FLYWAY_BASELINE_ON_MIGRATE=true
ENV FLYWAY_BASELINE_VERSION=0

# Default to `migrate`; the chart job overrides command/args explicitly, but
# this keeps the image runnable standalone for local verification.
CMD ["migrate"]
