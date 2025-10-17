using Microsoft.Extensions.Logging;
using PdfKnowledgeBase.Console.Helpers;
using PdfKnowledgeBase.Lib.DTOs;
using PdfKnowledgeBase.Lib.Interfaces;

namespace PdfKnowledgeBase.Console.Services;

/// <summary>
/// Service for interactive querying of the knowledge base.
/// </summary>
public class QueryInterface
{
    private readonly ILogger<QueryInterface> _logger;
    private readonly ITemporaryKnowledgeService _knowledgeService;
    private readonly ConsoleHelper _consoleHelper;
    private readonly List<string> _queryHistory = new();

    public QueryInterface(
        ILogger<QueryInterface> logger,
        ITemporaryKnowledgeService knowledgeService,
        ConsoleHelper consoleHelper)
    {
        _logger = logger;
        _knowledgeService = knowledgeService;
        _consoleHelper = consoleHelper;
    }

    /// <summary>
    /// Runs the interactive query interface.
    /// </summary>
    public async Task RunInteractiveQueryAsync(string sessionId)
    {
        _consoleHelper.DisplayMessage("=== Interactive Query Interface ===");
        _consoleHelper.DisplayMessage("Ask questions about your PDF document. Type 'exit' to return to main menu.");
        _consoleHelper.DisplayMessage("Type 'history' to see previous queries, 'help' for more options.");
        _consoleHelper.DisplayMessage();

        bool continueQuerying = true;
        while (continueQuerying)
        {
            try
            {
                var question = _consoleHelper.GetStringInput("🤔 Your question: ");
                
                if (string.IsNullOrWhiteSpace(question))
                {
                    continue;
                }

                switch (question.ToLowerInvariant())
                {
                    case "exit":
                    case "quit":
                        continueQuerying = false;
                        break;
                    case "history":
                        ShowQueryHistory();
                        break;
                    case "help":
                        ShowQueryHelp();
                        break;
                    case "clear":
                        _consoleHelper.ClearScreen();
                        _consoleHelper.DisplayMessage("=== Interactive Query Interface ===");
                        break;
                    default:
                        await ProcessQueryAsync(sessionId, question);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in interactive query interface");
                _consoleHelper.DisplayError($"An error occurred: {ex.Message}");
            }
        }

        _consoleHelper.DisplayMessage("Returning to main menu...");
    }

    /// <summary>
    /// Processes a single query.
    /// </summary>
    private async Task ProcessQueryAsync(string sessionId, string question)
    {
        try
        {
            // Add to history
            _queryHistory.Add(question);

            _consoleHelper.DisplayMessage("🔍 Searching for relevant information...");
            _consoleHelper.ShowProgressIndicator();

            var request = new DocumentQueryRequest
            {
                Question = question,
                SessionId = sessionId,
                MaxChunks = 3,
                Temperature = 0.2
            };

            var response = await _knowledgeService.QueryTemporaryKnowledgeAsync(sessionId, request);

            _consoleHelper.HideProgressIndicator();

            if (response.Success)
            {
                _consoleHelper.DisplayMessage("💡 Answer:", ConsoleColor.Green);
                _consoleHelper.DisplayWrappedText(response.Answer, 80, ConsoleColor.Green);
                
                if (response.RelevantChunks.Any())
                {
                    _consoleHelper.DisplayMessage("\n📄 Relevant sections:", ConsoleColor.Cyan);
                    foreach (var (chunk, index) in response.RelevantChunks.Select((c, i) => (c, i + 1)))
                    {
                        _consoleHelper.DisplayMessage($"\n--- Section {index} (Page {chunk.PageNumber}) ---", ConsoleColor.Yellow);
                        if (!string.IsNullOrEmpty(chunk.Chapter))
                        {
                            _consoleHelper.DisplayMessage($"Chapter: {chunk.Chapter}", ConsoleColor.Yellow);
                        }
                        _consoleHelper.DisplayWrappedText(chunk.Text, 80, ConsoleColor.White);
                        
                        if (chunk.SimilarityScore > 0)
                        {
                            _consoleHelper.DisplayMessage($"Relevance: {(chunk.SimilarityScore * 100):F1}%", ConsoleColor.Gray);
                        }
                    }
                }

                if (response.QueryMetadata != null)
                {
                    _consoleHelper.DisplayMessage($"\n📊 Query processed in {response.QueryMetadata.ProcessingTimeMs:F0}ms", ConsoleColor.Gray);
                    if (response.QueryMetadata.TokensUsed > 0)
                    {
                        _consoleHelper.DisplayMessage($"Tokens used: {response.QueryMetadata.TokensUsed:N0}", ConsoleColor.Gray);
                    }
                }
            }
            else
            {
                _consoleHelper.DisplayError($"Query failed: {response.ErrorMessage}");
            }

            _consoleHelper.DisplayMessage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query: {Question}", question);
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to process query: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the query history.
    /// </summary>
    private void ShowQueryHistory()
    {
        _consoleHelper.DisplayMessage("=== Query History ===");
        
        if (_queryHistory.Count == 0)
        {
            _consoleHelper.DisplayMessage("No queries in history yet.", ConsoleColor.Gray);
        }
        else
        {
            _consoleHelper.DisplayNumberedList(_queryHistory, "Previous queries:");
        }
        
        _consoleHelper.DisplayMessage();
    }

    /// <summary>
    /// Shows help for the query interface.
    /// </summary>
    private void ShowQueryHelp()
    {
        _consoleHelper.DisplayMessage("=== Query Help ===");
        _consoleHelper.DisplayMessage("You can ask various types of questions about your PDF document:");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("• Specific questions: 'What is the main topic of chapter 3?'");
        _consoleHelper.DisplayMessage("• Summary requests: 'Summarize the key points in the document'");
        _consoleHelper.DisplayMessage("• Definition queries: 'What does [term] mean according to this document?'");
        _consoleHelper.DisplayMessage("• Comparison questions: 'How do the authors compare X and Y?'");
        _consoleHelper.DisplayMessage("• Detail requests: 'Explain the methodology used in this study'");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("Special commands:");
        _consoleHelper.DisplayMessage("• 'history' - Show previous queries");
        _consoleHelper.DisplayMessage("• 'help' - Show this help");
        _consoleHelper.DisplayMessage("• 'clear' - Clear the screen");
        _consoleHelper.DisplayMessage("• 'exit' - Return to main menu");
        _consoleHelper.DisplayMessage();
        _consoleHelper.DisplayMessage("Tips for better results:");
        _consoleHelper.DisplayMessage("• Be specific and clear in your questions");
        _consoleHelper.DisplayMessage("• Reference specific chapters or sections when possible");
        _consoleHelper.DisplayMessage("• Ask follow-up questions for more details");
        _consoleHelper.DisplayMessage();
    }

    /// <summary>
    /// Gets the query history.
    /// </summary>
    public IReadOnlyList<string> GetQueryHistory()
    {
        return _queryHistory.AsReadOnly();
    }

    /// <summary>
    /// Clears the query history.
    /// </summary>
    public void ClearHistory()
    {
        _queryHistory.Clear();
    }
}
