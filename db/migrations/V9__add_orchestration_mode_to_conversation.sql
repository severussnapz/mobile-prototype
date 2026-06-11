-- V9: Add orchestration_mode to conversation table
-- Supports explicit non-windowed cross-check mode for P6/P7/P8 (Change 6 of the
-- token optimisation plan). The mode must be switched explicitly — never inferred.

CREATE TYPE orchestration_mode AS ENUM ('forward_sweep', 'cross_check');

ALTER TABLE conversation
    ADD COLUMN IF NOT EXISTS orchestration_mode orchestration_mode NOT NULL
        DEFAULT 'forward_sweep'::orchestration_mode;
