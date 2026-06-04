-- Make the project code uniqueness constraint ignore soft-deleted projects.
-- Previously the unique index covered every row, so a soft-deleted project's
-- code could never be reused — attempting to recreate it raised a DB-level
-- unique violation (surfaced as a 500). A partial index restricts uniqueness
-- to active (non-deleted) projects, allowing a deleted project's code to be
-- reused while still preventing duplicate active codes.

DROP INDEX IF EXISTS idx_uq_project_code;

CREATE UNIQUE INDEX idx_uq_project_code ON project (code) WHERE is_deleted = false;
