using Microsoft.Extensions.Logging;
using PdfKnowledgeBase.Lib.Interfaces;
using System.Text.Json;

namespace PdfKnowledgeBase.Lib.Services;

/// <summary>
/// Service for generating and managing text embeddings with fallback options.
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly ILogger<EmbeddingService> _logger;
    private readonly IChatGptService _chatGptService;
    private readonly Random _random;

    public EmbeddingService(ILogger<EmbeddingService> logger, IChatGptService chatGptService)
    {
        _logger = logger;
        _chatGptService = chatGptService;
        _random = new Random(42); // Fixed seed for consistent mock embeddings
    }

    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    public async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        try
        {
            _logger.LogDebug("Generating embedding for text of length: {TextLength}", text.Length);

            // Try to use real embedding service first
            if (await _chatGptService.IsConfiguredAsync())
            {
                try
                {
                    var embeddingVector = await _chatGptService.GetEmbeddingVectorAsync(text);
                    if (embeddingVector != null)
                    {
                        _logger.LogDebug("Successfully generated real embedding vector");
                        return embeddingVector;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate real embedding, falling back to mock embedding");
                }
            }

            // Fallback to mock embedding
            return GenerateMockEmbedding(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            return null;
        }
    }

    /// <summary>
    /// Generates embeddings for multiple texts in batch.
    /// </summary>
    public async Task<List<float[]?>> GenerateEmbeddingsAsync(IEnumerable<string> texts)
    {
        var results = new List<float[]?>();
        
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text);
            results.Add(embedding);
        }
        
        return results;
    }

    /// <summary>
    /// Calculates the cosine similarity between two embedding vectors.
    /// </summary>
    public float CalculateSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            _logger.LogWarning("Vector dimensions don't match: {DimA} vs {DimB}", vectorA.Length, vectorB.Length);
            return 0f;
        }

        float dotProduct = 0f;
        float magnitudeA = 0f;
        float magnitudeB = 0f;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        if (magnitudeA == 0f || magnitudeB == 0f)
            return 0f;

        var similarity = dotProduct / (float)(Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        
        // Clamp to [-1, 1] range
        return Math.Max(-1f, Math.Min(1f, similarity));
    }

    /// <summary>
    /// Finds the most similar vectors to the query vector.
    /// </summary>
    public List<SimilarityResult> FindMostSimilar(float[] queryVector, IEnumerable<EmbeddingVector> candidateVectors, int topK = 5)
    {
        var results = new List<SimilarityResult>();
        var rank = 1;

        foreach (var candidate in candidateVectors)
        {
            if (candidate.Vector.Length == queryVector.Length)
            {
                var similarity = CalculateSimilarity(queryVector, candidate.Vector);
                results.Add(new SimilarityResult
                {
                    Vector = candidate,
                    SimilarityScore = similarity,
                    Rank = rank++
                });
            }
        }

        return results
            .OrderByDescending(r => r.SimilarityScore)
            .Take(topK)
            .ToList();
    }

    /// <summary>
    /// Checks if the embedding service is available and configured.
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            return await _chatGptService.IsConfiguredAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking embedding service availability");
            return false;
        }
    }

    /// <summary>
    /// Gets the dimension of the embedding vectors.
    /// </summary>
    public int GetEmbeddingDimension()
    {
        // OpenAI text-embedding-ada-002 has 1536 dimensions
        return 1536;
    }

    /// <summary>
    /// Generates a mock embedding vector for fallback scenarios.
    /// </summary>
    private float[] GenerateMockEmbedding(string text)
    {
        _logger.LogDebug("Generating mock embedding for text");
        
        // Generate a deterministic mock embedding based on text content
        var hash = text.GetHashCode();
        var random = new Random(hash);
        
        var embedding = new float[GetEmbeddingDimension()];
        
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Random between -1 and 1
        }
        
        // Normalize the vector
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)(embedding[i] / magnitude);
            }
        }
        
        return embedding;
    }
}
