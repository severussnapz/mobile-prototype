ALTER TABLE project
    ADD COLUMN release_type               VARCHAR(50) NULL,
    ADD COLUMN assurance_required         BOOLEAN     NULL,
    ADD COLUMN pilot_deployment_process   TEXT        NULL,
    ADD COLUMN cso_role_assigned          BOOLEAN     NULL,
    ADD COLUMN ig_owner_role_assigned     BOOLEAN     NULL,
    ADD COLUMN security_reviewer_assigned BOOLEAN     NULL,
    ADD COLUMN medical_device_flag        BOOLEAN     NULL;
