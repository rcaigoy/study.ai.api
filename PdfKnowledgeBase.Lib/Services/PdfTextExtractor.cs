using Microsoft.Extensions.Logging;
using PdfKnowledgeBase.Lib.Interfaces;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PdfKnowledgeBase.Lib.Services;

/// <summary>
/// Service for extracting text from PDF documents using PdfPig.
/// </summary>
public class PdfTextExtractor : IPdfTextExtractor
{
    private readonly ILogger<PdfTextExtractor> _logger;

    public PdfTextExtractor(ILogger<PdfTextExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts text from a PDF file stream.
    /// </summary>
    public async Task<PdfExtractionResult> ExtractTextAsync(Stream fileStream, string fileName)
    {
        var settings = new PdfExtractionSettings();
        return await ExtractTextAsync(fileStream, fileName, settings);
    }

    /// <summary>
    /// Extracts text from a PDF file with custom settings.
    /// </summary>
    public async Task<PdfExtractionResult> ExtractTextAsync(Stream fileStream, string fileName, PdfExtractionSettings settings)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new PdfExtractionResult();

        try
        {
            _logger.LogInformation("Starting PDF text extraction for file: {FileName}", fileName);

            // Validate file size
            if (settings.MaxFileSizeBytes > 0 && fileStream.Length > settings.MaxFileSizeBytes)
            {
                result.ErrorMessage = $"File size ({fileStream.Length} bytes) exceeds maximum allowed size ({settings.MaxFileSizeBytes} bytes)";
                result.Success = false;
                return result;
            }

            // Validate PDF
            if (!await ValidatePdfAsync(fileStream))
            {
                result.ErrorMessage = "Invalid PDF file";
                result.Success = false;
                return result;
            }

            // Reset stream position
            fileStream.Position = 0;

            // Extract text and metadata
            var (extractedText, pages, metadata) = await ExtractTextAndMetadataAsync(fileStream, settings);

            result.Text = extractedText;
            result.Pages = pages;
            result.PageCount = metadata.PageCount;
            result.Metadata = metadata;
            result.Success = true;

            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;

            _logger.LogInformation("PDF text extraction completed for file: {FileName}. Pages: {PageCount}, Characters: {CharacterCount}, Time: {ProcessingTime}ms",
                fileName, result.PageCount, result.CharacterCount, result.ProcessingTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {FileName}", fileName);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = stopwatch.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// Validates if the file is a valid PDF.
    /// </summary>
    public async Task<bool> ValidatePdfAsync(Stream fileStream)
    {
        try
        {
            var originalPosition = fileStream.Position;
            fileStream.Position = 0;

            // Check PDF header
            var buffer = new byte[8];
            await fileStream.ReadAsync(buffer, 0, 8);
            fileStream.Position = originalPosition;

            var header = System.Text.Encoding.ASCII.GetString(buffer);
            return header.StartsWith("%PDF-");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate PDF file");
            return false;
        }
    }

    private async Task<(string text, Dictionary<int, string> pages, PdfMetadata metadata)> ExtractTextAndMetadataAsync(
        Stream fileStream, PdfExtractionSettings settings)
    {
        var text = new StringBuilder();
        var pages = new Dictionary<int, string>();
        var metadata = new PdfMetadata();

        using var document = PdfDocument.Open(fileStream);
        
        try
        {
            // Extract metadata
            metadata = await ExtractMetadataAsync(document);
            metadata.PageCount = document.NumberOfPages;

            var maxPages = settings.MaxPages > 0 ? Math.Min(settings.MaxPages, document.NumberOfPages) : document.NumberOfPages;

            // Extract text from each page
            for (int pageNum = 1; pageNum <= maxPages; pageNum++)
            {
                var page = document.GetPage(pageNum);
                var pageText = ExtractTextFromPage(page, settings);
                
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    pages[pageNum] = pageText;
                    text.AppendLine(pageText);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text and metadata from PDF");
            throw;
        }

        return (text.ToString(), pages, metadata);
    }

    private async Task<PdfMetadata> ExtractMetadataAsync(PdfDocument document)
    {
        var metadata = new PdfMetadata();

        try
        {
            var information = document.Information;
            if (information != null)
            {
                metadata.Title = information.Title;
                metadata.Author = information.Author;
                metadata.Subject = information.Subject;
                metadata.Creator = information.Creator;
                metadata.Producer = information.Producer;
                // PdfPig date handling - skip for now as the API is inconsistent
                // metadata.CreationDate = information.CreationDate;
                // metadata.ModificationDate = information.ModificationDate;
            }

            metadata.IsEncrypted = document.IsEncrypted;
            metadata.PdfVersion = document.Version.ToString();
            metadata.PageCount = document.NumberOfPages;

            await Task.CompletedTask; // Make method async for consistency
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract PDF metadata");
        }

        return metadata;
    }

    private string ExtractTextFromPage(Page page, PdfExtractionSettings settings)
    {
        try
        {
            // Extract text from the page using PdfPig
            var text = page.Text;

            // Clean up the text
            text = CleanExtractedText(text);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract text from page");
            return string.Empty;
        }
    }

    private string CleanExtractedText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Remove excessive whitespace and normalize line breaks
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n", "\n\n");
        
        return text.Trim();
    }


}
