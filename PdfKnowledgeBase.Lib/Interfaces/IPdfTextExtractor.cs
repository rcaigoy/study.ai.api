namespace PdfKnowledgeBase.Lib.Interfaces;

/// <summary>
/// Interface for extracting text from PDF documents.
/// </summary>
public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts text from a PDF file stream.
    /// </summary>
    /// <param name="fileStream">The PDF file stream.</param>
    /// <param name="fileName">The name of the PDF file (for logging purposes).</param>
    /// <returns>The extracted text with page information.</returns>
    Task<PdfExtractionResult> ExtractTextAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Extracts text from a PDF file with custom settings.
    /// </summary>
    /// <param name="fileStream">The PDF file stream.</param>
    /// <param name="fileName">The name of the PDF file.</param>
    /// <param name="settings">Custom extraction settings.</param>
    /// <returns>The extracted text with page information.</returns>
    Task<PdfExtractionResult> ExtractTextAsync(Stream fileStream, string fileName, PdfExtractionSettings settings);

    /// <summary>
    /// Validates if the file is a valid PDF.
    /// </summary>
    /// <param name="fileStream">The file stream to validate.</param>
    /// <returns>True if the file is a valid PDF.</returns>
    Task<bool> ValidatePdfAsync(Stream fileStream);
}

/// <summary>
/// Result of PDF text extraction.
/// </summary>
public class PdfExtractionResult
{
    /// <summary>
    /// The extracted text content.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text organized by page number.
    /// </summary>
    public Dictionary<int, string> Pages { get; set; } = new();

    /// <summary>
    /// The total number of pages in the PDF.
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// Whether the extraction was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if extraction failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The total number of characters extracted.
    /// </summary>
    public int CharacterCount => Text.Length;

    /// <summary>
    /// Processing time for the extraction.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Metadata about the PDF file.
    /// </summary>
    public PdfMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Settings for PDF text extraction.
/// </summary>
public class PdfExtractionSettings
{
    /// <summary>
    /// Whether to preserve formatting and layout.
    /// </summary>
    public bool PreserveFormatting { get; set; } = false;

    /// <summary>
    /// Whether to extract images as base64.
    /// </summary>
    public bool ExtractImages { get; set; } = false;

    /// <summary>
    /// Maximum file size in bytes (0 = no limit).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB

    /// <summary>
    /// Maximum number of pages to extract (0 = all pages).
    /// </summary>
    public int MaxPages { get; set; } = 0;

    /// <summary>
    /// Password for password-protected PDFs.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Whether to extract text from annotations.
    /// </summary>
    public bool ExtractAnnotations { get; set; } = true;
}

/// <summary>
/// Metadata about a PDF file.
/// </summary>
public class PdfMetadata
{
    /// <summary>
    /// The title of the PDF document.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The author of the PDF document.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// The subject of the PDF document.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The creator of the PDF document.
    /// </summary>
    public string? Creator { get; set; }

    /// <summary>
    /// The producer of the PDF document.
    /// </summary>
    public string? Producer { get; set; }

    /// <summary>
    /// The creation date of the PDF document.
    /// </summary>
    public DateTime? CreationDate { get; set; }

    /// <summary>
    /// The modification date of the PDF document.
    /// </summary>
    public DateTime? ModificationDate { get; set; }

    /// <summary>
    /// The number of pages in the PDF.
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// Whether the PDF is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// The PDF version.
    /// </summary>
    public string? PdfVersion { get; set; }
}
