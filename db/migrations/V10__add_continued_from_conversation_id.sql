ALTER TABLE conversation
    ADD COLUMN IF NOT EXISTS continued_from_conversation_id UUID NULL;
