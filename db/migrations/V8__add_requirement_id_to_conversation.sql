-- V8: Add requirement_id to conversation table
-- Supports per-requirement windowing (Change 3 of the token optimisation plan).
-- Each conversation can be scoped to a specific requirement (e.g. 'REQ-001').
-- Nullable: existing conversations and non-windowed stages have no requirement_id.

ALTER TABLE conversation
    ADD COLUMN IF NOT EXISTS requirement_id VARCHAR(50) NULL;

-- Index for efficient lookup of conversations by stage + requirement
CREATE INDEX IF NOT EXISTS idx_conversation_stage_requirement
    ON conversation (stage_id, requirement_id)
    WHERE requirement_id IS NOT NULL;
