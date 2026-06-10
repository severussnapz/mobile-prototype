-- Backfill Information Governance and Security pipeline stages.
-- Idempotent: safe to re-run without creating duplicate rows.

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Shift existing stage ordering for Normalisation and Planning.
UPDATE pipeline_stage
SET sort_order = CASE
    WHEN stage_type = 'normalisation'::stage_type THEN 9
    WHEN stage_type = 'planning'::stage_type THEN 10
    ELSE sort_order
END
WHERE stage_type IN ('normalisation'::stage_type, 'planning'::stage_type)
  AND (
      (stage_type = 'normalisation'::stage_type AND sort_order <> 9)
      OR (stage_type = 'planning'::stage_type AND sort_order <> 10)
  );

-- Backfill missing Information Governance rows per project.
WITH project_completion AS (
    SELECT
        project.project_id,
        EXISTS (
            SELECT 1
            FROM pipeline_stage
            WHERE pipeline_stage.project_id = project.project_id
              AND pipeline_stage.stage_type IN ('normalisation'::stage_type, 'planning'::stage_type)
              AND pipeline_stage.status = 'complete'::pipeline_stage_status
        ) AS has_later_stage_complete
    FROM project
)
INSERT INTO pipeline_stage (
    pipeline_stage_id,
    project_id,
    stage_type,
    status,
    iteration,
    started_at,
    completed_at,
    completed_by,
    sort_order
)
SELECT
    gen_random_uuid(),
    project_completion.project_id,
    'information_governance'::stage_type,
    CASE
        WHEN project_completion.has_later_stage_complete THEN 'complete'::pipeline_stage_status
        ELSE 'blocked'::pipeline_stage_status
    END,
    1,
    NULL,
    NULL,
    NULL,
    7
FROM project_completion
WHERE NOT EXISTS (
    SELECT 1
    FROM pipeline_stage
    WHERE pipeline_stage.project_id = project_completion.project_id
      AND pipeline_stage.stage_type = 'information_governance'::stage_type
);

-- Backfill missing Security rows per project.
WITH project_completion AS (
    SELECT
        project.project_id,
        EXISTS (
            SELECT 1
            FROM pipeline_stage
            WHERE pipeline_stage.project_id = project.project_id
              AND pipeline_stage.stage_type IN ('normalisation'::stage_type, 'planning'::stage_type)
              AND pipeline_stage.status = 'complete'::pipeline_stage_status
        ) AS has_later_stage_complete
    FROM project
)
INSERT INTO pipeline_stage (
    pipeline_stage_id,
    project_id,
    stage_type,
    status,
    iteration,
    started_at,
    completed_at,
    completed_by,
    sort_order
)
SELECT
    gen_random_uuid(),
    project_completion.project_id,
    'security'::stage_type,
    CASE
        WHEN project_completion.has_later_stage_complete THEN 'complete'::pipeline_stage_status
        ELSE 'blocked'::pipeline_stage_status
    END,
    1,
    NULL,
    NULL,
    NULL,
    8
FROM project_completion
WHERE NOT EXISTS (
    SELECT 1
    FROM pipeline_stage
    WHERE pipeline_stage.project_id = project_completion.project_id
      AND pipeline_stage.stage_type = 'security'::stage_type
);

-- Ensure canonical sort order for backfilled and pre-existing rows.
UPDATE pipeline_stage
SET sort_order = 7
WHERE stage_type = 'information_governance'::stage_type
  AND sort_order <> 7;

UPDATE pipeline_stage
SET sort_order = 8
WHERE stage_type = 'security'::stage_type
  AND sort_order <> 8;
