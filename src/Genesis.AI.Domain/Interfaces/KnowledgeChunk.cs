namespace Genesis.AI.Domain.Interfaces;

public sealed record KnowledgeChunk(
    string Content,
    string SourcePath,
    double Score,
    Dictionary<string, string> Metadata);
