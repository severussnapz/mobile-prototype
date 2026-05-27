namespace Genesis.AI.Domain.Interfaces;

public record AiDocumentContent(
    string Base64Data,
    string MediaType,
    string FileName);
