CREATE TABLE conversation_message_feedback (
    conversation_message_feedback_id uuid PRIMARY KEY,
    conversation_id uuid NOT NULL,
    message_id uuid NOT NULL,
    stage_type stage_type NOT NULL,
    is_helpful boolean NOT NULL,
    reason text NULL,
    created_by varchar(255) NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    CONSTRAINT fk_message_feedback_conversation
        FOREIGN KEY (conversation_id)
        REFERENCES conversation(conversation_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_message_feedback_message
        FOREIGN KEY (message_id)
        REFERENCES message(message_id)
        ON DELETE CASCADE
);

CREATE UNIQUE INDEX idx_uq_conversation_message_feedback_message_id_created_by
    ON conversation_message_feedback (message_id, created_by);

CREATE INDEX idx_conversation_message_feedback_stage_type_created_at
    ON conversation_message_feedback (stage_type, created_at DESC);

CREATE INDEX idx_conversation_message_feedback_conversation_id
    ON conversation_message_feedback (conversation_id);
