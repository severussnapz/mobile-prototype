-- V2: Move artefact content to object storage (S3 / LocalStack).
--
-- Artefact content is now stored in S3 and referenced by `s3_key`. The inline
-- `content` column is dropped; metadata (size, content_type, path, version)
-- remains in the database. Existing rows must already have `s3_key` populated
-- (the seed data and application write paths set it).

-- Ensure every existing artefact has a storage key before dropping content.
-- Backfills a deterministic key matching the application's key scheme:
--   projects/{project_id}/artefacts/{file_path}/v{version}
UPDATE artefact
SET s3_key = 'projects/' || project_id || '/artefacts/' || file_path || '/v' || version
WHERE s3_key IS NULL;

ALTER TABLE artefact ALTER COLUMN s3_key SET NOT NULL;
ALTER TABLE artefact DROP COLUMN content;
