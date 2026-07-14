namespace Genesis.AI.Domain.Interfaces;

public interface IEmbeddingService
{
    /// <summary>
    /// Generates a 1024-dimension embedding vector for the given text
    /// using Amazon Titan Text Embeddings v2 via InvokeModelAsync.
    /// </summary>
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken);

    const int Dimensions = 1024;
}