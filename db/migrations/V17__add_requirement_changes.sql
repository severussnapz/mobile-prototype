CREATE TABLE requirement_changes (
    id                               uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id                       uuid         NOT NULL REFERENCES projects(id),
    req_id                           varchar(50)  NOT NULL,
    change_type                      varchar(20)  NOT NULL,
    raising_pipeline                 varchar(50)  NOT NULL,
    raising_pipeline_conversation_id uuid         NULL,
    proposed_ac_text                 text         NULL,
    approved_ac_text                 text         NULL,
    human_edited                     boolean      NOT NULL DEFAULT false,
    rationale                        text         NOT NULL,
    status                           varchar(20)  NOT NULL DEFAULT 'pending',
    clinical_safety_impact           varchar(10)  NOT NULL DEFAULT 'none',
    ig_impact                        varchar(10)  NOT NULL DEFAULT 'none',
    security_impact                  varchar(10)  NOT NULL DEFAULT 'none',
    clinical_safety_reviewed         boolean      NOT NULL DEFAULT false,
    clinical_safety_reviewer         varchar(200) NULL,
    clinical_safety_reviewed_at      timestamptz  NULL,
    ig_reviewed                      boolean      NOT NULL DEFAULT false,
    ig_reviewer                      varchar(200) NULL,
    ig_reviewed_at                   timestamptz  NULL,
    security_reviewed                boolean      NOT NULL DEFAULT false,
    security_reviewer                varchar(200) NULL,
    security_reviewed_at             timestamptz  NULL,
    prototype_fragments_affected     text[]       NULL,
    approved_by                      varchar(200) NULL,
    approved_at                      timestamptz  NULL,
    undone_by                        varchar(200) NULL,
    undone_at                        timestamptz  NULL,
    undo_rationale                   text         NULL,
    created_at                       timestamptz  NOT NULL DEFAULT now(),
    created_by                       varchar(200) NOT NULL
);

CREATE INDEX idx_requirement_changes_project_id
    ON requirement_changes(project_id);

CREATE INDEX idx_requirement_changes_project_req
    ON requirement_changes(project_id, req_id);

CREATE INDEX idx_requirement_changes_pending
    ON requirement_changes(project_id, status)
    WHERE status = 'pending';
