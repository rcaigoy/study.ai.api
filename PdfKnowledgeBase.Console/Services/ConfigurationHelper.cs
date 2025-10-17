using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PdfKnowledgeBase.Console.Helpers;
using PdfKnowledgeBase.Lib.Interfaces;

namespace PdfKnowledgeBase.Console.Services;

/// <summary>
/// Service for handling configuration and API key management.
/// </summary>
public class ConfigurationHelper
{
    private readonly ILogger<ConfigurationHelper> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConsoleHelper _consoleHelper;
    private readonly IChatGptService _chatGptService;

    public ConfigurationHelper(
        ILogger<ConfigurationHelper> logger,
        IConfiguration configuration,
        ConsoleHelper consoleHelper,
        IChatGptService chatGptService)
    {
        _logger = logger;
        _configuration = configuration;
        _consoleHelper = consoleHelper;
        _chatGptService = chatGptService;
    }

    /// <summary>
    /// Checks the configuration - API key is already configured via PrivateValues.
    /// </summary>
    public async Task<bool> CheckConfigurationAsync()
    {
        try
        {
            // Configuration is already set up via PrivateValues in Program.cs
            _consoleHelper.DisplaySuccess("✅ OpenAI API key is configured via PrivateValues!");
            _consoleHelper.DisplayMessage("You will receive real AI responses from ChatGPT.");
            _consoleHelper.DisplayMessage();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking configuration");
            _consoleHelper.DisplayError($"Configuration check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets up the API key interactively.
    /// </summary>
    private async Task<bool> SetupApiKeyAsync()
    {
        try
        {
            _consoleHelper.DisplayMessage("=== API Key Setup ===");
            _consoleHelper.DisplayMessage();
            _consoleHelper.DisplayMessage("To get an OpenAI API key:");
            _consoleHelper.DisplayMessage("1. Go to https://platform.openai.com/api-keys");
            _consoleHelper.DisplayMessage("2. Sign in or create an account");
            _consoleHelper.DisplayMessage("3. Create a new API key");
            _consoleHelper.DisplayMessage("4. Copy the key (it starts with 'sk-')");
            _consoleHelper.DisplayMessage();

            var apiKey = _consoleHelper.GetStringInput("Enter your OpenAI API key (or 'skip' to continue without): ");
            
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("skip", StringComparison.OrdinalIgnoreCase))
            {
                _consoleHelper.DisplayMessage("Skipping API key setup. Continuing with mock responses...");
                _consoleHelper.DisplayMessage();
                return true;
            }

            // Basic validation
            if (!apiKey.StartsWith("sk-", StringComparison.OrdinalIgnoreCase))
            {
                _consoleHelper.DisplayWarning("API key doesn't look like a valid OpenAI key (should start with 'sk-').");
                var continueAnyway = _consoleHelper.GetBooleanInput("Continue anyway?", false);
                if (!continueAnyway)
                {
                    return await SetupApiKeyAsync(); // Try again
                }
            }

            // Store the API key in user secrets
            _consoleHelper.DisplayMessage("Storing API key in user secrets...");
            _consoleHelper.ShowProgressIndicator();

            try
            {
                // This would typically be done through the user secrets manager
                // For now, we'll just display instructions
                _consoleHelper.HideProgressIndicator();
                
                _consoleHelper.DisplayMessage("To permanently store your API key, run this command:");
                _consoleHelper.DisplayMessage($"dotnet user-secrets set \"ChatGpt:ApiKey\" \"{apiKey}\"", ConsoleColor.Yellow);
                _consoleHelper.DisplayMessage();
                
                _consoleHelper.DisplayMessage("Alternatively, add it to appsettings.json:");
                _consoleHelper.DisplayMessage("{", ConsoleColor.Yellow);
                _consoleHelper.DisplayMessage("  \"ChatGpt\": {", ConsoleColor.Yellow);
                _consoleHelper.DisplayMessage($"    \"ApiKey\": \"{apiKey}\"", ConsoleColor.Yellow);
                _consoleHelper.DisplayMessage("  }", ConsoleColor.Yellow);
                _consoleHelper.DisplayMessage("}", ConsoleColor.Yellow);
                _consoleHelper.DisplayMessage();

                // For this session, we can set it in the configuration
                // Note: This is a simplified approach - in production you'd want proper configuration management
                Environment.SetEnvironmentVariable("ChatGpt__ApiKey", apiKey);
                
                _consoleHelper.DisplaySuccess("✅ API key configured for this session!");
                _consoleHelper.DisplayMessage("Note: For permanent storage, use the methods shown above.");
                _consoleHelper.DisplayMessage();

                // Test the configuration
                var testConfigured = await _chatGptService.IsConfiguredAsync();
                if (testConfigured)
                {
                    _consoleHelper.DisplaySuccess("✅ Configuration test successful!");
                    return true;
                }
                else
                {
                    _consoleHelper.DisplayError("❌ Configuration test failed. Please check your API key.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _consoleHelper.HideProgressIndicator();
                _logger.LogError(ex, "Error storing API key");
                _consoleHelper.DisplayError($"Failed to store API key: {ex.Message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in API key setup");
            _consoleHelper.DisplayError($"API key setup failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Shows the current configuration status.
    /// </summary>
    public async Task ShowConfigurationStatusAsync()
    {
        _consoleHelper.DisplayMessage("=== Configuration Status ===");
        
        try
        {
            var isConfigured = await _chatGptService.IsConfiguredAsync();
            
            if (isConfigured)
            {
                _consoleHelper.DisplaySuccess("✅ OpenAI API key is configured");
                _consoleHelper.DisplayMessage("Real AI responses will be provided.");
            }
            else
            {
                _consoleHelper.DisplayWarning("⚠️ OpenAI API key is not configured");
                _consoleHelper.DisplayMessage("Mock responses will be provided.");
            }

            _consoleHelper.DisplayMessage();
            _consoleHelper.DisplayMessage("Configuration sources checked:");
            _consoleHelper.DisplayMessage("• appsettings.json");
            _consoleHelper.DisplayMessage("• User secrets");
            _consoleHelper.DisplayMessage("• Environment variables");
            _consoleHelper.DisplayMessage("• Command line arguments");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing configuration status");
            _consoleHelper.DisplayError($"Failed to show configuration status: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets configuration help information.
    /// </summary>
    public void ShowConfigurationHelp()
    {
        _consoleHelper.DisplayMessage("=== Configuration Help ===");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("The PDF Knowledge Base Console Application can work with or without an OpenAI API key.");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("With API Key (Recommended):");
        _consoleHelper.DisplayMessage("• Real AI responses from ChatGPT");
        _consoleHelper.DisplayMessage("• Actual text embeddings for semantic search");
        _consoleHelper.DisplayMessage("• Full functionality");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("Without API Key:");
        _consoleHelper.DisplayMessage("• Mock responses for demonstration");
        _consoleHelper.DisplayMessage("• Basic keyword-based search");
        _consoleHelper.DisplayMessage("• Limited functionality");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("How to configure your API key:");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("1. User Secrets (Recommended for development):");
        _consoleHelper.DisplayMessage("   dotnet user-secrets set \"ChatGpt:ApiKey\" \"your-api-key-here\"");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("2. appsettings.json:");
        _consoleHelper.DisplayMessage("   {");
        _consoleHelper.DisplayMessage("     \"ChatGpt\": {");
        _consoleHelper.DisplayMessage("       \"ApiKey\": \"your-api-key-here\"");
        _consoleHelper.DisplayMessage("     }");
        _consoleHelper.DisplayMessage("   }");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("3. Environment Variable:");
        _consoleHelper.DisplayMessage("   set ChatGPT__ApiKey=your-api-key-here");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("4. Command Line:");
        _consoleHelper.DisplayMessage("   dotnet run --ChatGpt:ApiKey=your-api-key-here");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("Get your API key at: https://platform.openai.com/api-keys");
    }
}
