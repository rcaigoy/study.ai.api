using Microsoft.Extensions.Logging;
using PdfKnowledgeBase.Console.Helpers;
using PdfKnowledgeBase.Lib.Interfaces;

namespace PdfKnowledgeBase.Console.Services;

/// <summary>
/// Service for managing PDF knowledge base sessions.
/// </summary>
public class SessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly ITemporaryKnowledgeService _knowledgeService;
    private readonly ConsoleHelper _consoleHelper;

    public SessionManager(
        ILogger<SessionManager> logger,
        ITemporaryKnowledgeService knowledgeService,
        ConsoleHelper consoleHelper)
    {
        _logger = logger;
        _knowledgeService = knowledgeService;
        _consoleHelper = consoleHelper;
    }

    /// <summary>
    /// Runs the session management interface.
    /// </summary>
    public async Task RunSessionManagementAsync(string? currentSessionId)
    {
        _consoleHelper.DisplayMessage("=== Session Management ===");
        
        if (string.IsNullOrEmpty(currentSessionId))
        {
            _consoleHelper.DisplayMessage("No active session to manage.");
            _consoleHelper.DisplayMessage("Load a PDF document first to create a session.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        bool continueManaging = true;
        while (continueManaging)
        {
            try
            {
                _consoleHelper.DisplayMessage($"Managing session: {currentSessionId}");
                _consoleHelper.DisplayMessage();
                
                _consoleHelper.DisplayMessage("Session Management Options:");
                _consoleHelper.DisplayMessage("1. Extend session expiration time");
                _consoleHelper.DisplayMessage("2. Delete current session");
                _consoleHelper.DisplayMessage("3. View session details");
                _consoleHelper.DisplayMessage("4. Return to main menu");
                _consoleHelper.DisplayMessage();

                var choice = _consoleHelper.GetStringInput("Select an option (1-4): ");

                switch (choice)
                {
                    case "1":
                        await ExtendSessionAsync(currentSessionId);
                        break;
                    case "2":
                        var deleted = await DeleteSessionAsync(currentSessionId);
                        if (deleted)
                        {
                            continueManaging = false;
                        }
                        break;
                    case "3":
                        await ShowSessionDetailsAsync(currentSessionId);
                        break;
                    case "4":
                        continueManaging = false;
                        break;
                    default:
                        _consoleHelper.DisplayError("Invalid choice. Please try again.");
                        break;
                }

                if (continueManaging)
                {
                    await _consoleHelper.WaitForKeyPressAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in session management");
                _consoleHelper.DisplayError($"An error occurred: {ex.Message}");
                await _consoleHelper.WaitForKeyPressAsync();
            }
        }
    }

    /// <summary>
    /// Extends the session expiration time.
    /// </summary>
    private async Task ExtendSessionAsync(string sessionId)
    {
        _consoleHelper.DisplayMessage("=== Extend Session ===");
        
        try
        {
            // Get current session info first
            var sessionInfo = await _knowledgeService.GetSessionInfoAsync(sessionId);
            if (sessionInfo == null)
            {
                _consoleHelper.DisplayError("Session not found or expired.");
                return;
            }

            _consoleHelper.DisplayMessage($"Current expiration: {sessionInfo.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
            _consoleHelper.DisplayMessage($"Time until expiration: {sessionInfo.ExpiresAt - DateTime.UtcNow:hh\\:mm\\:ss}");
            _consoleHelper.DisplayMessage();

            _consoleHelper.DisplayMessage("Available extension options:");
            _consoleHelper.DisplayMessage("1. 1 hour");
            _consoleHelper.DisplayMessage("2. 2 hours");
            _consoleHelper.DisplayMessage("3. 4 hours");
            _consoleHelper.DisplayMessage("4. 8 hours");
            _consoleHelper.DisplayMessage("5. Custom duration");
            _consoleHelper.DisplayMessage();

            var choice = _consoleHelper.GetStringInput("Select extension duration (1-5): ");

            TimeSpan extension;
            switch (choice)
            {
                case "1":
                    extension = TimeSpan.FromHours(1);
                    break;
                case "2":
                    extension = TimeSpan.FromHours(2);
                    break;
                case "3":
                    extension = TimeSpan.FromHours(4);
                    break;
                case "4":
                    extension = TimeSpan.FromHours(8);
                    break;
                case "5":
                    extension = await GetCustomExtensionAsync();
                    break;
                default:
                    _consoleHelper.DisplayError("Invalid choice.");
                    return;
            }

            if (extension <= TimeSpan.Zero)
            {
                _consoleHelper.DisplayError("Invalid extension duration.");
                return;
            }

            _consoleHelper.DisplayMessage($"Extending session by {extension:hh\\:mm\\:ss}...");
            _consoleHelper.ShowProgressIndicator();

            var success = await _knowledgeService.ExtendSessionAsync(sessionId, extension);

            _consoleHelper.HideProgressIndicator();

            if (success)
            {
                var newExpiration = sessionInfo.ExpiresAt.Add(extension);
                _consoleHelper.DisplaySuccess($"Session extended successfully!");
                _consoleHelper.DisplayMessage($"New expiration: {newExpiration:yyyy-MM-dd HH:mm:ss}");
                _consoleHelper.DisplayMessage($"Total session duration: {newExpiration - sessionInfo.ExpiresAt.Add(-extension):hh\\:mm\\:ss}");
            }
            else
            {
                _consoleHelper.DisplayError("Failed to extend session. It may have expired.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending session: {SessionId}", sessionId);
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to extend session: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets custom extension duration from user input.
    /// </summary>
    private async Task<TimeSpan> GetCustomExtensionAsync()
    {
        _consoleHelper.DisplayMessage("Enter custom extension duration:");
        
        var hours = _consoleHelper.GetIntegerInput("Hours: ", 1);
        var minutes = _consoleHelper.GetIntegerInput("Minutes: ", 0);
        
        var extension = TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(minutes));
        
        _consoleHelper.DisplayMessage($"Custom extension: {extension:hh\\:mm\\:ss}");
        
        var confirm = _consoleHelper.GetBooleanInput("Confirm this extension?");
        
        return confirm ? extension : TimeSpan.Zero;
    }

    /// <summary>
    /// Deletes the current session.
    /// </summary>
    private async Task<bool> DeleteSessionAsync(string sessionId)
    {
        _consoleHelper.DisplayMessage("=== Delete Session ===");
        
        try
        {
            var sessionInfo = await _knowledgeService.GetSessionInfoAsync(sessionId);
            if (sessionInfo != null)
            {
                _consoleHelper.DisplayMessage($"Session: {sessionInfo.SessionId}");
                _consoleHelper.DisplayMessage($"File: {sessionInfo.FileName}");
                _consoleHelper.DisplayMessage($"Expires: {sessionInfo.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
                _consoleHelper.DisplayMessage();
            }

            var confirm = _consoleHelper.GetBooleanInput("Are you sure you want to delete this session?", false);
            
            if (!confirm)
            {
                _consoleHelper.DisplayMessage("Session deletion cancelled.");
                return false;
            }

            _consoleHelper.DisplayMessage("Deleting session...");
            _consoleHelper.ShowProgressIndicator();

            var success = await _knowledgeService.DeleteSessionAsync(sessionId);

            _consoleHelper.HideProgressIndicator();

            if (success)
            {
                _consoleHelper.DisplaySuccess("Session deleted successfully!");
                _consoleHelper.DisplayMessage("You can load a new PDF document from the main menu.");
                return true;
            }
            else
            {
                _consoleHelper.DisplayError("Failed to delete session.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session: {SessionId}", sessionId);
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to delete session: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Shows detailed session information.
    /// </summary>
    private async Task ShowSessionDetailsAsync(string sessionId)
    {
        _consoleHelper.DisplayMessage("=== Session Details ===");
        
        try
        {
            var sessionInfo = await _knowledgeService.GetSessionInfoAsync(sessionId);
            
            if (sessionInfo == null)
            {
                _consoleHelper.DisplayError("Session not found or expired.");
                return;
            }

            // Display basic information
            _consoleHelper.DisplayMessage("Basic Information:");
            _consoleHelper.DisplayMessage($"  Session ID: {sessionInfo.SessionId}");
            _consoleHelper.DisplayMessage($"  File Name: {sessionInfo.FileName}");
            _consoleHelper.DisplayMessage($"  File Size: {sessionInfo.FileSize:N0} bytes");
            _consoleHelper.DisplayMessage($"  Pages: {sessionInfo.PageCount}");
            _consoleHelper.DisplayMessage($"  Chunks: {sessionInfo.ChunkCount}");
            _consoleHelper.DisplayMessage($"  Expires: {sessionInfo.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
            _consoleHelper.DisplayMessage($"  Embeddings: {(sessionInfo.EmbeddingsGenerated ? "Generated" : "Not Generated")}");
            _consoleHelper.DisplayMessage();

            // Display processing statistics
            _consoleHelper.DisplayMessage("Processing Statistics:");
            _consoleHelper.DisplayMessage($"  Success: {sessionInfo.ProcessingStats.Success}");
            _consoleHelper.DisplayMessage($"  Total Time: {sessionInfo.ProcessingStats.TotalProcessingTime.TotalMilliseconds:F0}ms");
            if (!string.IsNullOrEmpty(sessionInfo.ProcessingStats.ErrorMessage))
            {
                _consoleHelper.DisplayMessage($"  Error: {sessionInfo.ProcessingStats.ErrorMessage}");
            }
            _consoleHelper.DisplayMessage();

            // Display query statistics
            _consoleHelper.DisplayMessage("Query Statistics:");
            _consoleHelper.DisplayMessage($"  Total Queries: {sessionInfo.QueryStats.TotalQueries}");
            _consoleHelper.DisplayMessage($"  Average Response Time: {sessionInfo.QueryStats.AverageResponseTime.TotalMilliseconds:F0}ms");
            _consoleHelper.DisplayMessage($"  Total Tokens Used: {sessionInfo.QueryStats.TotalTokensUsed:N0}");
            if (sessionInfo.QueryStats.LastQueryAt.HasValue)
            {
                _consoleHelper.DisplayMessage($"  Last Query: {sessionInfo.QueryStats.LastQueryAt:yyyy-MM-dd HH:mm:ss}");
            }
            _consoleHelper.DisplayMessage();

            // Display session status
            var timeUntilExpiration = sessionInfo.ExpiresAt - DateTime.UtcNow;
            if (timeUntilExpiration > TimeSpan.Zero)
            {
                _consoleHelper.DisplayMessage($"Session Status: Active (expires in {timeUntilExpiration:hh\\:mm\\:ss})", ConsoleColor.Green);
            }
            else
            {
                _consoleHelper.DisplayMessage("Session Status: Expired", ConsoleColor.Red);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing session details: {SessionId}", sessionId);
            _consoleHelper.DisplayError($"Failed to show session details: {ex.Message}");
        }
    }
}
