CREATE TABLE contract_manifest (
    id         UUID NOT NULL,
    project_id UUID NOT NULL,
    version    INTEGER NOT NULL,
    created_by TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT pk_contract_manifest PRIMARY KEY (id),
    CONSTRAINT fk_contract_manifest_project FOREIGN KEY (project_id) REFERENCES projects(id),
    CONSTRAINT uq_contract_manifest_project_version UNIQUE (project_id, version)
);

CREATE TABLE contract_manifest_pin (
    id             UUID NOT NULL,
    manifest_id    UUID NOT NULL,
    role           TEXT NOT NULL,
    file_path      TEXT NOT NULL,
    pinned_version INTEGER NOT NULL,
    CONSTRAINT pk_contract_manifest_pin PRIMARY KEY (id),
    CONSTRAINT fk_contract_manifest_pin_manifest FOREIGN KEY (manifest_id) REFERENCES contract_manifest(id) ON DELETE CASCADE,
    CONSTRAINT uq_contract_manifest_pin_manifest_role UNIQUE (manifest_id, role)
);
