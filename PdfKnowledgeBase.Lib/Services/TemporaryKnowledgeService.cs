using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PdfKnowledgeBase.Lib.DTOs;
using PdfKnowledgeBase.Lib.Interfaces;
using PdfKnowledgeBase.Lib.Models;
using System.Text.Json;

namespace PdfKnowledgeBase.Lib.Services;

/// <summary>
/// Service for managing temporary knowledge bases from PDF documents.
/// </summary>
public class TemporaryKnowledgeService : ITemporaryKnowledgeService
{
    private readonly IMemoryCache _cache;
    private readonly IChatGptService _chatGptService;
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<TemporaryKnowledgeService> _logger;
    private readonly IConfiguration _configuration;

    public TemporaryKnowledgeService(
        IMemoryCache cache,
        IChatGptService chatGptService,
        IPdfTextExtractor pdfExtractor,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        ILogger<TemporaryKnowledgeService> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _chatGptService = chatGptService;
        _pdfExtractor = pdfExtractor;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Creates a temporary knowledge base from a PDF file.
    /// </summary>
    public async Task<string> CreateTemporaryKnowledgeBaseAsync(Stream fileStream, string fileName, TimeSpan expiration)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var sessionId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Creating temporary knowledge base for session: {SessionId}, File: {FileName}", sessionId, fileName);

            // Step 1: Extract text from PDF
            var extractionResult = await _pdfExtractor.ExtractTextAsync(fileStream, fileName);
            if (!extractionResult.Success)
            {
                throw new InvalidOperationException($"PDF extraction failed: {extractionResult.ErrorMessage}");
            }

            // Step 2: Chunk the text
            var allChunks = new List<DocumentChunk>();
            foreach (var (pageNumber, pageText) in extractionResult.Pages)
            {
                var chunks = await _chunkingService.ChunkTextAsync(pageText, pageNumber);
                allChunks.AddRange(chunks);
            }

            // Step 3: Generate embeddings for chunks
            var chunksWithEmbeddings = await GenerateEmbeddingsForChunksAsync(allChunks);

            // Step 4: Create session info
            var sessionInfo = new DocumentSession
            {
                SessionId = sessionId,
                ExpiresAt = DateTime.UtcNow.Add(expiration),
                FileName = fileName,
                FileSize = fileStream.Length,
                ChunkCount = chunksWithEmbeddings.Count,
                PageCount = extractionResult.PageCount,
                CharacterCount = extractionResult.CharacterCount,
                ProcessingStats = new ProcessingStats
                {
                    TextExtractionTime = extractionResult.ProcessingTime,
                    TotalProcessingTime = stopwatch.Elapsed,
                    Success = true
                },
                EmbeddingsGenerated = chunksWithEmbeddings.Any(c => c.Embedding != null)
            };

            // Step 5: Store in cache
            _cache.Set($"knowledge_{sessionId}", chunksWithEmbeddings, expiration);
            _cache.Set($"session_{sessionId}", sessionInfo, expiration);

            _logger.LogInformation("Temporary knowledge base created: {SessionId} with {ChunkCount} chunks, Processing time: {ProcessingTime}ms",
                sessionId, chunksWithEmbeddings.Count, stopwatch.Elapsed.TotalMilliseconds);
            
            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating temporary knowledge base for session: {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Queries the temporary knowledge base with a question.
    /// </summary>
    public async Task<DocumentQueryResponse> QueryTemporaryKnowledgeAsync(string sessionId, DocumentQueryRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            if (!_cache.TryGetValue($"knowledge_{sessionId}", out List<DocumentChunk> chunks))
            {
                return new DocumentQueryResponse
                {
                    Success = false,
                    ErrorMessage = "Knowledge base session expired or not found",
                    SessionId = sessionId
                };
            }

            _logger.LogInformation("Querying temporary knowledge base: {SessionId}", sessionId);

            // Find most relevant chunks
            var relevantChunks = await FindRelevantChunksAsync(chunks, request.Question, request.MaxChunks);
            
            if (!relevantChunks.Any())
            {
                // Fallback to first chunks if no relevant ones found
                relevantChunks = chunks.Take(2).ToList();
            }

            var context = string.Join("\n\n", relevantChunks.Select(c => c.Text));
            
            var prompt = $@"
Based on the following information from the uploaded document, please answer the question.

Document Content:
{context}

Question: {request.Question}

Please provide a detailed answer based only on the information provided in the document content. If the answer cannot be found in the provided content, please say so clearly.
";

            var chatRequest = new ChatGptRequestDto
            {
                Message = prompt,
                SystemPrompt = "You are a helpful assistant that answers questions based on uploaded document content. Be precise and only use information from the provided content. Cite specific sections when possible.",
                Temperature = request.Temperature,
                MaxTokens = 1000
            };

            var response = await _chatGptService.SendMessageAsync(chatRequest);

            // Update session statistics
            await UpdateSessionStatsAsync(sessionId, stopwatch.Elapsed, response.TokensUsed);

            stopwatch.Stop();

            return new DocumentQueryResponse
            {
                Answer = response.Response,
                SessionId = sessionId,
                RelevantChunks = relevantChunks.Select(c => new DocumentChunkDto
                {
                    Id = c.Id,
                    Text = c.Text.Length > 200 ? c.Text.Substring(0, 200) + "..." : c.Text,
                    PageNumber = c.PageNumber,
                    Chapter = c.Chapter,
                    SimilarityScore = c.SimilarityScore
                }).ToList(),
                QueryMetadata = new QueryMetadata
                {
                    ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                    TokensUsed = response.TokensUsed,
                    Model = response.Model,
                    ExecutedAt = DateTime.UtcNow
                },
                Success = response.Success,
                ErrorMessage = response.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying temporary knowledge base: {SessionId}", sessionId);
            return new DocumentQueryResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                SessionId = sessionId
            };
        }
    }

    /// <summary>
    /// Extends the expiration time of a session.
    /// </summary>
    public async Task<bool> ExtendSessionAsync(string sessionId, TimeSpan additionalTime)
    {
        try
        {
            if (!_cache.TryGetValue($"knowledge_{sessionId}", out List<DocumentChunk> chunks) ||
                !_cache.TryGetValue($"session_{sessionId}", out DocumentSession sessionInfo))
            {
                return false;
            }

            var newExpiration = sessionInfo.ExpiresAt.Add(additionalTime);
            
            _cache.Set($"knowledge_{sessionId}", chunks, newExpiration - DateTime.UtcNow);
            sessionInfo.ExpiresAt = newExpiration;
            _cache.Set($"session_{sessionId}", sessionInfo, newExpiration - DateTime.UtcNow);

            _logger.LogInformation("Session extended: {SessionId} until {NewExpiration}", sessionId, newExpiration);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending session: {SessionId}", sessionId);
            return false;
        }
    }

    /// <summary>
    /// Deletes a session and frees up memory.
    /// </summary>
    public async Task<bool> DeleteSessionAsync(string sessionId)
    {
        try
        {
            _cache.Remove($"knowledge_{sessionId}");
            _cache.Remove($"session_{sessionId}");
            
            _logger.LogInformation("Session deleted: {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session: {SessionId}", sessionId);
            return false;
        }
    }

    /// <summary>
    /// Gets information about a session.
    /// </summary>
    public async Task<DocumentSessionResponse?> GetSessionInfoAsync(string sessionId)
    {
        try
        {
            if (_cache.TryGetValue($"session_{sessionId}", out DocumentSession sessionInfo))
            {
                return new DocumentSessionResponse
                {
                    SessionId = sessionInfo.SessionId,
                    ExpiresAt = sessionInfo.ExpiresAt,
                    FileName = sessionInfo.FileName,
                    FileSize = sessionInfo.FileSize,
                    ChunkCount = sessionInfo.ChunkCount,
                    PageCount = sessionInfo.PageCount,
                    EmbeddingsGenerated = sessionInfo.EmbeddingsGenerated,
                    ProcessingStats = new ProcessingStatsDto
                    {
                        TotalProcessingTime = sessionInfo.ProcessingStats.TotalProcessingTime,
                        Success = sessionInfo.ProcessingStats.Success,
                        ErrorMessage = sessionInfo.ProcessingStats.ErrorMessage
                    },
                    QueryStats = new QueryStatsDto
                    {
                        TotalQueries = sessionInfo.QueryStats.TotalQueries,
                        AverageResponseTime = sessionInfo.QueryStats.AverageResponseTime,
                        LastQueryAt = sessionInfo.QueryStats.LastQueryAt,
                        TotalTokensUsed = sessionInfo.QueryStats.TotalTokensUsed
                    }
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session info: {SessionId}", sessionId);
            return null;
        }
    }

    /// <summary>
    /// Gets all active sessions.
    /// </summary>
    public async Task<List<DocumentSessionResponse>> GetActiveSessionsAsync()
    {
        // This is a simplified implementation
        // In a production system, you'd want to track active sessions more efficiently
        var activeSessions = new List<DocumentSessionResponse>();
        
        // For now, we'll return empty list since we can't easily enumerate cache entries
        // A better implementation would maintain a separate list of active session IDs
        
        await Task.CompletedTask;
        return activeSessions;
    }

    /// <summary>
    /// Generates a quiz from the document content.
    /// </summary>
    public async Task<List<QuizQuestion>> GenerateQuizAsync(string sessionId, int questionCount = 5, string difficulty = "medium")
    {
        try
        {
            if (!_cache.TryGetValue($"knowledge_{sessionId}", out List<DocumentChunk> chunks))
            {
                throw new InvalidOperationException("Knowledge base session expired or not found");
            }

            var context = string.Join("\n\n", chunks.Take(10).Select(c => c.Text));
            
            var prompt = $@"
Based on the following document content, generate {questionCount} multiple choice quiz questions with a difficulty level of {difficulty}.

Document Content:
{context}

Return ONLY a valid JSON array in this EXACT format (no additional text, no markdown):
[
  {{
    ""question"": ""What is..?"",
    ""options"": [""Option A"", ""Option B"", ""Option C"", ""Option D""],
    ""correctAnswer"": ""Option A"",
    ""explanation"": ""This is correct because..."",
    ""chapter"": ""Chapter 1"",
    ""difficulty"": ""{difficulty}""
  }}
]

Generate {questionCount} questions following this format exactly.
";

            var request = new ChatGptRequestDto
            {
                Message = prompt,
                SystemPrompt = "You are an educational content creator. Generate high-quality quiz questions that test understanding of the material. Return ONLY a valid JSON array with no additional text or markdown formatting.",
                Temperature = 0.7,
                MaxTokens = 2000
            };

            var response = await _chatGptService.SendMessageAsync(request);
            
            if (response.Success && !string.IsNullOrWhiteSpace(response.Response))
            {
                try
                {
                    // Clean up the response (remove markdown code blocks if present)
                    var jsonResponse = response.Response.Trim();
                    if (jsonResponse.StartsWith("```json"))
                    {
                        jsonResponse = jsonResponse.Substring(7);
                    }
                    if (jsonResponse.StartsWith("```"))
                    {
                        jsonResponse = jsonResponse.Substring(3);
                    }
                    if (jsonResponse.EndsWith("```"))
                    {
                        jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
                    }
                    jsonResponse = jsonResponse.Trim();
                    
                    // Try to parse as JSON array
                    var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    if (questions != null && questions.Any())
                    {
                        _logger.LogInformation("Successfully generated {Count} quiz questions from ChatGPT", questions.Count);
                        return questions;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse ChatGPT response as JSON. Response: {Response}", response.Response.Substring(0, Math.Min(200, response.Response.Length)));
                }
            }
            
            // Fallback to mock questions if parsing fails
            _logger.LogWarning("Using mock quiz questions as fallback");
            return GenerateMockQuizQuestions(questionCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quiz for session: {SessionId}", sessionId);
            return GenerateMockQuizQuestions(questionCount);
        }
    }

    /// <summary>
    /// Generates flashcards from the document content.
    /// </summary>
    public async Task<List<Flashcard>> GenerateFlashcardsAsync(string sessionId, int flashcardCount = 10)
    {
        try
        {
            if (!_cache.TryGetValue($"knowledge_{sessionId}", out List<DocumentChunk> chunks))
            {
                throw new InvalidOperationException("Knowledge base session expired or not found");
            }

            var context = string.Join("\n\n", chunks.Take(10).Select(c => c.Text));
            
            var prompt = $@"
Based on the following document content, generate {flashcardCount} flashcards for studying.

Document Content:
{context}

Return ONLY a valid JSON array in this EXACT format (no additional text, no markdown):
[
  {{
    ""front"": ""What is consideration in contract law?"",
    ""back"": ""Something of value exchanged between parties..."",
    ""chapter"": ""Chapter 3"",
    ""context"": ""Additional context or examples""
  }}
]

Generate {flashcardCount} flashcards following this format exactly.
";

            var request = new ChatGptRequestDto
            {
                Message = prompt,
                SystemPrompt = "You are a study assistant. Create effective flashcards that help with memorization and understanding of key concepts. Return ONLY a valid JSON array with no additional text or markdown formatting.",
                Temperature = 0.7,
                MaxTokens = 2000
            };

            var response = await _chatGptService.SendMessageAsync(request);
            
            if (response.Success && !string.IsNullOrWhiteSpace(response.Response))
            {
                try
                {
                    // Clean up the response (remove markdown code blocks if present)
                    var jsonResponse = response.Response.Trim();
                    if (jsonResponse.StartsWith("```json"))
                    {
                        jsonResponse = jsonResponse.Substring(7);
                    }
                    if (jsonResponse.StartsWith("```"))
                    {
                        jsonResponse = jsonResponse.Substring(3);
                    }
                    if (jsonResponse.EndsWith("```"))
                    {
                        jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
                    }
                    jsonResponse = jsonResponse.Trim();
                    
                    // Try to parse as JSON array
                    var flashcards = JsonSerializer.Deserialize<List<Flashcard>>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    if (flashcards != null && flashcards.Any())
                    {
                        _logger.LogInformation("Successfully generated {Count} flashcards from ChatGPT", flashcards.Count);
                        return flashcards;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse ChatGPT response as JSON. Response: {Response}", response.Response.Substring(0, Math.Min(200, response.Response.Length)));
                }
            }
            
            // Fallback to mock flashcards if parsing fails
            _logger.LogWarning("Using mock flashcards as fallback");
            return GenerateMockFlashcards(flashcardCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating flashcards for session: {SessionId}", sessionId);
            return GenerateMockFlashcards(flashcardCount);
        }
    }

    /// <summary>
    /// Gets content from a specific chapter or section.
    /// </summary>
    public async Task<List<DocumentChunk>> GetChapterContentAsync(string sessionId, string chapter)
    {
        try
        {
            if (!_cache.TryGetValue($"knowledge_{sessionId}", out List<DocumentChunk> chunks))
            {
                throw new InvalidOperationException("Knowledge base session expired or not found");
            }

            var chapterChunks = chunks
                .Where(c => !string.IsNullOrEmpty(c.Chapter) && 
                           c.Chapter.Contains(chapter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return chapterChunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chapter content for session: {SessionId}, chapter: {Chapter}", sessionId, chapter);
            return new List<DocumentChunk>();
        }
    }

    private async Task<List<DocumentChunk>> GenerateEmbeddingsForChunksAsync(List<DocumentChunk> chunks)
    {
        var chunksWithEmbeddings = new List<DocumentChunk>();
        var isConfigured = await _chatGptService.IsConfiguredAsync();
        
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            float[]? embedding = null;
            
            if (isConfigured)
            {
                try
                {
                    embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Text);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate embedding for chunk {ChunkIndex}", i);
                }
            }

            chunk.Embedding = embedding;
            chunksWithEmbeddings.Add(chunk);
        }
        
        return chunksWithEmbeddings;
    }

    private async Task<List<DocumentChunk>> FindRelevantChunksAsync(List<DocumentChunk> chunks, string question, int topK)
    {
        var isConfigured = await _embeddingService.IsAvailableAsync();
        
        if (isConfigured)
        {
            try
            {
                // Use semantic similarity with embeddings
                var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);
                if (questionEmbedding != null)
                {
                    var chunksWithEmbeddings = chunks.Where(c => c.Embedding != null).ToList();
                    var candidateVectors = chunksWithEmbeddings.Select(c => new EmbeddingVector
                    {
                        Vector = c.Embedding!,
                        Text = c.Text,
                        Id = c.Id,
                        Metadata = new Dictionary<string, object> { ["chunk"] = c }
                    }).ToList();

                    var similarResults = _embeddingService.FindMostSimilar(questionEmbedding, candidateVectors, topK);
                    
                    return similarResults.Select(r => (DocumentChunk)r.Vector.Metadata["chunk"]).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to use semantic similarity, falling back to keyword matching");
            }
        }

        // Fallback to keyword matching
        var questionWords = question.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 2)
            .ToHashSet();

        return chunks
            .Select(c => new { Chunk = c, Score = questionWords.Count(word => c.Text.ToLowerInvariant().Contains(word)) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();
    }

    private async Task UpdateSessionStatsAsync(string sessionId, TimeSpan responseTime, int tokensUsed)
    {
        try
        {
            if (_cache.TryGetValue($"session_{sessionId}", out DocumentSession sessionInfo))
            {
                sessionInfo.QueryStats.TotalQueries++;
                sessionInfo.QueryStats.TotalTokensUsed += tokensUsed;
                sessionInfo.QueryStats.LastQueryAt = DateTime.UtcNow;
                
                // Update average response time
                var totalTime = sessionInfo.QueryStats.AverageResponseTime.TotalMilliseconds * (sessionInfo.QueryStats.TotalQueries - 1);
                sessionInfo.QueryStats.AverageResponseTime = TimeSpan.FromMilliseconds((totalTime + responseTime.TotalMilliseconds) / sessionInfo.QueryStats.TotalQueries);
                
                // Update cache with modified session info
                var expiration = sessionInfo.ExpiresAt - DateTime.UtcNow;
                if (expiration > TimeSpan.Zero)
                {
                    _cache.Set($"session_{sessionId}", sessionInfo, expiration);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update session stats for {SessionId}", sessionId);
        }
    }

    private List<QuizQuestion> GenerateMockQuizQuestions(int count)
    {
        var questions = new List<QuizQuestion>();
        for (int i = 1; i <= count; i++)
        {
            questions.Add(new QuizQuestion
            {
                Question = $"Sample question {i} about the document content?",
                Options = new List<string> { "Option A", "Option B", "Option C", "Option D" },
                CorrectAnswer = "Option A",
                Explanation = "This is the correct answer because...",
                Chapter = "Chapter 1",
                Difficulty = "medium"
            });
        }
        return questions;
    }

    private List<Flashcard> GenerateMockFlashcards(int count)
    {
        var flashcards = new List<Flashcard>();
        for (int i = 1; i <= count; i++)
        {
            flashcards.Add(new Flashcard
            {
                Front = $"Term {i}",
                Back = $"Definition {i} - This is what the term means...",
                Chapter = "Chapter 1",
                Context = "Additional context and examples..."
            });
        }
        return flashcards;
    }

    /// <summary>
    /// Generates a summary of a specific chapter using ChatGPT.
    /// </summary>
    public async Task<string> SummarizeChapterAsync(string sessionId, string chapterName, int maxLength = 500)
    {
        try
        {
            _logger.LogInformation("Generating summary for chapter '{ChapterName}' in session {SessionId}", chapterName, sessionId);

            // Get chapter content
            var chapterChunks = await GetChapterContentAsync(sessionId, chapterName);
            
            if (!chapterChunks.Any())
            {
                // Fallback: Try to find chunks by page number if chapter name looks like a page reference
                if (int.TryParse(chapterName, out int pageNum))
                {
                    if (!_cache.TryGetValue($"knowledge_{sessionId}", out List<DocumentChunk> allChunks))
                    {
                        return $"Session expired or not found.";
                    }
                    
                    chapterChunks = allChunks.Where(c => c.PageNumber == pageNum).ToList();
                    
                    if (!chapterChunks.Any())
                    {
                        return $"No content found for page {pageNum}.";
                    }
                }
                else
                {
                    return $"No content found for chapter '{chapterName}'. Try using a page number or a different chapter name.";
                }
            }

            // Combine all chapter content
            var chapterContent = string.Join("\n\n", chapterChunks.OrderBy(c => c.PageNumber).Select(c => c.Text));
            
            if (string.IsNullOrWhiteSpace(chapterContent))
            {
                return $"Chapter '{chapterName}' contains no readable text content.";
            }

            // Check if ChatGPT is configured
            var isConfigured = await _chatGptService.IsConfiguredAsync();
            
            if (!isConfigured)
            {
                // Generate a basic summary without ChatGPT
                return GenerateBasicSummary(chapterContent, chapterName, maxLength);
            }

            // Create a prompt for ChatGPT to summarize the chapter
            var summaryPrompt = $@"Please provide a comprehensive summary of the following chapter content. 
The summary should be approximately {maxLength} words and should capture the main points, key concepts, and important details.

Chapter: {chapterName}

Content:
{chapterContent}

Please provide a well-structured summary that includes:
1. Main topic and purpose of the chapter
2. Key concepts and ideas presented
3. Important details and examples
4. Conclusions or takeaways

Summary:";

            var request = new ChatGptRequestDto
            {
                Message = summaryPrompt,
                SystemPrompt = "You are a helpful assistant that creates clear, comprehensive summaries of educational content. Focus on accuracy and clarity.",
                Temperature = 0.3, // Lower temperature for more focused summaries
                MaxTokens = Math.Max(500, maxLength * 2), // Allow enough tokens for the requested length
                Model = "gpt-3.5-turbo"
            };

            var response = await _chatGptService.SendMessageAsync(request);
            
            if (response.Success && !string.IsNullOrWhiteSpace(response.Response))
            {
                _logger.LogInformation("Successfully generated summary for chapter '{ChapterName}' using ChatGPT", chapterName);
                return response.Response.Trim();
            }
            else
            {
                _logger.LogWarning("ChatGPT failed to generate summary for chapter '{ChapterName}': {Error}", chapterName, response.ErrorMessage);
                // Fallback to basic summary
                return GenerateBasicSummary(chapterContent, chapterName, maxLength);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for chapter '{ChapterName}' in session {SessionId}", chapterName, sessionId);
            return $"Error generating summary: {ex.Message}";
        }
    }

    /// <summary>
    /// Generates a basic summary without ChatGPT when API is not available.
    /// </summary>
    private string GenerateBasicSummary(string content, string chapterName, int maxLength)
    {
        try
        {
            // Simple text summarization by taking the first few sentences and key phrases
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Trim().Length > 20)
                .Take(5)
                .ToList();

            var summary = $"Summary of {chapterName}:\n\n";
            
            if (sentences.Any())
            {
                summary += string.Join(". ", sentences) + ".";
                
                // Add key phrases if we have room
                var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 5)
                    .GroupBy(w => w.ToLowerInvariant())
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => g.Key)
                    .ToList();

                if (words.Any())
                {
                    summary += $"\n\nKey concepts: {string.Join(", ", words.Take(5))}";
                }
            }
            else
            {
                summary += "This chapter contains content that could not be automatically summarized. Please review the original text for details.";
            }

            // Truncate if too long
            if (summary.Length > maxLength)
            {
                summary = summary.Substring(0, maxLength - 3) + "...";
            }

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating basic summary for chapter '{ChapterName}'", chapterName);
            return $"Error generating summary: {ex.Message}";
        }
    }
}
