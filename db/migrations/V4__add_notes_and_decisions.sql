-- V3: Project notes and decisions
--
-- Standalone, project-scoped records for capturing freeform notes and
-- ADR-style decisions. These are never included in AI conversation context.

-- Notes ---------------------------------------------------------------------
CREATE TABLE project_note (
    project_note_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    project_id uuid NOT NULL,
    content text NOT NULL,
    author_ern varchar(200),
    author_given_name varchar(100),
    author_family_name varchar(100),
    created_at timestamptz DEFAULT now() NOT NULL,
    updated_at timestamptz DEFAULT now() NOT NULL,
    CONSTRAINT pk_project_note PRIMARY KEY (project_note_id),
    CONSTRAINT fk_project_note_project_id FOREIGN KEY (project_id) REFERENCES project (project_id) ON DELETE CASCADE
);

CREATE INDEX idx_project_note_project_id ON project_note (project_id);

-- Decisions (ADR-style) -----------------------------------------------------
CREATE TABLE project_decision (
    project_decision_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    project_id uuid NOT NULL,
    title varchar(200) NOT NULL,
    context text NOT NULL,
    decision text NOT NULL,
    consequences text NOT NULL,
    author_ern varchar(200),
    author_given_name varchar(100),
    author_family_name varchar(100),
    created_at timestamptz DEFAULT now() NOT NULL,
    updated_at timestamptz DEFAULT now() NOT NULL,
    CONSTRAINT pk_project_decision PRIMARY KEY (project_decision_id),
    CONSTRAINT fk_project_decision_project_id FOREIGN KEY (project_id) REFERENCES project (project_id) ON DELETE CASCADE
);

CREATE INDEX idx_project_decision_project_id ON project_decision (project_id);
