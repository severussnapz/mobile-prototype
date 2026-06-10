-- Add enum values required for new pipeline stages.
-- Backfill/update statements are intentionally in V7 to ensure a commit boundary
-- before using these new enum values.

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

ALTER TYPE stage_type ADD VALUE IF NOT EXISTS 'information_governance';
ALTER TYPE stage_type ADD VALUE IF NOT EXISTS 'security';
