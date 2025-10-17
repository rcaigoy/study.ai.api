using Microsoft.Extensions.DependencyInjection;
using PdfKnowledgeBase.Lib.Interfaces;
using PdfKnowledgeBase.Lib.Services;

namespace PdfKnowledgeBase.Lib.Extensions;

/// <summary>
/// Extension methods for configuring PDF Knowledge Base services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PDF Knowledge Base services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPdfKnowledgeBase(this IServiceCollection services)
    {
        // Add core services
        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IChatGptService, ChatGptService>();
        services.AddScoped<ITemporaryKnowledgeService, TemporaryKnowledgeService>();

        // Add HTTP client for ChatGPT service
        services.AddHttpClient<IChatGptService, ChatGptService>();

        // Add memory cache for temporary knowledge bases
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Adds PDF Knowledge Base services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for customizing services.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPdfKnowledgeBase(this IServiceCollection services, Action<PdfKnowledgeBaseOptions> configure)
    {
        var options = new PdfKnowledgeBaseOptions();
        configure(options);

        // Add core services
        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IChatGptService, ChatGptService>();
        services.AddScoped<ITemporaryKnowledgeService, TemporaryKnowledgeService>();

        // Add HTTP client for ChatGPT service with custom configuration
        services.AddHttpClient<IChatGptService, ChatGptService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
            if (options.ChatGptApiKey != null)
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ChatGptApiKey);
            }
        });

        // Add memory cache with custom configuration
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = options.SizeLimit;
        });

        // Register options
        services.AddSingleton(options);

        return services;
    }
}

/// <summary>
/// Configuration options for PDF Knowledge Base services.
/// </summary>
public class PdfKnowledgeBaseOptions
{
    /// <summary>
    /// The ChatGPT API key.
    /// </summary>
    public string? ChatGptApiKey { get; set; }

    /// <summary>
    /// HTTP timeout in seconds for API calls.
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum cache size limit.
    /// </summary>
    public int SizeLimit { get; set; } = 1000;

    /// <summary>
    /// Default chunk size for text processing.
    /// </summary>
    public int DefaultChunkSize { get; set; } = 1000;

    /// <summary>
    /// Default chunk overlap size.
    /// </summary>
    public int DefaultChunkOverlap { get; set; } = 200;

    /// <summary>
    /// Default session expiration time in hours.
    /// </summary>
    public int DefaultSessionExpirationHours { get; set; } = 2;

    /// <summary>
    /// Maximum file size in MB for PDF processing.
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 50;
}
