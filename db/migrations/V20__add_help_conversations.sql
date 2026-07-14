CREATE TABLE help_conversation (
    help_conversation_uuid UUID NOT NULL DEFAULT uuid_generate_v4(),
    project_id UUID NULL,
    user_ern VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_help_conversation PRIMARY KEY (help_conversation_uuid)
);

CREATE TABLE help_message (
    help_message_uuid UUID NOT NULL DEFAULT uuid_generate_v4(),
    help_conversation_id UUID NOT NULL REFERENCES help_conversation(help_conversation_uuid) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_help_message PRIMARY KEY (help_message_uuid)
);

CREATE INDEX idx_help_conversation_project ON help_conversation(project_id) WHERE project_id IS NOT NULL;
CREATE INDEX idx_help_conversation_user ON help_conversation(user_ern);
CREATE INDEX idx_help_message_conversation ON help_message(help_conversation_id);
