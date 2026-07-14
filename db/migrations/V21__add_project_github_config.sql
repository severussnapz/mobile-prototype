ALTER TABLE project
    ADD COLUMN github_api_repo_url    VARCHAR(500) NULL,
    ADD COLUMN github_app_repo_url    VARCHAR(500) NULL,
    ADD COLUMN github_repo_owner      VARCHAR(200) NULL,
    ADD COLUMN github_repo_name       VARCHAR(200) NULL,
    ADD COLUMN github_installation_id VARCHAR(100) NULL,
    ADD COLUMN figma_file_url         VARCHAR(500) NULL,
    ADD COLUMN figma_pat_encrypted    TEXT         NULL;
