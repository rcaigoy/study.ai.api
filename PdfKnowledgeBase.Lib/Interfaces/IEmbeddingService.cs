namespace PdfKnowledgeBase.Lib.Interfaces;

/// <summary>
/// Interface for generating and managing text embeddings.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to generate an embedding for.</param>
    /// <returns>The embedding vector or null if generation failed.</returns>
    Task<float[]?> GenerateEmbeddingAsync(string text);

    /// <summary>
    /// Generates embeddings for multiple texts in batch.
    /// </summary>
    /// <param name="texts">The texts to generate embeddings for.</param>
    /// <returns>List of embedding vectors (null for failed generations).</returns>
    Task<List<float[]?>> GenerateEmbeddingsAsync(IEnumerable<string> texts);

    /// <summary>
    /// Calculates the cosine similarity between two embedding vectors.
    /// </summary>
    /// <param name="vectorA">The first embedding vector.</param>
    /// <param name="vectorB">The second embedding vector.</param>
    /// <returns>The cosine similarity score (-1 to 1).</returns>
    float CalculateSimilarity(float[] vectorA, float[] vectorB);

    /// <summary>
    /// Finds the most similar vectors to the query vector.
    /// </summary>
    /// <param name="queryVector">The query embedding vector.</param>
    /// <param name="candidateVectors">The candidate vectors to search through.</param>
    /// <param name="topK">The number of most similar vectors to return.</param>
    /// <returns>List of similarity results ordered by similarity score.</returns>
    List<SimilarityResult> FindMostSimilar(float[] queryVector, IEnumerable<EmbeddingVector> candidateVectors, int topK = 5);

    /// <summary>
    /// Checks if the embedding service is available and configured.
    /// </summary>
    /// <returns>True if the service is available.</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Gets the dimension of the embedding vectors.
    /// </summary>
    /// <returns>The dimension of embedding vectors.</returns>
    int GetEmbeddingDimension();
}

/// <summary>
/// Represents an embedding vector with metadata.
/// </summary>
public class EmbeddingVector
{
    /// <summary>
    /// The embedding vector data.
    /// </summary>
    public float[] Vector { get; set; } = Array.Empty<float>();

    /// <summary>
    /// The text that was embedded.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the vector.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a similarity search result.
/// </summary>
public class SimilarityResult
{
    /// <summary>
    /// The embedding vector that was matched.
    /// </summary>
    public EmbeddingVector Vector { get; set; } = new();

    /// <summary>
    /// The similarity score.
    /// </summary>
    public float SimilarityScore { get; set; }

    /// <summary>
    /// The rank of this result (1-based).
    /// </summary>
    public int Rank { get; set; }
}
