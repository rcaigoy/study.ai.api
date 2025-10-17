using Microsoft.Extensions.Logging;
using PdfKnowledgeBase.Console.Helpers;
using PdfKnowledgeBase.Console.Services;
using PdfKnowledgeBase.Lib.DTOs;
using PdfKnowledgeBase.Lib.Interfaces;
using study.ai.api.Models;

namespace PdfKnowledgeBase.Console.Services;

/// <summary>
/// Main service for testing PDF knowledge base functionality.
/// </summary>
public class KnowledgeBaseTester
{
    private readonly ILogger<KnowledgeBaseTester> _logger;
    private readonly ITemporaryKnowledgeService _knowledgeService;
    private readonly PdfUploader _pdfUploader;
    private readonly QueryInterface _queryInterface;
    private readonly SessionManager _sessionManager;
    private readonly ConfigurationHelper _configHelper;
    private readonly ConsoleHelper _consoleHelper;
    private readonly IChatGptService _chatGptService;

    private string? _currentSessionId;
    private const string LAW_PDF_PATH = @"C:\Users\ryanc\Downloads\Advanced Business Law and the Legal Environment.pdf";

    public KnowledgeBaseTester(
        ILogger<KnowledgeBaseTester> logger,
        ITemporaryKnowledgeService knowledgeService,
        PdfUploader pdfUploader,
        QueryInterface queryInterface,
        SessionManager sessionManager,
        ConfigurationHelper configHelper,
        ConsoleHelper consoleHelper,
        IChatGptService chatGptService)
    {
        _logger = logger;
        _knowledgeService = knowledgeService;
        _pdfUploader = pdfUploader;
        _queryInterface = queryInterface;
        _sessionManager = sessionManager;
        _configHelper = configHelper;
        _consoleHelper = consoleHelper;
        _chatGptService = chatGptService;
    }

    /// <summary>
    /// Runs the main application loop.
    /// </summary>
    public async Task RunAsync()
    {
        try
        {
            // Verify ChatGPT is configured and working
            await VerifyChatGptConfigurationAsync();
            
            // Ask if user wants to load the law PDF
            await OfferToLoadLawPdfAsync();

            // Main application loop
            bool continueRunning = true;
            while (continueRunning)
            
            {
                try
                {
                    _consoleHelper.ClearScreen();
                    _consoleHelper.DisplayHeader();
                    _consoleHelper.DisplayCurrentSession(_currentSessionId);

                    var choice = _consoleHelper.DisplayMainMenu();
                    
                    switch (choice)
                    {
                        case "1":
                            await LoadPdfAsync();
                            break;
                        case "2":
                            await QueryKnowledgeBaseAsync();
                            break;
                        case "3":
                            await GenerateQuizAsync();
                            break;
                        case "4":
                            await GenerateFlashcardsAsync();
                            break;
                        case "5":
                            await QueryByChapterAsync();
                            break;
                        case "6":
                            await SummarizeChapterAsync();
                            break;
                        case "7":
                            await ManageSessionAsync();
                            break;
                        case "8":
                            await ShowSessionInfoAsync();
                            break;
                        case "9":
                            await ShowHelpAsync();
                            break;
                        case "a":
                        case "A":
                            await AdvancedChatGptFeaturesAsync();
                            break;
                        case "0":
                            continueRunning = false;
                            break;
                        default:
                            _consoleHelper.DisplayError("Invalid choice. Please try again.");
                            await _consoleHelper.WaitForKeyPressAsync();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in main application loop");
                    _consoleHelper.DisplayError($"An error occurred: {ex.Message}");
                    await _consoleHelper.WaitForKeyPressAsync();
                }
            }

            _consoleHelper.DisplayMessage("Thank you for using the PDF Knowledge Base Console Application!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in KnowledgeBaseTester");
            throw;
        }
    }

    /// <summary>
    /// Verifies ChatGPT configuration and tests connectivity.
    /// </summary>
    private async Task VerifyChatGptConfigurationAsync()
    {
        _consoleHelper.DisplayMessage("🔧 Verifying ChatGPT Configuration...");
        
        // Debug: Show API key info (first few characters only)
        var apiKey = PrivateValues.ChatGPTApiKey;
        var apiKeyPreview = string.IsNullOrEmpty(apiKey) ? "NULL" : $"{apiKey.Substring(0, Math.Min(10, apiKey.Length))}...";
        _consoleHelper.DisplayMessage($"API Key Preview: {apiKeyPreview}");
        
        _consoleHelper.ShowProgressIndicator();

        try
        {
            var isConfigured = await _chatGptService.IsConfiguredAsync();
            
            _consoleHelper.HideProgressIndicator();
            
            if (!isConfigured)
            {
                _consoleHelper.DisplayError("❌ ChatGPT API key not configured!");
                _consoleHelper.DisplayMessage("Please check your PrivateValues.cs file and ensure the ChatGPTApiKey is set.");
                _consoleHelper.DisplayMessage($"API Key Length: {apiKey?.Length ?? 0}");
                await _consoleHelper.WaitForKeyPressAsync();
                return;
            }

            _consoleHelper.DisplaySuccess("✅ ChatGPT API key is configured!");
            
            // Test ChatGPT with a simple request
            _consoleHelper.DisplayMessage("🧪 Testing ChatGPT connectivity...");
            _consoleHelper.ShowProgressIndicator();
            
            var testRequest = new ChatGptRequestDto
            {
                Message = "Hello! Please respond with 'ChatGPT is working correctly.'",
                SystemPrompt = "You are a helpful assistant. Respond briefly and accurately.",
                Temperature = 0.1,
                MaxTokens = 50
            };

            var testResponse = await _chatGptService.SendMessageAsync(testRequest);
            
            _consoleHelper.HideProgressIndicator();

            if (testResponse.Success && testResponse.Response.Contains("working correctly"))
            {
                _consoleHelper.DisplaySuccess("✅ ChatGPT is configured and working!");
                _consoleHelper.DisplayMessage($"Model: {testResponse.Model}");
                _consoleHelper.DisplayMessage($"Response time: {testResponse.ProcessingTimeMs:F0}ms");
            }
            else
            {
                _consoleHelper.DisplayWarning("⚠️ ChatGPT API key is configured but test failed.");
                _consoleHelper.DisplayMessage($"Response: {testResponse.Response}");
                _consoleHelper.DisplayMessage($"Error: {testResponse.ErrorMessage}");
                _consoleHelper.DisplayMessage($"Success: {testResponse.Success}");
            }

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"❌ Error testing ChatGPT: {ex.Message}");
            _consoleHelper.DisplayMessage($"Stack Trace: {ex.StackTrace}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
    }

    /// <summary>
    /// Offers to load the law PDF automatically.
    /// </summary>
    private async Task OfferToLoadLawPdfAsync()
    {
        _consoleHelper.DisplayMessage("📚 Law PDF Loader");
        _consoleHelper.DisplayMessage($"Found law PDF at: {LAW_PDF_PATH}");
        
        if (!File.Exists(LAW_PDF_PATH))
        {
            _consoleHelper.DisplayWarning($"⚠️ Law PDF not found at the expected location.");
            _consoleHelper.DisplayMessage("You can still load other PDFs using the main menu.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        var loadLawPdf = _consoleHelper.GetBooleanInput("Would you like to load the Advanced Business Law PDF now?", true);
        
        if (loadLawPdf)
        {
            await LoadLawPdfAsync();
        }
    }

    /// <summary>
    /// Loads the law PDF from the specified path.
    /// </summary>
    private async Task LoadLawPdfAsync()
    {
        _consoleHelper.DisplayMessage("📖 Loading Advanced Business Law PDF...");
        _consoleHelper.ShowProgressIndicator();

        try
        {
            using var fileStream = new FileStream(LAW_PDF_PATH, FileMode.Open, FileAccess.Read);
            var fileName = Path.GetFileName(LAW_PDF_PATH);

            _currentSessionId = await _knowledgeService.CreateTemporaryKnowledgeBaseAsync(
                fileStream, 
                fileName, 
                TimeSpan.FromHours(4)); // Longer expiration for testing

            _consoleHelper.HideProgressIndicator();
            
            var sessionInfo = await _knowledgeService.GetSessionInfoAsync(_currentSessionId);
            if (sessionInfo != null)
            {
                _consoleHelper.DisplaySuccess("✅ Advanced Business Law PDF loaded successfully!");
                _consoleHelper.DisplayMessage($"Session ID: {sessionInfo.SessionId}");
                _consoleHelper.DisplayMessage($"Pages: {sessionInfo.PageCount}");
                _consoleHelper.DisplayMessage($"Chunks: {sessionInfo.ChunkCount}");
                _consoleHelper.DisplayMessage($"Expires: {sessionInfo.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
            }

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading law PDF");
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to load law PDF: {ex.Message}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
    }

    private async Task LoadPdfAsync()
    {
        _consoleHelper.DisplayMessage("=== Load PDF Document ===");
        
        try
        {
            var fileStream = await _pdfUploader.SelectAndValidatePdfAsync();
            if (fileStream == null)
            {
                _consoleHelper.DisplayMessage("No PDF file selected.");
                await _consoleHelper.WaitForKeyPressAsync();
                return;
            }

            _consoleHelper.DisplayMessage("Processing PDF document...");
            _consoleHelper.ShowProgressIndicator();

            var sessionId = await _knowledgeService.CreateTemporaryKnowledgeBaseAsync(
                fileStream, 
                _pdfUploader.SelectedFileName!, 
                TimeSpan.FromHours(2));

            _currentSessionId = sessionId;
            _consoleHelper.HideProgressIndicator();
            
            var sessionInfo = await _knowledgeService.GetSessionInfoAsync(sessionId);
            if (sessionInfo != null)
            {
                _consoleHelper.DisplayMessage($"✅ PDF loaded successfully!");
                _consoleHelper.DisplayMessage($"Session ID: {sessionInfo.SessionId}");
                _consoleHelper.DisplayMessage($"File: {sessionInfo.FileName}");
                _consoleHelper.DisplayMessage($"Pages: {sessionInfo.PageCount}");
                _consoleHelper.DisplayMessage($"Chunks: {sessionInfo.ChunkCount}");
                _consoleHelper.DisplayMessage($"Expires: {sessionInfo.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
            }

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading PDF");
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to load PDF: {ex.Message}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
        finally
        {
            _pdfUploader.Cleanup();
        }
    }

    private async Task QueryKnowledgeBaseAsync()
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            _consoleHelper.DisplayError("No PDF document loaded. Please load a PDF first.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        await _queryInterface.RunInteractiveQueryAsync(_currentSessionId);
    }

    private async Task GenerateQuizAsync()
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            _consoleHelper.DisplayError("No PDF document loaded. Please load a PDF first.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        _consoleHelper.DisplayMessage("=== Generate Quiz ===");
        
        try
        {
            var questionCount = _consoleHelper.GetIntegerInput("Enter number of questions (default: 5): ", 5);
            var difficulty = _consoleHelper.GetStringInput("Enter difficulty (easy/medium/hard, default: medium): ", "medium");

            _consoleHelper.DisplayMessage("Generating quiz questions...");
            _consoleHelper.ShowProgressIndicator();

            var questions = await _knowledgeService.GenerateQuizAsync(_currentSessionId, questionCount, difficulty);

            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayMessage($"✅ Generated {questions.Count} quiz questions!");

            foreach (var (question, index) in questions.Select((q, i) => (q, i + 1)))
            {
                _consoleHelper.DisplayMessage($"\n--- Question {index} ---");
                _consoleHelper.DisplayMessage($"Question: {question.Question}");
                _consoleHelper.DisplayMessage($"Difficulty: {question.Difficulty}");
                if (!string.IsNullOrEmpty(question.Chapter))
                    _consoleHelper.DisplayMessage($"Chapter: {question.Chapter}");
                
                foreach (var (option, optIndex) in question.Options.Select((o, i) => (o, (char)('A' + i))))
                {
                    _consoleHelper.DisplayMessage($"  {optIndex}. {option}");
                }
                
                _consoleHelper.DisplayMessage($"Correct Answer: {question.CorrectAnswer}");
                _consoleHelper.DisplayMessage($"Explanation: {question.Explanation}");
            }

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quiz");
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to generate quiz: {ex.Message}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
    }

    private async Task GenerateFlashcardsAsync()
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            _consoleHelper.DisplayError("No PDF document loaded. Please load a PDF first.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        _consoleHelper.DisplayMessage("=== Generate Flashcards ===");
        
        try
        {
            var flashcardCount = _consoleHelper.GetIntegerInput("Enter number of flashcards (default: 10): ", 10);

            _consoleHelper.DisplayMessage("Generating flashcards...");
            _consoleHelper.ShowProgressIndicator();

            var flashcards = await _knowledgeService.GenerateFlashcardsAsync(_currentSessionId, flashcardCount);

            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayMessage($"✅ Generated {flashcards.Count} flashcards!");

            foreach (var (flashcard, index) in flashcards.Select((f, i) => (f, i + 1)))
            {
                _consoleHelper.DisplayMessage($"\n--- Flashcard {index} ---");
                if (!string.IsNullOrEmpty(flashcard.Chapter))
                    _consoleHelper.DisplayMessage($"Chapter: {flashcard.Chapter}");
                
                _consoleHelper.DisplayMessage($"Front: {flashcard.Front}");
                _consoleHelper.DisplayMessage($"Back: {flashcard.Back}");
                
                if (!string.IsNullOrEmpty(flashcard.Context))
                    _consoleHelper.DisplayMessage($"Context: {flashcard.Context}");
            }

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating flashcards");
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to generate flashcards: {ex.Message}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
    }

    private async Task QueryByChapterAsync()
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            _consoleHelper.DisplayError("No PDF document loaded. Please load a PDF first.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        _consoleHelper.DisplayMessage("=== Query by Chapter ===");
        
        try
        {
            var chapter = _consoleHelper.GetStringInput("Enter chapter name or part of it: ");
            if (string.IsNullOrWhiteSpace(chapter))
            {
                _consoleHelper.DisplayMessage("No chapter specified.");
                await _consoleHelper.WaitForKeyPressAsync();
                return;
            }

            _consoleHelper.DisplayMessage($"Searching for chapter: {chapter}");
            _consoleHelper.ShowProgressIndicator();

            var chapterContent = await _knowledgeService.GetChapterContentAsync(_currentSessionId, chapter);

            _consoleHelper.HideProgressIndicator();

            if (chapterContent.Any())
            {
                _consoleHelper.DisplayMessage($"✅ Found {chapterContent.Count} chunks in chapter '{chapter}'!");
                
                foreach (var (chunk, index) in chapterContent.Take(5).Select((c, i) => (c, i + 1)))
                {
                    _consoleHelper.DisplayMessage($"\n--- Chunk {index} (Page {chunk.PageNumber}) ---");
                    var preview = chunk.Text.Length > 200 ? chunk.Text.Substring(0, 200) + "..." : chunk.Text;
                    _consoleHelper.DisplayMessage(preview);
                }

                if (chapterContent.Count > 5)
                {
                    _consoleHelper.DisplayMessage($"\n... and {chapterContent.Count - 5} more chunks.");
                }
            }
            else
            {
                _consoleHelper.DisplayMessage($"No content found for chapter '{chapter}'.");
            }

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying by chapter");
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to query chapter: {ex.Message}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
    }

    private async Task ManageSessionAsync()
    {
        await _sessionManager.RunSessionManagementAsync(_currentSessionId);
    }

    private async Task ShowSessionInfoAsync()
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            _consoleHelper.DisplayError("No PDF document loaded.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        _consoleHelper.DisplayMessage("=== Session Information ===");
        
        try
        {
            var sessionInfo = await _knowledgeService.GetSessionInfoAsync(_currentSessionId);
            if (sessionInfo != null)
            {
                _consoleHelper.DisplayMessage($"Session ID: {sessionInfo.SessionId}");
                _consoleHelper.DisplayMessage($"File Name: {sessionInfo.FileName}");
                _consoleHelper.DisplayMessage($"File Size: {sessionInfo.FileSize:N0} bytes");
                _consoleHelper.DisplayMessage($"Pages: {sessionInfo.PageCount}");
                _consoleHelper.DisplayMessage($"Chunks: {sessionInfo.ChunkCount}");
                _consoleHelper.DisplayMessage($"Expires: {sessionInfo.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
                _consoleHelper.DisplayMessage($"Embeddings Generated: {(sessionInfo.EmbeddingsGenerated ? "Yes" : "No")}");
                
                _consoleHelper.DisplayMessage($"\nProcessing Stats:");
                _consoleHelper.DisplayMessage($"  Success: {sessionInfo.ProcessingStats.Success}");
                _consoleHelper.DisplayMessage($"  Total Time: {sessionInfo.ProcessingStats.TotalProcessingTime.TotalMilliseconds:F0}ms");
                
                _consoleHelper.DisplayMessage($"\nQuery Stats:");
                _consoleHelper.DisplayMessage($"  Total Queries: {sessionInfo.QueryStats.TotalQueries}");
                _consoleHelper.DisplayMessage($"  Average Response Time: {sessionInfo.QueryStats.AverageResponseTime.TotalMilliseconds:F0}ms");
                _consoleHelper.DisplayMessage($"  Total Tokens Used: {sessionInfo.QueryStats.TotalTokensUsed:N0}");
                if (sessionInfo.QueryStats.LastQueryAt.HasValue)
                    _consoleHelper.DisplayMessage($"  Last Query: {sessionInfo.QueryStats.LastQueryAt:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                _consoleHelper.DisplayError("Session not found or expired.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session info");
            _consoleHelper.DisplayError($"Failed to get session info: {ex.Message}");
        }

        await _consoleHelper.WaitForKeyPressAsync();
    }

    private async Task SummarizeChapterAsync()
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            _consoleHelper.DisplayError("No PDF document loaded. Please load a PDF first.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        _consoleHelper.DisplayMessage("=== Summarize Chapter ===");
        
        try
        {
            var chapterName = _consoleHelper.GetStringInput("Enter chapter name or part of it: ");
            if (string.IsNullOrWhiteSpace(chapterName))
            {
                _consoleHelper.DisplayMessage("No chapter specified.");
                await _consoleHelper.WaitForKeyPressAsync();
                return;
            }

            var maxLengthInput = _consoleHelper.GetStringInput("Enter maximum summary length in words (default: 500): ");
            var maxLength = 500;
            if (!string.IsNullOrWhiteSpace(maxLengthInput) && int.TryParse(maxLengthInput, out var parsedLength) && parsedLength > 0)
            {
                maxLength = parsedLength;
            }

            _consoleHelper.DisplayMessage($"Generating summary for chapter: {chapterName}");
            _consoleHelper.DisplayMessage($"Maximum length: {maxLength} words");
            _consoleHelper.ShowProgressIndicator();

            var summary = await _knowledgeService.SummarizeChapterAsync(_currentSessionId, chapterName, maxLength);

            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplaySuccess($"✅ Chapter summary generated!");
            _consoleHelper.DisplayMessage();
            _consoleHelper.DisplayMessage("=== Chapter Summary ===", ConsoleColor.Cyan);
            _consoleHelper.DisplayMessage(summary, ConsoleColor.White);
            _consoleHelper.DisplayMessage();
            _consoleHelper.DisplayMessage($"Summary length: {summary.Split(' ').Length} words", ConsoleColor.Gray);

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chapter summary");
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to generate summary: {ex.Message}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
    }

    private async Task AdvancedChatGptFeaturesAsync()
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            _consoleHelper.DisplayError("No PDF document loaded. Please load a PDF first.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        _consoleHelper.DisplayMessage("=== Advanced ChatGPT Features ===");
        _consoleHelper.DisplayMessage("1. Legal Analysis Mode");
        _consoleHelper.DisplayMessage("2. Case Study Generator");
        _consoleHelper.DisplayMessage("3. Concept Explanation");
        _consoleHelper.DisplayMessage("4. Test ChatGPT Directly");
        _consoleHelper.DisplayMessage("0. Back to Main Menu");
        
        var choice = _consoleHelper.GetStringInput("Select an option (0-4): ");

        switch (choice)
        {
            case "1":
                await LegalAnalysisModeAsync();
                break;
            case "2":
                await CaseStudyGeneratorAsync();
                break;
            case "3":
                await ConceptExplanationAsync();
                break;
            case "4":
                await TestChatGptDirectlyAsync();
                break;
            default:
                _consoleHelper.DisplayMessage("Returning to main menu...");
                break;
        }
    }

    private async Task LegalAnalysisModeAsync()
    {
        _consoleHelper.DisplayMessage("=== Legal Analysis Mode ===");
        _consoleHelper.DisplayMessage("This mode uses ChatGPT to provide legal analysis of concepts from your PDF.");
        
        var question = _consoleHelper.GetStringInput("Enter a legal question or concept to analyze: ");
        if (string.IsNullOrWhiteSpace(question))
        {
            _consoleHelper.DisplayMessage("No question provided.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        _consoleHelper.DisplayMessage("🔍 Performing legal analysis...");
        _consoleHelper.ShowProgressIndicator();

        try
        {
            var request = new DocumentQueryRequest
            {
                Question = question,
                SessionId = _currentSessionId,
                MaxChunks = 5,
                Temperature = 0.3
            };

            var response = await _knowledgeService.QueryTemporaryKnowledgeAsync(_currentSessionId, request);

            _consoleHelper.HideProgressIndicator();

            if (response.Success)
            {
                _consoleHelper.DisplayMessage("⚖️ Legal Analysis:", ConsoleColor.Green);
                _consoleHelper.DisplayWrappedText(response.Answer, 80, ConsoleColor.Green);
                
                if (response.RelevantChunks.Any())
                {
                    _consoleHelper.DisplayMessage("\n📚 Supporting Documentation:", ConsoleColor.Cyan);
                    foreach (var (chunk, index) in response.RelevantChunks.Take(3).Select((c, i) => (c, i + 1)))
                    {
                        _consoleHelper.DisplayMessage($"\n--- Reference {index} (Page {chunk.PageNumber}) ---", ConsoleColor.Yellow);
                        _consoleHelper.DisplayWrappedText(chunk.Text, 80, ConsoleColor.White);
                    }
                }
            }
            else
            {
                _consoleHelper.DisplayError($"Analysis failed: {response.ErrorMessage}");
            }

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in legal analysis mode");
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to perform legal analysis: {ex.Message}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
    }

    private async Task CaseStudyGeneratorAsync()
    {
        _consoleHelper.DisplayMessage("=== Case Study Generator ===");
        _consoleHelper.DisplayMessage("Generate hypothetical case studies based on legal concepts from your PDF.");
        
        var topic = _consoleHelper.GetStringInput("Enter a legal topic or concept: ");
        if (string.IsNullOrWhiteSpace(topic))
        {
            _consoleHelper.DisplayMessage("No topic provided.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        _consoleHelper.DisplayMessage("📝 Generating case study...");
        _consoleHelper.ShowProgressIndicator();

        try
        {
            // Create a custom prompt for case study generation
            var prompt = $@"
Based on the legal concepts in the uploaded document, create a realistic case study involving the topic: {topic}

Please include:
1. A clear scenario description
2. The legal issues involved
3. Key facts and circumstances
4. Potential legal arguments for each side
5. Questions for analysis

Make the case study realistic and relevant to the concepts covered in the document.
";

            var request = new DocumentQueryRequest
            {
                Question = prompt,
                SessionId = _currentSessionId,
                MaxChunks = 5,
                Temperature = 0.7
            };

            var response = await _knowledgeService.QueryTemporaryKnowledgeAsync(_currentSessionId, request);

            _consoleHelper.HideProgressIndicator();

            if (response.Success)
            {
                _consoleHelper.DisplayMessage("📋 Generated Case Study:", ConsoleColor.Green);
                _consoleHelper.DisplayWrappedText(response.Answer, 80, ConsoleColor.Green);
            }
            else
            {
                _consoleHelper.DisplayError($"Case study generation failed: {response.ErrorMessage}");
            }

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating case study");
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to generate case study: {ex.Message}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
    }

    private async Task ConceptExplanationAsync()
    {
        _consoleHelper.DisplayMessage("=== Concept Explanation ===");
        _consoleHelper.DisplayMessage("Get detailed explanations of legal concepts from your PDF.");
        
        var concept = _consoleHelper.GetStringInput("Enter a legal concept to explain: ");
        if (string.IsNullOrWhiteSpace(concept))
        {
            _consoleHelper.DisplayMessage("No concept provided.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        _consoleHelper.DisplayMessage("💡 Generating explanation...");
        _consoleHelper.ShowProgressIndicator();

        try
        {
            var prompt = $@"
Please provide a comprehensive explanation of '{concept}' based on the information in the uploaded document.

Include:
1. Definition and key characteristics
2. Legal principles involved
3. Examples or applications
4. Important distinctions or nuances
5. How it relates to other concepts

Make the explanation clear and educational, suitable for someone learning about this concept.
";

            var request = new DocumentQueryRequest
            {
                Question = prompt,
                SessionId = _currentSessionId,
                MaxChunks = 4,
                Temperature = 0.4
            };

            var response = await _knowledgeService.QueryTemporaryKnowledgeAsync(_currentSessionId, request);

            _consoleHelper.HideProgressIndicator();

            if (response.Success)
            {
                _consoleHelper.DisplayMessage($"📖 Explanation of '{concept}':", ConsoleColor.Green);
                _consoleHelper.DisplayWrappedText(response.Answer, 80, ConsoleColor.Green);
            }
            else
            {
                _consoleHelper.DisplayError($"Explanation failed: {response.ErrorMessage}");
            }

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining concept");
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to explain concept: {ex.Message}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
    }

    private async Task TestChatGptDirectlyAsync()
    {
        _consoleHelper.DisplayMessage("=== Test ChatGPT Directly ===");
        _consoleHelper.DisplayMessage("Send a direct message to ChatGPT without using the PDF context.");
        
        var message = _consoleHelper.GetStringInput("Enter your message to ChatGPT: ");
        if (string.IsNullOrWhiteSpace(message))
        {
            _consoleHelper.DisplayMessage("No message provided.");
            await _consoleHelper.WaitForKeyPressAsync();
            return;
        }

        _consoleHelper.DisplayMessage("🤖 Sending message to ChatGPT...");
        _consoleHelper.ShowProgressIndicator();

        try
        {
            var request = new ChatGptRequestDto
            {
                Message = message,
                SystemPrompt = "You are a helpful assistant with knowledge of business law and legal concepts.",
                Temperature = 0.7,
                MaxTokens = 1000
            };

            var response = await _chatGptService.SendMessageAsync(request);

            _consoleHelper.HideProgressIndicator();

            if (response.Success)
            {
                _consoleHelper.DisplayMessage("🤖 ChatGPT Response:", ConsoleColor.Green);
                _consoleHelper.DisplayWrappedText(response.Response, 80, ConsoleColor.Green);
                _consoleHelper.DisplayMessage($"\nModel: {response.Model} | Tokens: {response.TokensUsed} | Time: {response.ProcessingTimeMs:F0}ms", ConsoleColor.Gray);
            }
            else
            {
                _consoleHelper.DisplayError($"ChatGPT request failed: {response.ErrorMessage}");
            }

            await _consoleHelper.WaitForKeyPressAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing ChatGPT directly");
            _consoleHelper.HideProgressIndicator();
            _consoleHelper.DisplayError($"Failed to test ChatGPT: {ex.Message}");
            await _consoleHelper.WaitForKeyPressAsync();
        }
    }

    private async Task ShowHelpAsync()
    {
        _consoleHelper.DisplayMessage("=== Help ===");
        _consoleHelper.DisplayMessage("This application allows you to:");
        _consoleHelper.DisplayMessage("1. Load PDF documents and create temporary knowledge bases");
        _consoleHelper.DisplayMessage("2. Query the knowledge base with natural language questions");
        _consoleHelper.DisplayMessage("3. Generate quiz questions from the document content");
        _consoleHelper.DisplayMessage("4. Generate flashcards for studying");
        _consoleHelper.DisplayMessage("5. Query content by chapter or section");
        _consoleHelper.DisplayMessage("6. Generate AI-powered chapter summaries");
        _consoleHelper.DisplayMessage("7. Manage active sessions (extend, delete)");
        _consoleHelper.DisplayMessage("8. View detailed session information and statistics");
        _consoleHelper.DisplayMessage("A. Advanced ChatGPT Features for enhanced legal analysis");
        _consoleHelper.DisplayMessage("\nAdvanced ChatGPT Features:");
        _consoleHelper.DisplayMessage("- Legal Analysis Mode: Get legal analysis of concepts from your PDF");
        _consoleHelper.DisplayMessage("- Case Study Generator: Create hypothetical legal scenarios");
        _consoleHelper.DisplayMessage("- Concept Explanation: Detailed explanations of legal terms");
        _consoleHelper.DisplayMessage("- Direct ChatGPT Testing: Send messages directly to ChatGPT");
        _consoleHelper.DisplayMessage("\nConfiguration:");
        _consoleHelper.DisplayMessage("- OpenAI API key is configured via PrivateValues");
        _consoleHelper.DisplayMessage("- ChatGPT is verified to be working on startup");
        _consoleHelper.DisplayMessage("- The application offers to load the law PDF automatically");
        _consoleHelper.DisplayMessage("\nTips:");
        _consoleHelper.DisplayMessage("- The app automatically offers to load the law PDF on startup");
        _consoleHelper.DisplayMessage("- ChatGPT is verified to be working before you start");
        _consoleHelper.DisplayMessage("- Use specific legal terms for better results in advanced features");
        _consoleHelper.DisplayMessage("- Ask follow-up questions for deeper analysis");
        _consoleHelper.DisplayMessage("- Sessions expire after 2 hours by default (4 hours for law PDF)");

        await _consoleHelper.WaitForKeyPressAsync();
    }
}
