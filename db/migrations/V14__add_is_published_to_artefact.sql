ALTER TABLE artefact
ADD COLUMN is_published BOOLEAN NOT NULL DEFAULT TRUE;

CREATE INDEX idx_artefact_project_file_published_version
ON artefact(project_id, file_path, is_published, version DESC);
