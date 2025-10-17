using PdfKnowledgeBase.Lib.DTOs;

namespace PdfKnowledgeBase.Lib.Interfaces;

/// <summary>
/// Interface for ChatGPT/OpenAI API integration.
/// </summary>
public interface IChatGptService
{
    /// <summary>
    /// Sends a message to ChatGPT and gets a response.
    /// </summary>
    /// <param name="request">The request containing the message and parameters.</param>
    /// <returns>The response from ChatGPT.</returns>
    Task<ChatGptResponseDto> SendMessageAsync(ChatGptRequestDto request);

    /// <summary>
    /// Sends a message with conversation history.
    /// </summary>
    /// <param name="request">The request containing the message and parameters.</param>
    /// <param name="conversationHistory">Previous messages in the conversation.</param>
    /// <returns>The response from ChatGPT.</returns>
    Task<ChatGptResponseDto> SendMessageWithHistoryAsync(ChatGptRequestDto request, List<ChatMessageDto> conversationHistory);

    /// <summary>
    /// Analyzes an image using ChatGPT Vision API.
    /// </summary>
    /// <param name="imageBytes">The image data as bytes.</param>
    /// <param name="prompt">The prompt describing what to analyze.</param>
    /// <returns>The analysis response.</returns>
    Task<ChatGptResponseDto> AnalyzeImageAsync(byte[] imageBytes, string prompt);

    /// <summary>
    /// Generates an embedding for the given text.
    /// </summary>
    /// <param name="text">The text to generate an embedding for.</param>
    /// <returns>The embedding response.</returns>
    Task<ChatGptResponseDto> GenerateEmbeddingAsync(string text);

    /// <summary>
    /// Sends a message to a custom GPT assistant.
    /// </summary>
    /// <param name="assistantId">The ID of the custom assistant.</param>
    /// <param name="request">The request containing the message.</param>
    /// <returns>The response from the custom assistant.</returns>
    Task<ChatGptResponseDto> SendMessageToCustomGptAsync(string assistantId, ChatGptRequestDto request);

    /// <summary>
    /// Checks if the ChatGPT service is properly configured.
    /// </summary>
    /// <returns>True if configured, false otherwise.</returns>
    Task<bool> IsConfiguredAsync();

    /// <summary>
    /// Gets the embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to get embedding for.</param>
    /// <returns>The embedding vector or null if not available.</returns>
    Task<float[]?> GetEmbeddingVectorAsync(string text);
}
