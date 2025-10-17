namespace PdfKnowledgeBase.Lib.DTOs;

/// <summary>
/// Response DTO from ChatGPT API calls.
/// </summary>
public class ChatGptResponseDto
{
    /// <summary>
    /// The response content from ChatGPT.
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// The model that generated the response.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Number of tokens used in the request/response.
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public double ProcessingTimeMs { get; set; }

    /// <summary>
    /// Whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the request failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The assistant's message for conversation tracking.
    /// </summary>
    public ChatMessageDto? AssistantMessage { get; set; }
}

/// <summary>
/// Token usage information from ChatGPT API.
/// </summary>
public class ChatGptUsageDto
{
    /// <summary>
    /// Number of tokens in the prompt.
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Number of tokens in the completion.
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens used.
    /// </summary>
    public int TotalTokens { get; set; }
}
