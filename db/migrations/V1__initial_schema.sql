-- Genesis AI Requirements API — Initial Schema
-- Consolidated from V1_1 through V1_7

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Enum types
CREATE TYPE compliance_domain AS ENUM ('clinical_uk', 'generic', 'finance');
CREATE TYPE project_status AS ENUM ('discovery', 'in_progress', 'complete', 'archived');
CREATE TYPE stage_type AS ENUM (
    'requirements_discovery',
    'architecture',
    'design',
    'pxd',
    'clinical_safety',
    'planning',
    'normalisation',
    'prototype'
);
CREATE TYPE pipeline_stage_status AS ENUM ('not_started', 'in_progress', 'complete', 'blocked');
CREATE TYPE conversation_status AS ENUM ('active', 'paused', 'completed');
CREATE TYPE message_role AS ENUM ('user', 'assistant', 'system');
CREATE TYPE parking_lot_priority AS ENUM ('critical', 'high', 'medium');
CREATE TYPE parking_lot_status AS ENUM ('open', 'resolved', 'deferred');

-- Projects
CREATE TABLE project (
    project_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    code varchar(10) NOT NULL,
    name varchar(200) NOT NULL,
    description varchar(2000),
    compliance_domain compliance_domain NOT NULL,
    status project_status DEFAULT 'discovery'::project_status NOT NULL,
    created_by varchar NOT NULL,
    created_at timestamptz DEFAULT now() NOT NULL,
    updated_at timestamptz DEFAULT now() NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL,
    CONSTRAINT pk_project PRIMARY KEY (project_id)
);

CREATE UNIQUE INDEX idx_uq_project_code ON project (code);
CREATE INDEX idx_project_status ON project (status);

-- Pipeline stages
CREATE TABLE pipeline_stage (
    pipeline_stage_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    project_id uuid NOT NULL,
    stage_type stage_type NOT NULL,
    status pipeline_stage_status DEFAULT 'not_started'::pipeline_stage_status NOT NULL,
    iteration integer DEFAULT 1 NOT NULL,
    started_at timestamptz,
    completed_at timestamptz,
    completed_by varchar,
    sort_order smallint DEFAULT 0 NOT NULL,
    CONSTRAINT pk_pipeline_stage PRIMARY KEY (pipeline_stage_id),
    CONSTRAINT fk_pipeline_stage_project_id FOREIGN KEY (project_id) REFERENCES project (project_id) ON DELETE CASCADE
);

CREATE INDEX idx_pipeline_stage_project_id ON pipeline_stage (project_id);
CREATE INDEX idx_pipeline_stage_project_id_sort_order ON pipeline_stage (project_id, sort_order);

-- Conversations
CREATE TABLE conversation (
    conversation_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    stage_id uuid NOT NULL,
    status conversation_status DEFAULT 'active'::conversation_status NOT NULL,
    message_count integer DEFAULT 0 NOT NULL,
    created_at timestamptz DEFAULT now() NOT NULL,
    resumed_at timestamptz,
    current_phase integer DEFAULT 0 NOT NULL,
    phase_name varchar(100) DEFAULT 'mode_selection'::varchar NOT NULL,
    total_phases integer DEFAULT 12 NOT NULL,
    questions_asked integer DEFAULT 0 NOT NULL,
    estimated_total_questions integer,
    requirements_captured integer DEFAULT 0 NOT NULL,
    CONSTRAINT pk_conversation PRIMARY KEY (conversation_id),
    CONSTRAINT fk_conversation_stage_id FOREIGN KEY (stage_id) REFERENCES pipeline_stage (pipeline_stage_id) ON DELETE CASCADE
);

CREATE INDEX idx_conversation_stage_id ON conversation (stage_id);

-- Messages
CREATE TABLE message (
    message_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    conversation_id uuid NOT NULL,
    role message_role NOT NULL,
    content text NOT NULL,
    token_count integer,
    user_ern varchar(200),
    given_name varchar(100),
    family_name varchar(100),
    images jsonb,
    documents jsonb,
    created_at timestamptz DEFAULT now() NOT NULL,
    CONSTRAINT pk_message PRIMARY KEY (message_id),
    CONSTRAINT fk_message_conversation_id FOREIGN KEY (conversation_id) REFERENCES conversation (conversation_id) ON DELETE CASCADE
);

COMMENT ON COLUMN message.images IS 'Optional JSONB array of image attachments: [{data: base64, mediaType: string}]';
COMMENT ON COLUMN message.documents IS 'Optional JSONB array of document attachments: [{data: base64, mediaType: string, fileName: string}]';

CREATE INDEX idx_message_conversation_id ON message (conversation_id);
CREATE INDEX idx_message_created_at ON message (conversation_id, created_at);

-- Parking lot items
CREATE TABLE parking_lot_item (
    parking_lot_item_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    conversation_id uuid NOT NULL,
    content text NOT NULL,
    priority parking_lot_priority DEFAULT 'medium'::parking_lot_priority NOT NULL,
    status parking_lot_status DEFAULT 'open'::parking_lot_status NOT NULL,
    source_phase integer NOT NULL,
    resolved_at timestamptz,
    created_at timestamptz DEFAULT now() NOT NULL,
    CONSTRAINT pk_parking_lot_item PRIMARY KEY (parking_lot_item_id),
    CONSTRAINT fk_parking_lot_item_conversation_id FOREIGN KEY (conversation_id) REFERENCES conversation (conversation_id) ON DELETE CASCADE
);

CREATE INDEX idx_parking_lot_item_conversation_id ON parking_lot_item (conversation_id);
CREATE INDEX idx_parking_lot_item_status ON parking_lot_item (conversation_id, status);

-- Artefacts
CREATE TABLE artefact (
    artefact_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    project_id uuid NOT NULL,
    version integer DEFAULT 1 NOT NULL,
    file_path varchar(500) NOT NULL,
    s3_key varchar(1000),
    content_type varchar(100) NOT NULL,
    content text,
    size_bytes bigint,
    created_by varchar NOT NULL,
    created_at timestamptz DEFAULT now() NOT NULL,
    CONSTRAINT pk_artefact PRIMARY KEY (artefact_id),
    CONSTRAINT fk_artefact_project_id FOREIGN KEY (project_id) REFERENCES project (project_id) ON DELETE CASCADE
);

CREATE INDEX idx_artefact_project_id ON artefact (project_id);
CREATE INDEX idx_artefact_project_filepath ON artefact (project_id, file_path);

-- Token usage tracking per streaming turn
CREATE TABLE token_usage (
    token_usage_id uuid DEFAULT uuid_generate_v4() NOT NULL,
    conversation_id uuid NOT NULL,
    input_tokens integer NOT NULL,
    output_tokens integer NOT NULL,
    cache_read_input_tokens integer DEFAULT 0 NOT NULL,
    cache_write_input_tokens integer DEFAULT 0 NOT NULL,
    created_at timestamptz DEFAULT now() NOT NULL,
    CONSTRAINT pk_token_usage PRIMARY KEY (token_usage_id),
    CONSTRAINT fk_token_usage_conversation_id FOREIGN KEY (conversation_id) REFERENCES conversation (conversation_id) ON DELETE CASCADE
);

CREATE INDEX idx_token_usage_conversation_id ON token_usage (conversation_id);
