using Genesis.AI.Core.Domain;
using Genesis.AI.Domain.Enums;
using Pgvector;

namespace Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate;

public class KnowledgeDocument : Entity
{
    public KnowledgeNamespace Namespace { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string SourcePath { get; private set; } = string.Empty;
    public int ChunkIndex { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public Vector Embedding { get; private set; } = null!;
    public Dictionary<string, string> Metadata { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private KnowledgeDocument() { } // EF Core

    public static KnowledgeDocument Create(
        KnowledgeNamespace @namespace,
        Guid? projectId,
        string sourcePath,
        int chunkIndex,
        string content,
        Vector embedding,
        Dictionary<string, string> metadata,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Namespace = @namespace,
            ProjectId = projectId,
            SourcePath = sourcePath,
            ChunkIndex = chunkIndex,
            Content = content,
            Embedding = embedding,
            Metadata = metadata,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}