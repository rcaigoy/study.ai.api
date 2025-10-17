namespace PdfKnowledgeBase.Lib.DTOs;

/// <summary>
/// Response DTO for document session information.
/// </summary>
public class DocumentSessionResponse
{
    /// <summary>
    /// The session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// When the session expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// The original filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// The number of chunks created.
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// The number of pages in the document.
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// Whether embeddings were generated successfully.
    /// </summary>
    public bool EmbeddingsGenerated { get; set; }

    /// <summary>
    /// Processing statistics.
    /// </summary>
    public ProcessingStatsDto ProcessingStats { get; set; } = new();

    /// <summary>
    /// Query statistics.
    /// </summary>
    public QueryStatsDto QueryStats { get; set; } = new();
}

/// <summary>
/// Processing statistics DTO.
/// </summary>
public class ProcessingStatsDto
{
    /// <summary>
    /// Total processing time.
    /// </summary>
    public TimeSpan TotalProcessingTime { get; set; }

    /// <summary>
    /// Whether processing completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Any error messages.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Query statistics DTO.
/// </summary>
public class QueryStatsDto
{
    /// <summary>
    /// Total number of queries.
    /// </summary>
    public int TotalQueries { get; set; }

    /// <summary>
    /// Average response time.
    /// </summary>
    public TimeSpan AverageResponseTime { get; set; }

    /// <summary>
    /// Last query timestamp.
    /// </summary>
    public DateTime? LastQueryAt { get; set; }

    /// <summary>
    /// Total tokens used.
    /// </summary>
    public int TotalTokensUsed { get; set; }
}

/// <summary>
/// Request DTO for document queries.
/// </summary>
public class DocumentQueryRequest
{
    /// <summary>
    /// The question to ask about the document.
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The session ID to query against.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of relevant chunks to retrieve.
    /// </summary>
    public int MaxChunks { get; set; } = 3;

    /// <summary>
    /// Temperature for the AI response.
    /// </summary>
    public double Temperature { get; set; } = 0.2;
}

/// <summary>
/// Response DTO for document queries.
/// </summary>
public class DocumentQueryResponse
{
    /// <summary>
    /// The AI-generated answer.
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// The session ID that was queried.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// The relevant chunks used to generate the answer.
    /// </summary>
    public List<DocumentChunkDto> RelevantChunks { get; set; } = new();

    /// <summary>
    /// Query metadata.
    /// </summary>
    public QueryMetadata QueryMetadata { get; set; } = new();

    /// <summary>
    /// Whether the query was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the query failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Simplified document chunk DTO for responses.
/// </summary>
public class DocumentChunkDto
{
    /// <summary>
    /// The chunk identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The text content (may be truncated).
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// The chapter or section.
    /// </summary>
    public string? Chapter { get; set; }

    /// <summary>
    /// The similarity score.
    /// </summary>
    public float SimilarityScore { get; set; }
}

/// <summary>
/// Query metadata for tracking and analytics.
/// </summary>
public class QueryMetadata
{
    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public double ProcessingTimeMs { get; set; }

    /// <summary>
    /// Number of tokens used.
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// The model used for the query.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// When the query was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
