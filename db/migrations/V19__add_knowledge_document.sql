CREATE TYPE knowledge_namespace AS ENUM ('genesis_tool', 'project_artefact');

CREATE TABLE knowledge_document (
    knowledge_document_uuid UUID NOT NULL DEFAULT uuid_generate_v4(),
    namespace knowledge_namespace NOT NULL,
    project_id UUID NULL,
    source_path VARCHAR(500) NOT NULL,
    chunk_index INT NOT NULL DEFAULT 0,
    content TEXT NOT NULL,
    embedding vector(1024) NOT NULL,
    metadata JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_knowledge_document PRIMARY KEY (knowledge_document_uuid),
    CONSTRAINT uq_knowledge_document_chunk UNIQUE (namespace, source_path, project_id, chunk_index)
);

CREATE INDEX idx_knowledge_document_namespace ON knowledge_document(namespace);
CREATE INDEX idx_knowledge_document_project ON knowledge_document(project_id)
    WHERE project_id IS NOT NULL;
CREATE INDEX idx_knowledge_document_source ON knowledge_document(namespace, source_path);
CREATE INDEX idx_knowledge_document_embedding ON knowledge_document
    USING hnsw (embedding vector_cosine_ops);