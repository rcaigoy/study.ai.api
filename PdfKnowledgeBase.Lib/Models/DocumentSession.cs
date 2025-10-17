using System.Text.Json.Serialization;

namespace PdfKnowledgeBase.Lib.Models;

/// <summary>
/// Represents a document processing session with metadata and statistics.
/// </summary>
public class DocumentSession
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// When the session expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// The original filename of the uploaded document.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The size of the original file in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The number of chunks created from the document.
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// The total number of pages in the document.
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// The total number of characters in the extracted text.
    /// </summary>
    public int CharacterCount { get; set; }

    /// <summary>
    /// Processing statistics.
    /// </summary>
    public ProcessingStats ProcessingStats { get; set; } = new();

    /// <summary>
    /// Query statistics for this session.
    /// </summary>
    public QueryStats QueryStats { get; set; } = new();

    /// <summary>
    /// Whether embeddings were successfully generated for all chunks.
    /// </summary>
    public bool EmbeddingsGenerated { get; set; }

    /// <summary>
    /// Additional metadata for the session.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Processing statistics for a document session.
/// </summary>
public class ProcessingStats
{
    /// <summary>
    /// Time taken to extract text from the PDF.
    /// </summary>
    public TimeSpan TextExtractionTime { get; set; }

    /// <summary>
    /// Time taken to chunk the text.
    /// </summary>
    public TimeSpan ChunkingTime { get; set; }

    /// <summary>
    /// Time taken to generate embeddings.
    /// </summary>
    public TimeSpan EmbeddingGenerationTime { get; set; }

    /// <summary>
    /// Total processing time.
    /// </summary>
    public TimeSpan TotalProcessingTime { get; set; }

    /// <summary>
    /// Whether processing completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Any error messages during processing.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Query statistics for a document session.
/// </summary>
public class QueryStats
{
    /// <summary>
    /// Total number of queries made against this session.
    /// </summary>
    public int TotalQueries { get; set; }

    /// <summary>
    /// Average response time for queries.
    /// </summary>
    public TimeSpan AverageResponseTime { get; set; }

    /// <summary>
    /// Last query timestamp.
    /// </summary>
    public DateTime? LastQueryAt { get; set; }

    /// <summary>
    /// Total tokens used across all queries.
    /// </summary>
    public int TotalTokensUsed { get; set; }
}
