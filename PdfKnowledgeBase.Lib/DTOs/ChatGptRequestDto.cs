using System.ComponentModel.DataAnnotations;

namespace PdfKnowledgeBase.Lib.DTOs;

/// <summary>
/// Request DTO for ChatGPT API calls.
/// </summary>
public class ChatGptRequestDto
{
    /// <summary>
    /// The message to send to ChatGPT.
    /// </summary>
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional system prompt to set the context.
    /// </summary>
    [StringLength(1000)]
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Temperature for response generation (0.1 to 2.0).
    /// </summary>
    [Range(0.1, 2.0)]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum number of tokens in the response.
    /// </summary>
    [Range(1, 4096)]
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// The model to use for the request.
    /// </summary>
    [StringLength(50)]
    public string Model { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// Conversation history for context.
    /// </summary>
    public List<ChatMessageDto>? ConversationHistory { get; set; }
}

/// <summary>
/// Represents a chat message in the conversation.
/// </summary>
public class ChatMessageDto
{
    /// <summary>
    /// The role of the message sender (system, user, assistant).
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The content of the message.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;
}
