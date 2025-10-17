using Microsoft.Extensions.Logging;
using PdfKnowledgeBase.Console.Helpers;

namespace PdfKnowledgeBase.Console.Services;

/// <summary>
/// Service for handling PDF file selection and validation.
/// </summary>
public class PdfUploader
{
    private readonly ILogger<PdfUploader> _logger;
    private readonly ConsoleHelper _consoleHelper;
    
    public string? SelectedFileName { get; private set; }
    private Stream? _currentFileStream;

    public PdfUploader(ILogger<PdfUploader> logger, ConsoleHelper consoleHelper)
    {
        _logger = logger;
        _consoleHelper = consoleHelper;
    }

    /// <summary>
    /// Selects and validates a PDF file from the user.
    /// </summary>
    public async Task<Stream?> SelectAndValidatePdfAsync()
    {
        try
        {
            _consoleHelper.DisplayMessage("PDF File Selection");
            _consoleHelper.DisplayMessage("Enter the full path to your PDF file:");

            while (true)
            {
                var filePath = _consoleHelper.GetStringInput("PDF file path: ");
                
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    _consoleHelper.DisplayError("Please enter a valid file path.");
                    continue;
                }

                // Expand environment variables and resolve relative paths
                filePath = Environment.ExpandEnvironmentVariables(filePath);
                if (!Path.IsPathRooted(filePath))
                {
                    filePath = Path.GetFullPath(filePath);
                }

                if (!File.Exists(filePath))
                {
                    _consoleHelper.DisplayError($"File not found: {filePath}");
                    continue;
                }

                var fileInfo = new FileInfo(filePath);
                
                // Check file extension
                if (!fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    _consoleHelper.DisplayError("Please select a PDF file (.pdf extension required).");
                    continue;
                }

                // Check file size (50MB limit)
                const long maxSizeBytes = 50 * 1024 * 1024;
                if (fileInfo.Length > maxSizeBytes)
                {
                    _consoleHelper.DisplayError($"File size ({fileInfo.Length / (1024 * 1024):N0} MB) exceeds the maximum allowed size (50 MB).");
                    continue;
                }

                // Validate PDF file
                if (!await ValidatePdfFileAsync(filePath))
                {
                    _consoleHelper.DisplayError("The selected file is not a valid PDF or is corrupted.");
                    continue;
                }

                // Open file stream
                try
                {
                    _currentFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    SelectedFileName = fileInfo.Name;
                    
                    _consoleHelper.DisplaySuccess($"PDF file selected: {SelectedFileName}");
                    _consoleHelper.DisplayMessage($"File size: {fileInfo.Length:N0} bytes");
                    _consoleHelper.DisplayMessage($"Last modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    
                    return _currentFileStream;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error opening PDF file: {FilePath}", filePath);
                    _consoleHelper.DisplayError($"Error opening file: {ex.Message}");
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PDF file selection");
            _consoleHelper.DisplayError($"Error selecting PDF file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Validates that the file is a valid PDF.
    /// </summary>
    private async Task<bool> ValidatePdfFileAsync(string filePath)
    {
        try
        {
            // Basic PDF validation - check for PDF header
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var buffer = new byte[8];
            await fileStream.ReadAsync(buffer, 0, 8);
            
            var header = System.Text.Encoding.ASCII.GetString(buffer);
            return header.StartsWith("%PDF-");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating PDF file: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Cleans up resources.
    /// </summary>
    public void Cleanup()
    {
        try
        {
            _currentFileStream?.Dispose();
            _currentFileStream = null;
            SelectedFileName = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cleanup");
        }
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        Cleanup();
    }
}
