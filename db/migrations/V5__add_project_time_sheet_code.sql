-- Adds a required time sheet code to projects for cost/effort tracking (e.g. PORTASK0001045).
ALTER TABLE project ADD COLUMN time_sheet_code varchar(50);

-- Backfill existing rows so the column can be made NOT NULL.
UPDATE project SET time_sheet_code = 'UNKNOWN' WHERE time_sheet_code IS NULL;

ALTER TABLE project ALTER COLUMN time_sheet_code SET NOT NULL;
