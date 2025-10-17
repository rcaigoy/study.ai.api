using System.Text.Json.Serialization;

namespace PdfKnowledgeBase.Lib.Models;

/// <summary>
/// Represents a chunk of text extracted from a document with metadata and embedding information.
/// </summary>
public class DocumentChunk
{
    /// <summary>
    /// Unique identifier for the chunk.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The text content of the chunk.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The page number where this chunk was extracted from.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// The vector embedding for semantic search.
    /// </summary>
    [JsonIgnore]
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Similarity score when used in search results.
    /// </summary>
    public float SimilarityScore { get; set; }

    /// <summary>
    /// The chapter or section this chunk belongs to (if applicable).
    /// </summary>
    public string? Chapter { get; set; }

    /// <summary>
    /// Additional metadata for the chunk.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// The position of this chunk within the document.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// The total number of chunks in the document.
    /// </summary>
    public int TotalChunks { get; set; }
}
