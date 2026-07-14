CREATE TABLE push_failure_log (
    push_failure_log_uuid UUID NOT NULL DEFAULT uuid_generate_v4(),
    project_id            UUID NOT NULL,
    artefact_id           UUID NOT NULL,
    file_path             VARCHAR(500) NOT NULL,
    error_message         TEXT NOT NULL,
    failed_at             TIMESTAMPTZ NOT NULL,
    retry_count           INTEGER NOT NULL DEFAULT 0,
    resolved_at           TIMESTAMPTZ NULL,
    CONSTRAINT pk_push_failure_log PRIMARY KEY (push_failure_log_uuid)
);

CREATE INDEX idx_push_failure_log_project ON push_failure_log(project_id);
CREATE INDEX idx_push_failure_log_unresolved ON push_failure_log(project_id) WHERE resolved_at IS NULL;
