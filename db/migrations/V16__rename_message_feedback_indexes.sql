-- Forward-only fix: V13 was already applied in some environments with legacy index names.
-- Rename indexes in a new migration to preserve Flyway checksum history.
ALTER INDEX IF EXISTS ux_message_feedback_message_created_by
    RENAME TO idx_uq_conversation_message_feedback_message_id_created_by;

ALTER INDEX IF EXISTS ix_message_feedback_stage_type_created_at
    RENAME TO idx_conversation_message_feedback_stage_type_created_at;

ALTER INDEX IF EXISTS ix_message_feedback_conversation_id
    RENAME TO idx_conversation_message_feedback_conversation_id;
