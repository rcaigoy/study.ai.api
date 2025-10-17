using PdfKnowledgeBase.Lib.DTOs;
using PdfKnowledgeBase.Lib.Models;

namespace PdfKnowledgeBase.Lib.Interfaces;

/// <summary>
/// Interface for managing temporary knowledge bases from PDF documents.
/// </summary>
public interface ITemporaryKnowledgeService
{
    /// <summary>
    /// Creates a temporary knowledge base from a PDF file.
    /// </summary>
    /// <param name="fileStream">The PDF file stream.</param>
    /// <param name="fileName">The name of the PDF file.</param>
    /// <param name="expiration">How long the knowledge base should be kept in memory.</param>
    /// <returns>The session ID for the created knowledge base.</returns>
    Task<string> CreateTemporaryKnowledgeBaseAsync(Stream fileStream, string fileName, TimeSpan expiration);

    /// <summary>
    /// Queries the temporary knowledge base with a question.
    /// </summary>
    /// <param name="sessionId">The session ID of the knowledge base.</param>
    /// <param name="request">The query request.</param>
    /// <returns>The query response with answer and relevant chunks.</returns>
    Task<DocumentQueryResponse> QueryTemporaryKnowledgeAsync(string sessionId, DocumentQueryRequest request);

    /// <summary>
    /// Extends the expiration time of a session.
    /// </summary>
    /// <param name="sessionId">The session ID to extend.</param>
    /// <param name="additionalTime">Additional time to add to the session.</param>
    /// <returns>True if the session was extended successfully.</returns>
    Task<bool> ExtendSessionAsync(string sessionId, TimeSpan additionalTime);

    /// <summary>
    /// Deletes a session and frees up memory.
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <returns>True if the session was deleted successfully.</returns>
    Task<bool> DeleteSessionAsync(string sessionId);

    /// <summary>
    /// Gets information about a session.
    /// </summary>
    /// <param name="sessionId">The session ID to get information for.</param>
    /// <returns>Session information or null if not found.</returns>
    Task<DocumentSessionResponse?> GetSessionInfoAsync(string sessionId);

    /// <summary>
    /// Gets all active sessions.
    /// </summary>
    /// <returns>List of active sessions.</returns>
    Task<List<DocumentSessionResponse>> GetActiveSessionsAsync();

    /// <summary>
    /// Generates a quiz from the document content.
    /// </summary>
    /// <param name="sessionId">The session ID to generate quiz from.</param>
    /// <param name="questionCount">Number of questions to generate.</param>
    /// <param name="difficulty">Difficulty level of the quiz.</param>
    /// <returns>Generated quiz questions.</returns>
    Task<List<QuizQuestion>> GenerateQuizAsync(string sessionId, int questionCount = 5, string difficulty = "medium");

    /// <summary>
    /// Generates flashcards from the document content.
    /// </summary>
    /// <param name="sessionId">The session ID to generate flashcards from.</param>
    /// <param name="flashcardCount">Number of flashcards to generate.</param>
    /// <returns>Generated flashcards.</returns>
    Task<List<Flashcard>> GenerateFlashcardsAsync(string sessionId, int flashcardCount = 10);

    /// <summary>
    /// Gets content from a specific chapter or section.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="chapter">The chapter or section name.</param>
    /// <returns>Chunks from the specified chapter.</returns>
    Task<List<DocumentChunk>> GetChapterContentAsync(string sessionId, string chapter);

    /// <summary>
    /// Generates a summary of a specific chapter using ChatGPT.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="chapterName">The chapter or section name.</param>
    /// <param name="maxLength">Maximum length of the summary in words.</param>
    /// <returns>A summary of the chapter content.</returns>
    Task<string> SummarizeChapterAsync(string sessionId, string chapterName, int maxLength = 500);
}

/// <summary>
/// Represents a quiz question.
/// </summary>
public class QuizQuestion
{
    /// <summary>
    /// The question text.
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The possible answers.
    /// </summary>
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// The correct answer.
    /// </summary>
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// Explanation of the correct answer.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// The chapter or section this question relates to.
    /// </summary>
    public string? Chapter { get; set; }

    /// <summary>
    /// The difficulty level.
    /// </summary>
    public string Difficulty { get; set; } = "medium";
}

/// <summary>
/// Represents a flashcard.
/// </summary>
public class Flashcard
{
    /// <summary>
    /// The front of the flashcard (question or term).
    /// </summary>
    public string Front { get; set; } = string.Empty;

    /// <summary>
    /// The back of the flashcard (answer or definition).
    /// </summary>
    public string Back { get; set; } = string.Empty;

    /// <summary>
    /// The chapter or section this flashcard relates to.
    /// </summary>
    public string? Chapter { get; set; }

    /// <summary>
    /// Additional context or examples.
    /// </summary>
    public string? Context { get; set; }
}
