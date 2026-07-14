-- V13: RDS IAM database user
-- Creates the application login role used by the API when deployed to AWS,
-- mirroring the ai-scribe-eval-api pattern.
--
-- The role is granted the `rds_iam` role ONLY when `rds_iam` exists (i.e. on an
-- AWS RDS / Aurora instance with IAM authentication enabled). On a local
-- PostgreSQL container that grant is skipped, so local development continues to
-- connect as the standard `postgres` superuser via the DefaultConnection
-- string. In AWS the API authenticates as `genesis_ai_app` using short-lived
-- IAM tokens (see DependencyInjection.AddPersistence).

-- Create the application login role (idempotent).
DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'genesis_ai_app') THEN
    CREATE ROLE genesis_ai_app WITH LOGIN;
  END IF;
END
$$;

-- Least-privilege data-plane grants on the existing schema objects.
GRANT CONNECT ON DATABASE genesis_ai_requirements TO genesis_ai_app;
GRANT USAGE ON SCHEMA public TO genesis_ai_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO genesis_ai_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO genesis_ai_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO genesis_ai_app;

-- Ensure objects created by future migrations are granted automatically.
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO genesis_ai_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO genesis_ai_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO genesis_ai_app;

-- Enable IAM authentication for this user (AWS RDS only; skipped in local dev).
DO $$
BEGIN
  IF EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'rds_iam') THEN
    GRANT rds_iam TO genesis_ai_app;
  END IF;
END
$$;
