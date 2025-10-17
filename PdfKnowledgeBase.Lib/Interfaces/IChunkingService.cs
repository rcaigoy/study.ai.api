using PdfKnowledgeBase.Lib.Models;

namespace PdfKnowledgeBase.Lib.Interfaces;

/// <summary>
/// Interface for text chunking strategies.
/// </summary>
public interface IChunkingService
{
    /// <summary>
    /// Splits text into chunks using the default strategy.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="pageNumber">The page number this text comes from.</param>
    /// <returns>List of document chunks.</returns>
    Task<List<DocumentChunk>> ChunkTextAsync(string text, int pageNumber = 1);

    /// <summary>
    /// Splits text into chunks using the specified strategy.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="pageNumber">The page number this text comes from.</param>
    /// <param name="strategy">The chunking strategy to use.</param>
    /// <returns>List of document chunks.</returns>
    Task<List<DocumentChunk>> ChunkTextAsync(string text, int pageNumber, ChunkingStrategy strategy);

    /// <summary>
    /// Splits text into chunks with custom settings.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="pageNumber">The page number this text comes from.</param>
    /// <param name="settings">Custom chunking settings.</param>
    /// <returns>List of document chunks.</returns>
    Task<List<DocumentChunk>> ChunkTextAsync(string text, int pageNumber, ChunkingSettings settings);
}

/// <summary>
/// Available chunking strategies.
/// </summary>
public enum ChunkingStrategy
{
    /// <summary>
    /// Simple word-based chunking with overlap.
    /// </summary>
    WordBased,

    /// <summary>
    /// Sentence-based chunking that preserves sentence boundaries.
    /// </summary>
    SentenceBased,

    /// <summary>
    /// Paragraph-based chunking that preserves paragraph boundaries.
    /// </summary>
    ParagraphBased,

    /// <summary>
    /// Semantic chunking that groups related content together.
    /// </summary>
    Semantic,

    /// <summary>
    /// Chapter-based chunking that respects document structure.
    /// </summary>
    ChapterBased
}

/// <summary>
/// Settings for text chunking.
/// </summary>
public class ChunkingSettings
{
    /// <summary>
    /// The maximum size of each chunk in characters.
    /// </summary>
    public int MaxChunkSize { get; set; } = 1000;

    /// <summary>
    /// The overlap between chunks in characters.
    /// </summary>
    public int OverlapSize { get; set; } = 200;

    /// <summary>
    /// The chunking strategy to use.
    /// </summary>
    public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.WordBased;

    /// <summary>
    /// Whether to preserve sentence boundaries.
    /// </summary>
    public bool PreserveSentenceBoundaries { get; set; } = true;

    /// <summary>
    /// Whether to preserve paragraph boundaries.
    /// </summary>
    public bool PreserveParagraphBoundaries { get; set; } = false;

    /// <summary>
    /// Whether to detect and preserve chapter boundaries.
    /// </summary>
    public bool DetectChapterBoundaries { get; set; } = true;

    /// <summary>
    /// Patterns to identify chapter boundaries.
    /// </summary>
    public List<string> ChapterPatterns { get; set; } = new()
    {
        @"^Chapter\s+\d+",
        @"^CHAPTER\s+\d+",
        @"^Part\s+\d+",
        @"^PART\s+\d+",
        @"^Section\s+\d+",
        @"^SECTION\s+\d+"
    };

    /// <summary>
    /// Minimum chunk size in characters.
    /// </summary>
    public int MinChunkSize { get; set; } = 100;

    /// <summary>
    /// Whether to merge small chunks with adjacent ones.
    /// </summary>
    public bool MergeSmallChunks { get; set; } = true;
}
