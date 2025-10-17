using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PdfKnowledgeBase.Lib.DTOs;
using PdfKnowledgeBase.Lib.Interfaces;
using study.ai.api.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace PdfKnowledgeBase.Lib.Services;

/// <summary>
/// Service for ChatGPT/OpenAI API integration.
/// </summary>
public class ChatGptService : IChatGptService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatGptService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChatGptService(HttpClient httpClient, IConfiguration configuration, ILogger<ChatGptService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // The API key is now set via HTTP client configuration in ServiceCollectionExtensions
        // Check if it's already set in the headers, otherwise try to get it from PrivateValues
        if (_httpClient.DefaultRequestHeaders.Authorization == null)
        {
            var apiKey = PrivateValues.ChatGPTApiKey;
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                _logger.LogInformation("ChatGPT API key configured from PrivateValues");
            }
            else
            {
                _logger.LogWarning("No ChatGPT API key found in PrivateValues");
            }
        }
        else
        {
            _logger.LogInformation("ChatGPT API key configured via HTTP client");
        }

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        }

        _logger.LogInformation("ChatGPT service initialized with base address: {BaseAddress}", _httpClient.BaseAddress);
    }

    /// <summary>
    /// Sends a message to ChatGPT and gets a response.
    /// </summary>
    public async Task<ChatGptResponseDto> SendMessageAsync(ChatGptRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var apiKey = PrivateValues.ChatGPTApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return CreateMockResponse(request.Message, stopwatch.Elapsed.TotalMilliseconds);
            }

            var messages = BuildMessages(request);
            var requestBody = new
            {
                model = request.Model,
                messages = messages,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to ChatGPT API: {Model}", request.Model);

            var response = await _httpClient.PostAsync("chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ChatGPT API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                return new ChatGptResponseDto
                {
                    Success = false,
                    ErrorMessage = $"API Error: {response.StatusCode}",
                    ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ChatGptApiResponse>(responseJson, _jsonOptions);

            if (apiResponse?.Choices?.Count > 0)
            {
                var choice = apiResponse.Choices[0];
                var assistantMessage = new ChatMessageDto
                {
                    Role = "assistant",
                    Content = choice.Message?.Content ?? ""
                };

                return new ChatGptResponseDto
                {
                    Response = choice.Message?.Content ?? "",
                    Model = apiResponse.Model ?? request.Model,
                    TokensUsed = apiResponse.Usage?.TotalTokens ?? 0,
                    ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                    Success = true,
                    AssistantMessage = assistantMessage
                };
            }

            return new ChatGptResponseDto
            {
                Success = false,
                ErrorMessage = "No response from ChatGPT API",
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ChatGPT API");
            return new ChatGptResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
    }

    /// <summary>
    /// Sends a message with conversation history.
    /// </summary>
    public async Task<ChatGptResponseDto> SendMessageWithHistoryAsync(ChatGptRequestDto request, List<ChatMessageDto> conversationHistory)
    {
        // Add conversation history to the request
        request.ConversationHistory = conversationHistory;
        return await SendMessageAsync(request);
    }

    /// <summary>
    /// Analyzes an image using ChatGPT Vision API.
    /// </summary>
    public async Task<ChatGptResponseDto> AnalyzeImageAsync(byte[] imageBytes, string prompt)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var apiKey = PrivateValues.ChatGPTApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return CreateMockResponse($"Image analysis for prompt: {prompt}", stopwatch.Elapsed.TotalMilliseconds);
            }

            var base64Image = Convert.ToBase64String(imageBytes);
            var messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/jpeg;base64,{base64Image}" }
                        }
                    }
                }
            };

            var requestBody = new
            {
                model = "gpt-4-vision-preview",
                messages = messages,
                max_tokens = 1000
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ChatGPT Vision API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                return new ChatGptResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Vision API Error: {response.StatusCode}",
                    ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ChatGptApiResponse>(responseJson, _jsonOptions);

            if (apiResponse?.Choices?.Count > 0)
            {
                var choice = apiResponse.Choices[0];
                return new ChatGptResponseDto
                {
                    Response = choice.Message?.Content ?? "",
                    Model = apiResponse.Model ?? "gpt-4-vision-preview",
                    TokensUsed = apiResponse.Usage?.TotalTokens ?? 0,
                    ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                    Success = true
                };
            }

            return new ChatGptResponseDto
            {
                Success = false,
                ErrorMessage = "No response from ChatGPT Vision API",
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ChatGPT Vision API");
            return new ChatGptResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
    }

    /// <summary>
    /// Generates an embedding for the given text.
    /// </summary>
    public async Task<ChatGptResponseDto> GenerateEmbeddingAsync(string text)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var apiKey = PrivateValues.ChatGPTApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return CreateMockResponse($"Embedding generated for: {text}", stopwatch.Elapsed.TotalMilliseconds);
            }

            var requestBody = new
            {
                model = "text-embedding-ada-002",
                input = text
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("embeddings", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ChatGPT Embeddings API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                return new ChatGptResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Embeddings API Error: {response.StatusCode}",
                    ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ChatGptEmbeddingResponse>(responseJson, _jsonOptions);

            if (apiResponse?.Data?.Count > 0)
            {
                var embedding = apiResponse.Data[0];
                return new ChatGptResponseDto
                {
                    Response = $"Embedding generated with {embedding.Embedding?.Count ?? 0} dimensions",
                    Model = apiResponse.Model ?? "text-embedding-ada-002",
                    TokensUsed = apiResponse.Usage?.TotalTokens ?? 0,
                    ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                    Success = true
                };
            }

            return new ChatGptResponseDto
            {
                Success = false,
                ErrorMessage = "No embedding response from ChatGPT API",
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ChatGPT Embeddings API");
            return new ChatGptResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
    }

    /// <summary>
    /// Gets the embedding vector for the given text.
    /// </summary>
    public async Task<float[]?> GetEmbeddingVectorAsync(string text)
    {
        try
        {
            var apiKey = PrivateValues.ChatGPTApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return null;
            }

            var requestBody = new
            {
                model = "text-embedding-ada-002",
                input = text
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("embeddings", content);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ChatGptEmbeddingResponse>(responseJson, _jsonOptions);

            if (apiResponse?.Data?.Count > 0)
            {
                var embedding = apiResponse.Data[0];
                return embedding.Embedding?.Select(x => (float)x).ToArray();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting embedding vector");
            return null;
        }
    }

    /// <summary>
    /// Sends a message to a custom GPT assistant.
    /// </summary>
    public async Task<ChatGptResponseDto> SendMessageToCustomGptAsync(string assistantId, ChatGptRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var apiKey = PrivateValues.ChatGPTApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return CreateMockResponse(request.Message, stopwatch.Elapsed.TotalMilliseconds);
            }

            // For now, return a mock response since we don't have the full OpenAI Assistants API implementation
            _logger.LogInformation("Sending message to custom GPT assistant: {AssistantId}", assistantId);
            
            return new ChatGptResponseDto
            {
                Response = $"This is a mock response from custom assistant {assistantId}. The message was: {request.Message}",
                Model = "custom-gpt",
                TokensUsed = 50,
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling custom GPT assistant: {AssistantId}", assistantId);
            return new ChatGptResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
    }

    /// <summary>
    /// Checks if the ChatGPT service is properly configured.
    /// </summary>
    public async Task<bool> IsConfiguredAsync()
    {
        // Check if API key is set in HTTP client headers (from options)
        var httpClientAuth = _httpClient.DefaultRequestHeaders.Authorization;
        if (httpClientAuth != null && !string.IsNullOrEmpty(httpClientAuth.Parameter))
        {
            _logger.LogInformation("ChatGPT API key found in HTTP client headers");
            //return true;
        }

        // Fallback to check PrivateValues
        var apiKey = PrivateValues.ChatGPTApiKey;
        var isConfigured = !string.IsNullOrEmpty(apiKey);
        
        if (isConfigured)
        {
            _logger.LogInformation("ChatGPT API key found in PrivateValues");
            // If we found it in PrivateValues but not in HTTP client, try to set it
            if (httpClientAuth == null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                _logger.LogInformation("Set ChatGPT API key from PrivateValues to HTTP client");
            }
        }
        else
        {
            _logger.LogWarning("ChatGPT API key not found in HTTP client headers or PrivateValues");
            _logger.LogWarning("HTTP Client Authorization: {Auth}", httpClientAuth?.ToString() ?? "NULL");
            _logger.LogWarning("PrivateValues API Key: {ApiKey}", apiKey ?? "NULL");
        }
        
        return isConfigured;
    }

    private List<object> BuildMessages(ChatGptRequestDto request)
    {
        var messages = new List<object>();

        // Add system prompt if provided
        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            messages.Add(new { role = "system", content = request.SystemPrompt });
        }

        // Add conversation history if provided
        if (request.ConversationHistory != null)
        {
            foreach (var message in request.ConversationHistory)
            {
                messages.Add(new { role = message.Role, content = message.Content });
            }
        }

        // Add current user message
        messages.Add(new { role = "user", content = request.Message });

        return messages;
    }

    private ChatGptResponseDto CreateMockResponse(string message, double processingTimeMs)
    {
        _logger.LogWarning("ChatGPT API key not configured, returning mock response");
        
        var mockResponses = new[]
        {
            "This is a mock response from ChatGPT. Please configure your API key to get real responses.",
            "I'm a mock ChatGPT response. Add your OpenAI API key to the configuration to enable real AI responses.",
            "Mock response: I understand you're asking about '" + message + "'. Configure your API key for real AI assistance.",
            "This is a placeholder response. To get actual AI responses, please add your OpenAI API key to the appsettings.json file."
        };

        var random = new Random();
        var mockResponse = mockResponses[random.Next(mockResponses.Length)];

        return new ChatGptResponseDto
        {
            Response = mockResponse,
            Model = "gpt-3.5-turbo (mock)",
            TokensUsed = 50,
            ProcessingTimeMs = processingTimeMs,
            Success = true
        };
    }
}

// API Response Models
public class ChatGptApiResponse
{
    public string? Id { get; set; }
    public string? Object { get; set; }
    public long Created { get; set; }
    public string? Model { get; set; }
    public List<ChatGptChoice>? Choices { get; set; }
    public ChatGptUsageDto? Usage { get; set; }
}

public class ChatGptChoice
{
    public int Index { get; set; }
    public ChatGptMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}

public class ChatGptMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public class ChatGptEmbeddingResponse
{
    public string? Object { get; set; }
    public List<ChatGptEmbeddingData>? Data { get; set; }
    public string? Model { get; set; }
    public ChatGptUsageDto? Usage { get; set; }
}

public class ChatGptEmbeddingData
{
    public string? Object { get; set; }
    public List<double>? Embedding { get; set; }
    public int Index { get; set; }
}
