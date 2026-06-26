CREATE TYPE requirement_impact AS ENUM ('cosmetic', 'substantive');

CREATE TABLE ui_delta (
    ui_delta_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    project_id uuid NOT NULL,
    stage_id uuid NOT NULL,
    requirement_id varchar(100),
    target_id varchar(300) NOT NULL,
    file_path varchar(500) NOT NULL,
    operation_type varchar(100) NOT NULL,
    source_type varchar(100) NOT NULL,
    user_request text,
    before_summary text NOT NULL,
    after_summary text NOT NULL,
    requirement_impact requirement_impact NOT NULL,
    conversation_id uuid,
    message_id uuid,
    lock_batch_id uuid,
    locked_requirement_file_path varchar(500),
    locked_at timestamptz,
    created_by varchar(200) NOT NULL,
    created_at timestamptz DEFAULT now() NOT NULL,
    CONSTRAINT pk_ui_delta PRIMARY KEY (ui_delta_id),
    CONSTRAINT fk_ui_delta_project_id FOREIGN KEY (project_id) REFERENCES project (project_id) ON DELETE CASCADE,
    CONSTRAINT fk_ui_delta_stage_id FOREIGN KEY (stage_id) REFERENCES pipeline_stage (pipeline_stage_id) ON DELETE CASCADE,
    CONSTRAINT fk_ui_delta_conversation_id FOREIGN KEY (conversation_id) REFERENCES conversation (conversation_id) ON DELETE SET NULL,
    CONSTRAINT fk_ui_delta_message_id FOREIGN KEY (message_id) REFERENCES message (message_id) ON DELETE SET NULL
);

CREATE INDEX idx_ui_delta_project_requirement_locked
    ON ui_delta (project_id, requirement_id, locked_at);

CREATE INDEX idx_ui_delta_stage_id
    ON ui_delta (stage_id);

CREATE TABLE prototype_lock (
    prototype_lock_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    project_id uuid NOT NULL,
    stage_id uuid NOT NULL,
    locked_at timestamptz,
    locked_by varchar(200),
    updated_at timestamptz DEFAULT now() NOT NULL,
    CONSTRAINT pk_prototype_lock PRIMARY KEY (prototype_lock_id),
    CONSTRAINT fk_prototype_lock_project_id FOREIGN KEY (project_id) REFERENCES project (project_id) ON DELETE CASCADE,
    CONSTRAINT fk_prototype_lock_stage_id FOREIGN KEY (stage_id) REFERENCES pipeline_stage (pipeline_stage_id) ON DELETE CASCADE,
    CONSTRAINT uq_prototype_lock_project_id UNIQUE (project_id),
    CONSTRAINT uq_prototype_lock_stage_id UNIQUE (stage_id)
);

CREATE INDEX idx_prototype_lock_project_id
    ON prototype_lock (project_id);

CREATE INDEX idx_prototype_lock_stage_id
    ON prototype_lock (stage_id);
