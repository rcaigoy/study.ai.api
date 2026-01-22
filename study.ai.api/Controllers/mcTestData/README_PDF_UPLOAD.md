# PDF Upload and Test Generation API

This document describes the PDF upload feature that generates multiple choice tests from PDF documents.

## Overview

The PDF upload endpoint allows authenticated users to upload PDF files and automatically generate multiple choice tests based on the document content. This feature combines the PDF knowledge base processing from the console application with the multiple choice test generation capabilities.

## API Endpoint

### Upload PDF and Generate Test

**Endpoint:** `POST /MCTestData/upload-pdf`

**Content-Type:** `multipart/form-data`

**Authorization:** Required via `Authorization` header

#### Security

The endpoint is protected by a simple header-based authentication mechanism:

- **Header Name:** `Authorization`
- **Header Value:** Must start with the value configured in `PrivateValues.HeaderSecurityStart` (currently: `"IHaveAskedRyan"`)
- **Example:** `Authorization: IHaveAskedRyan-<optional-session-guid>`

The portion after `"IHaveAskedRyan"` can be a GUID or any identifier for session tracking (though session management is handled internally by the knowledge base service).

#### Request

**Form Data:**
- `pdfFile` (required): The PDF file to process
  - **MIME Type:** Must be `application/pdf`
  - **File Extension:** Must be `.pdf`
  - **Max Size:** 50 MB (configurable in Startup.cs)

#### Response

**Success Response (200 OK):**

```json
{
  "questions": [
    {
      "questionText": "What is the main topic of Chapter 1?",
      "options": [
        {
          "option": "A",
          "text": "Introduction to the subject",
          "description": "This chapter covers the basics..."
        },
        {
          "option": "B",
          "text": "Advanced concepts",
          "description": "This would be covered later..."
        },
        {
          "option": "C",
          "text": "Historical background",
          "description": "While mentioned, this is not the main topic..."
        },
        {
          "option": "D",
          "text": "Practical applications",
          "description": "Applications are discussed in later chapters..."
        }
      ],
      "correctAnswer": "A",
      "answerDescription": "Chapter 1 serves as an introduction to the fundamental concepts of the subject."
    }
    // ... 4 more questions (5 total)
  ]
}
```

**Error Responses:**

- `401 Unauthorized`: Missing or invalid authorization header
  ```json
  "Missing authorization header"
  ```
  or
  ```json
  "Invalid authorization"
  ```

- `400 Bad Request`: Invalid file or missing file
  ```json
  "No PDF file uploaded"
  ```
  or
  ```json
  "Invalid file type. Expected PDF but received: <mime-type>"
  ```
  or
  ```json
  "Invalid file extension. Expected .pdf but received: <extension>"
  ```

- `500 Internal Server Error`: Processing error
  ```json
  "Error processing PDF: <error-message>"
  ```

## How It Works

### Processing Flow

1. **Security Validation**: The endpoint validates the authorization header starts with the configured security prefix.

2. **File Validation**: 
   - Checks if a file was uploaded
   - Validates MIME type is `application/pdf`
   - Validates file extension is `.pdf`

3. **PDF Processing**:
   - Creates a temporary knowledge base from the PDF
   - Extracts text from all pages
   - Chunks the text into manageable pieces
   - Generates embeddings for semantic search (if configured)

4. **Test Generation**:
   - Queries the knowledge base for a comprehensive summary
   - Uses the summary (or direct chunks) to generate test questions
   - Formats the questions according to the `apiTestData.json` example format
   - Generates 5 multiple choice questions with 4 options each

5. **Cleanup**:
   - Deletes the temporary knowledge base session
   - Returns the generated test data

### Memory Management

The feature uses an in-memory cache for temporary PDF processing:
- **Session Expiration**: 2 hours (configurable)
- **Max File Size**: 50 MB (configurable)
- **Automatic Cleanup**: Sessions are deleted after test generation
- **Small User Base**: Designed for limited concurrent users to prevent memory overload

## Usage Examples

### cURL Example

```bash
curl -X POST "https://your-api.com/MCTestData/upload-pdf" \
  -H "Authorization: IHaveAskedRyan-550e8400-e29b-41d4-a716-446655440000" \
  -F "pdfFile=@/path/to/your/document.pdf"
```

### JavaScript/Fetch Example

```javascript
const formData = new FormData();
formData.append('pdfFile', pdfFileInput.files[0]);

fetch('https://your-api.com/MCTestData/upload-pdf', {
  method: 'POST',
  headers: {
    'Authorization': 'IHaveAskedRyan-550e8400-e29b-41d4-a716-446655440000'
  },
  body: formData
})
  .then(response => response.json())
  .then(data => console.log('Test generated:', data))
  .catch(error => console.error('Error:', error));
```

### C# Example

```csharp
using var httpClient = new HttpClient();
using var formData = new MultipartFormDataContent();

var fileContent = new ByteArrayContent(File.ReadAllBytes("document.pdf"));
fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
formData.Add(fileContent, "pdfFile", "document.pdf");

httpClient.DefaultRequestHeaders.Add("Authorization", 
    $"IHaveAskedRyan-{Guid.NewGuid()}");

var response = await httpClient.PostAsync(
    "https://your-api.com/MCTestData/upload-pdf", 
    formData);

if (response.IsSuccessStatusCode)
{
    var testData = await response.Content.ReadFromJsonAsync<MCTestData>();
    // Use test data...
}
```

## Configuration

### Startup.cs Configuration

The PDF Knowledge Base services are registered in `Startup.cs`:

```csharp
services.AddPdfKnowledgeBase(options =>
{
    options.ChatGptApiKey = PrivateValues.ChatGPTApiKey;
    options.HttpTimeoutSeconds = 60;
    options.DefaultSessionExpirationHours = 2;
    options.MaxFileSizeMB = 50;
    options.DefaultChunkSize = 1500;
    options.DefaultChunkOverlap = 300;
});
```

**Configurable Options:**
- `ChatGptApiKey`: OpenAI API key for GPT processing
- `HttpTimeoutSeconds`: Timeout for HTTP requests (default: 60)
- `DefaultSessionExpirationHours`: How long sessions are kept in memory (default: 2)
- `MaxFileSizeMB`: Maximum PDF file size (default: 50)
- `DefaultChunkSize`: Text chunk size for processing (default: 1500)
- `DefaultChunkOverlap`: Overlap between chunks (default: 300)

### Security Configuration

Update the security header prefix in `PrivateValues.cs`:

```csharp
public static class PrivateValues
{
    public static string ChatGPTApiKey = "your-openai-api-key";
    public static string HeaderSecurityStart = "IHaveAskedRyan";
}
```

## Limitations

1. **Memory Usage**: Since PDF content is stored in-memory, large PDFs or many concurrent users may consume significant memory.

2. **User Base**: Designed for a small user base. For larger scale, consider:
   - Implementing rate limiting
   - Using a distributed cache (Redis)
   - Implementing queue-based processing

3. **File Size**: Limited to 50 MB by default to prevent memory issues.

4. **Question Quality**: The quality of generated questions depends on:
   - PDF text extraction quality
   - Document structure and clarity
   - OpenAI GPT model performance

5. **Processing Time**: Large PDFs may take several seconds to process due to:
   - Text extraction
   - Embedding generation
   - GPT API calls for test generation

## Troubleshooting

### "Invalid authorization" Error
- Ensure the `Authorization` header starts with the exact value from `PrivateValues.HeaderSecurityStart`
- Header is case-sensitive

### "Invalid file type" Error
- Ensure the file is actually a PDF
- Check that the MIME type is set correctly when uploading

### "Session not found" Error
- This is an internal error - check server logs
- May indicate memory pressure or session expiration

### Empty or Poor Quality Questions
- Check that the PDF has extractable text (not just images)
- Scanned PDFs may not work well without OCR
- Check OpenAI API key is valid and has quota remaining

### 500 Internal Server Error
- Check server logs for detailed error messages
- Verify OpenAI API key is configured correctly
- Ensure PDF file is not corrupted
- Check memory availability

## Dependencies

### NuGet Packages (Inherited from PdfKnowledgeBase.Lib)
- PdfPig (0.1.8) - PDF text extraction
- Microsoft.Extensions.Caching.Memory (8.0.1) - In-memory caching
- Newtonsoft.Json (13.0.3) - JSON serialization

### Services
- `ITemporaryKnowledgeService` - PDF processing and knowledge base management
- `IChatGptService` - OpenAI GPT integration
- `IPdfTextExtractor` - PDF text extraction
- `IChunkingService` - Text chunking
- `IEmbeddingService` - Text embedding generation

## Migration from Console App

This API endpoint migrates the PDF processing functionality from the `PdfKnowledgeBase.Console` application. Key differences:

1. **Authentication**: Console app had no auth; API uses header-based security
2. **Output Format**: Console app was interactive; API returns structured JSON
3. **Session Management**: Console app had manual session management; API auto-manages sessions
4. **Integration**: API is designed for programmatic access vs. console interactive mode

## Future Enhancements

Potential improvements:
- [ ] Support for different question counts (not just 5)
- [ ] Difficulty level selection
- [ ] Question type variety (true/false, short answer, etc.)
- [ ] Persistent session management for multi-step workflows
- [ ] Batch processing of multiple PDFs
- [ ] OCR support for scanned PDFs
- [ ] Question customization via request parameters
- [ ] Export to different formats (CSV, XML, etc.)
- [ ] Analytics and usage tracking

