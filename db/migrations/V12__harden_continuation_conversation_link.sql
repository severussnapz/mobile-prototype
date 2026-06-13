-- V12: Harden continuation conversation linkage for restart reliability.
-- - Null out orphaned links before adding the FK constraint.
-- - Add index for continuation chain lookups.

UPDATE conversation AS child
SET continued_from_conversation_id = NULL
WHERE child.continued_from_conversation_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM conversation AS parent
      WHERE parent.conversation_id = child.continued_from_conversation_id
  );

CREATE INDEX IF NOT EXISTS idx_conversation_continued_from_conversation_id
    ON conversation (continued_from_conversation_id);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_conversation_continued_from_conversation_id'
    ) THEN
        ALTER TABLE conversation
            ADD CONSTRAINT fk_conversation_continued_from_conversation_id
            FOREIGN KEY (continued_from_conversation_id)
            REFERENCES conversation (conversation_id)
            ON DELETE SET NULL;
    END IF;
END $$;
