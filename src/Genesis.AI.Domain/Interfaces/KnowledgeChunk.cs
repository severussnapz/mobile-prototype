namespace Genesis.AI.Domain.Interfaces;

public record KnowledgeChunk(
    string Content,
    string SourcePath,
    double Score,
    Dictionary<string, string> Metadata);