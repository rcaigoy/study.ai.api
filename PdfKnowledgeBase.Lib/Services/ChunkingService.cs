using Microsoft.Extensions.Logging;
using PdfKnowledgeBase.Lib.Interfaces;
using PdfKnowledgeBase.Lib.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfKnowledgeBase.Lib.Services;

/// <summary>
/// Service for chunking text into manageable pieces with various strategies.
/// </summary>
public class ChunkingService : IChunkingService
{
    private readonly ILogger<ChunkingService> _logger;

    public ChunkingService(ILogger<ChunkingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Splits text into chunks using the default strategy.
    /// </summary>
    public async Task<List<DocumentChunk>> ChunkTextAsync(string text, int pageNumber = 1)
    {
        var settings = new ChunkingSettings();
        return await ChunkTextAsync(text, pageNumber, settings);
    }

    /// <summary>
    /// Splits text into chunks using the specified strategy.
    /// </summary>
    public async Task<List<DocumentChunk>> ChunkTextAsync(string text, int pageNumber, ChunkingStrategy strategy)
    {
        var settings = new ChunkingSettings { Strategy = strategy };
        return await ChunkTextAsync(text, pageNumber, settings);
    }

    /// <summary>
    /// Splits text into chunks with custom settings.
    /// </summary>
    public async Task<List<DocumentChunk>> ChunkTextAsync(string text, int pageNumber, ChunkingSettings settings)
    {
        _logger.LogInformation("Starting text chunking with strategy: {Strategy}, MaxChunkSize: {MaxChunkSize}, PageNumber: {PageNumber}",
            settings.Strategy, settings.MaxChunkSize, pageNumber);

        var chunks = new List<DocumentChunk>();

        try
        {
            switch (settings.Strategy)
            {
                case ChunkingStrategy.WordBased:
                    chunks = await ChunkByWordsAsync(text, pageNumber, settings);
                    break;
                case ChunkingStrategy.SentenceBased:
                    chunks = await ChunkBySentencesAsync(text, pageNumber, settings);
                    break;
                case ChunkingStrategy.ParagraphBased:
                    chunks = await ChunkByParagraphsAsync(text, pageNumber, settings);
                    break;
                case ChunkingStrategy.Semantic:
                    chunks = await ChunkSemanticallyAsync(text, pageNumber, settings);
                    break;
                case ChunkingStrategy.ChapterBased:
                    chunks = await ChunkByChaptersAsync(text, pageNumber, settings);
                    break;
                default:
                    chunks = await ChunkByWordsAsync(text, pageNumber, settings);
                    break;
            }

            // Post-process chunks
            chunks = await PostProcessChunksAsync(chunks, settings);

            _logger.LogInformation("Text chunking completed. Generated {ChunkCount} chunks", chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during text chunking");
            throw;
        }

        return chunks;
    }

    private async Task<List<DocumentChunk>> ChunkByWordsAsync(string text, int pageNumber, ChunkingSettings settings)
    {
        var chunks = new List<DocumentChunk>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length; i += settings.MaxChunkSize - settings.OverlapSize)
        {
            var chunkWords = words.Skip(i).Take(settings.MaxChunkSize);
            var chunkText = string.Join(" ", chunkWords);
            
            if (!string.IsNullOrWhiteSpace(chunkText) && chunkText.Length >= settings.MinChunkSize)
            {
                var chunk = CreateDocumentChunk(chunkText, pageNumber, chunks.Count, words.Length / settings.MaxChunkSize + 1);
                chunks.Add(chunk);
            }
        }

        await Task.CompletedTask;
        return chunks;
    }

    private async Task<List<DocumentChunk>> ChunkBySentencesAsync(string text, int pageNumber, ChunkingSettings settings)
    {
        var chunks = new List<DocumentChunk>();
        var sentences = SplitIntoSentences(text);
        var currentChunk = new StringBuilder();
        
        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > settings.MaxChunkSize && currentChunk.Length > 0)
            {
                var chunkText = currentChunk.ToString().Trim();
                if (chunkText.Length >= settings.MinChunkSize)
                {
                    var chunk = CreateDocumentChunk(chunkText, pageNumber, chunks.Count, sentences.Count);
                    chunks.Add(chunk);
                }
                currentChunk.Clear();
                
                // Add overlap
                if (settings.OverlapSize > 0)
                {
                    var overlapText = GetOverlapText(chunkText, settings.OverlapSize);
                    currentChunk.Append(overlapText);
                }
            }
            
            currentChunk.Append(sentence).Append(" ");
        }

        // Add remaining text as final chunk
        if (currentChunk.Length > 0)
        {
            var chunkText = currentChunk.ToString().Trim();
            if (chunkText.Length >= settings.MinChunkSize)
            {
                var chunk = CreateDocumentChunk(chunkText, pageNumber, chunks.Count, sentences.Count);
                chunks.Add(chunk);
            }
        }

        await Task.CompletedTask;
        return chunks;
    }

    private async Task<List<DocumentChunk>> ChunkByParagraphsAsync(string text, int pageNumber, ChunkingSettings settings)
    {
        var chunks = new List<DocumentChunk>();
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = new StringBuilder();
        
        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > settings.MaxChunkSize && currentChunk.Length > 0)
            {
                var chunkText = currentChunk.ToString().Trim();
                if (chunkText.Length >= settings.MinChunkSize)
                {
                    var chunk = CreateDocumentChunk(chunkText, pageNumber, chunks.Count, paragraphs.Length);
                    chunks.Add(chunk);
                }
                currentChunk.Clear();
                
                // Add overlap
                if (settings.OverlapSize > 0)
                {
                    var overlapText = GetOverlapText(chunkText, settings.OverlapSize);
                    currentChunk.Append(overlapText);
                }
            }
            
            currentChunk.Append(paragraph).Append("\n\n");
        }

        // Add remaining text as final chunk
        if (currentChunk.Length > 0)
        {
            var chunkText = currentChunk.ToString().Trim();
            if (chunkText.Length >= settings.MinChunkSize)
            {
                var chunk = CreateDocumentChunk(chunkText, pageNumber, chunks.Count, paragraphs.Length);
                chunks.Add(chunk);
            }
        }

        await Task.CompletedTask;
        return chunks;
    }

    private async Task<List<DocumentChunk>> ChunkSemanticallyAsync(string text, int pageNumber, ChunkingSettings settings)
    {
        // For now, fall back to sentence-based chunking
        // In a full implementation, this would use semantic analysis
        _logger.LogInformation("Semantic chunking not fully implemented, falling back to sentence-based chunking");
        return await ChunkBySentencesAsync(text, pageNumber, settings);
    }

    private async Task<List<DocumentChunk>> ChunkByChaptersAsync(string text, int pageNumber, ChunkingSettings settings)
    {
        var chunks = new List<DocumentChunk>();
        var chapters = SplitIntoChapters(text, settings.ChapterPatterns);
        
        foreach (var (chapterTitle, chapterContent) in chapters)
        {
            var chapterChunks = await ChunkBySentencesAsync(chapterContent, pageNumber, settings);
            
            foreach (var chunk in chapterChunks)
            {
                chunk.Chapter = chapterTitle;
                chunks.Add(chunk);
            }
        }

        return chunks;
    }

    private List<string> SplitIntoSentences(string text)
    {
        // Simple sentence splitting - in production, use a proper NLP library
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        
        return sentences;
    }

    private List<(string title, string content)> SplitIntoChapters(string text, List<string> patterns)
    {
        var chapters = new List<(string title, string content)>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        string? currentChapter = null;
        var currentContent = new StringBuilder();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Check if line matches any chapter pattern
            var isChapterHeader = patterns.Any(pattern => 
                Regex.IsMatch(trimmedLine, pattern, RegexOptions.IgnoreCase));
            
            if (isChapterHeader)
            {
                // Save previous chapter
                if (currentChapter != null && currentContent.Length > 0)
                {
                    chapters.Add((currentChapter, currentContent.ToString()));
                }
                
                // Start new chapter
                currentChapter = trimmedLine;
                currentContent.Clear();
            }
            else
            {
                currentContent.AppendLine(line);
            }
        }
        
        // Add final chapter
        if (currentChapter != null && currentContent.Length > 0)
        {
            chapters.Add((currentChapter, currentContent.ToString()));
        }
        
        // If no chapters found, treat entire text as one chapter
        if (chapters.Count == 0)
        {
            chapters.Add(("Document", text));
        }
        
        return chapters;
    }

    private DocumentChunk CreateDocumentChunk(string text, int pageNumber, int chunkIndex, int totalChunks)
    {
        // Try to detect chapter from the text
        var chapter = DetectChapterFromText(text, pageNumber);
        
        return new DocumentChunk
        {
            Id = $"page_{pageNumber}_chunk_{chunkIndex}",
            Text = text,
            PageNumber = pageNumber,
            ChunkIndex = chunkIndex,
            TotalChunks = totalChunks,
            Chapter = chapter,
            Metadata = new Dictionary<string, object>
            {
                ["created_at"] = DateTime.UtcNow,
                ["character_count"] = text.Length,
                ["word_count"] = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
            }
        };
    }

    private string? DetectChapterFromText(string text, int pageNumber)
    {
        try
        {
            // Look for common chapter patterns at the start of the text
            var firstLines = text.Split('\n').Take(5).Select(l => l.Trim());
            
            foreach (var line in firstLines)
            {
                // Match patterns like:
                // "Chapter 1", "CHAPTER ONE", "Ch. 1:", "1. Introduction", "Part I", etc.
                var chapterPatterns = new[]
                {
                    @"^(Chapter|CHAPTER|Ch\.|Ch)\s+(\d+|[IVX]+|One|Two|Three|Four|Five|Six|Seven|Eight|Nine|Ten)[\s:.\-]*(.*)",
                    @"^(Part|PART|Section|SECTION)\s+(\d+|[IVX]+|One|Two|Three|Four|Five|Six|Seven|Eight|Nine|Ten)[\s:.\-]*(.*)",
                    @"^(\d+)\.\s+([A-Z][a-zA-Z\s]+)$",
                    @"^([IVX]+)\.\s+([A-Z][a-zA-Z\s]+)$"
                };
                
                foreach (var pattern in chapterPatterns)
                {
                    var match = Regex.Match(line, pattern);
                    if (match.Success && line.Length < 100) // Chapter titles are usually short
                    {
                        return line;
                    }
                }
            }
            
            // Fallback: Use page number as chapter indicator
            return $"Pages {pageNumber}-{pageNumber + 5}";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error detecting chapter from text");
            return $"Page {pageNumber}";
        }
    }

    private string GetOverlapText(string text, int overlapSize)
    {
        if (string.IsNullOrEmpty(text) || overlapSize <= 0)
            return string.Empty;
        
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var overlapWords = words.TakeLast(Math.Min(overlapSize / 6, words.Length)); // Rough estimate: 6 chars per word
        
        return string.Join(" ", overlapWords) + " ";
    }

    private async Task<List<DocumentChunk>> PostProcessChunksAsync(List<DocumentChunk> chunks, ChunkingSettings settings)
    {
        var processedChunks = new List<DocumentChunk>();
        
        foreach (var chunk in chunks)
        {
            // Merge small chunks if enabled
            if (settings.MergeSmallChunks && chunk.Text.Length < settings.MinChunkSize && processedChunks.Count > 0)
            {
                var lastChunk = processedChunks[^1];
                if (lastChunk.Text.Length + chunk.Text.Length <= settings.MaxChunkSize)
                {
                    lastChunk.Text += " " + chunk.Text;
                    continue;
                }
            }
            
            processedChunks.Add(chunk);
        }
        
        await Task.CompletedTask;
        return processedChunks;
    }
}
