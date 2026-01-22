# PDF Upload Feature Migration Summary

## Overview
Successfully migrated the PDF knowledge base functionality from the console application to the main API with new endpoints for PDF-based multiple choice test generation.

## What Was Done

### 1. Project Updates
- **Upgraded study.ai.api to .NET 8.0** (from .NET 6.0) for compatibility with PdfKnowledgeBase.Lib
- **Added project reference** from study.ai.api to PdfKnowledgeBase.Lib
- **Removed circular dependency** by eliminating PrivateValues reference from PdfKnowledgeBase.Lib

### 2. Code Changes

#### a. PrivateValues.cs
Added new security header configuration:
```csharp
public static string HeaderSecurityStart = "IHaveAskedRyan";
```

#### b. Startup.cs
Registered PDF Knowledge Base services with configuration:
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

#### c. MCTestDataController.cs
Added new PDF upload endpoint and supporting methods:

**New Endpoint:**
- `POST /MCTestData/upload-pdf`
  - Accepts: PDF file via multipart/form-data
  - Security: Header-based authentication
  - Returns: MCTestData with 5 multiple choice questions

**Features:**
- Header-based security validation (Authorization must start with "IHaveAskedRyan")
- PDF MIME type and file extension validation
- Temporary knowledge base creation from PDF
- AI-powered test generation using document content
- Automatic cleanup after test generation

#### d. PdfKnowledgeBase.Lib/Services/ChatGptService.cs
Updated to remove dependency on PrivateValues and rely solely on HttpClient configuration:
- Removed all direct references to PrivateValues
- Now uses API key configured through ServiceCollectionExtensions
- Maintains compatibility with existing functionality

#### e. PdfKnowledgeBase.Lib/PdfKnowledgeBase.Lib.csproj
Removed circular project reference to study.ai.api

## New API Endpoint

### POST /MCTestData/upload-pdf

**Request:**
- Header: `Authorization: IHaveAskedRyan-<optional-guid>`
- Body: multipart/form-data with `pdfFile` field
- File must be valid PDF (MIME type: application/pdf, extension: .pdf)

**Response:**
```json
{
  "questions": [
    {
      "questionText": "Question text",
      "options": [
        {
          "option": "A",
          "text": "Option text",
          "description": "Option description"
        }
      ],
      "correctAnswer": "A",
      "answerDescription": "Explanation"
    }
  ]
}
```

**Error Responses:**
- 401: Missing or invalid authorization header
- 400: Invalid file type, missing file, or bad request
- 500: Processing error

## Security Features
1. **Header-based Authentication**: Simple password protection via header prefix
2. **MIME Type Validation**: Ensures only PDF files are accepted
3. **File Extension Validation**: Double-checks file extension
4. **Temporary Session Management**: Automatically cleans up after processing

## Memory Management
- Uses in-memory caching for temporary PDF processing
- Session expiration: 2 hours (configurable)
- Max file size: 50 MB (configurable)
- Automatic cleanup after test generation
- Designed for small user base to prevent memory overload

## Documentation Created
- `study.ai.api/Controllers/mcTestData/README_PDF_UPLOAD.md` - Comprehensive API documentation with examples

## Build Status
✅ **Build Successful**
- No compilation errors
- 106 warnings (mostly XML documentation and nullable references - non-critical)
- All dependencies resolved correctly

## How to Use

### Example cURL Request
```bash
curl -X POST "https://your-api.com/MCTestData/upload-pdf" \
  -H "Authorization: IHaveAskedRyan-550e8400-e29b-41d4-a716-446655440000" \
  -F "pdfFile=@/path/to/document.pdf"
```

### Example JavaScript
```javascript
const formData = new FormData();
formData.append('pdfFile', pdfFileInput.files[0]);

fetch('https://your-api.com/MCTestData/upload-pdf', {
  method: 'POST',
  headers: {
    'Authorization': 'IHaveAskedRyan-' + crypto.randomUUID()
  },
  body: formData
})
  .then(response => response.json())
  .then(data => console.log('Test generated:', data))
  .catch(error => console.error('Error:', error));
```

## Configuration

### Customization Options in Startup.cs
- `HttpTimeoutSeconds`: Timeout for API calls (default: 60)
- `DefaultSessionExpirationHours`: Session lifetime (default: 2)
- `MaxFileSizeMB`: Maximum PDF size (default: 50)
- `DefaultChunkSize`: Text chunk size (default: 1500)
- `DefaultChunkOverlap`: Chunk overlap (default: 300)

### Security Header
Change in `PrivateValues.cs`:
```csharp
public static string HeaderSecurityStart = "YourCustomPassword";
```

## Processing Flow
1. **Validate Security Header** → Ensure authorization starts with configured prefix
2. **Validate File** → Check MIME type and extension
3. **Extract PDF Text** → Use PdfPig to extract text from all pages
4. **Create Knowledge Base** → Chunk text and generate embeddings
5. **Query for Summary** → Get comprehensive document summary
6. **Generate Test** → Use OpenAI GPT to create 5 multiple choice questions
7. **Cleanup** → Delete temporary session
8. **Return Results** → Send MCTestData JSON response

## Dependencies
- OpenAI API key required (configured in PrivateValues.cs)
- .NET 8.0 SDK
- PdfPig library for PDF processing
- In-memory cache for temporary storage

## Testing Recommendations
1. Test with various PDF types (text-based, different sizes)
2. Test security header validation
3. Test with invalid file types
4. Test with large PDFs (near 50MB limit)
5. Test with malformed PDFs
6. Monitor memory usage under load

## Known Limitations
1. Memory-intensive for large PDFs or many concurrent users
2. Designed for small user base (add rate limiting for production)
3. Scanned PDFs without OCR may not work well
4. Question quality depends on PDF text extraction quality
5. Processing time varies with PDF size (can take several seconds)

## Future Enhancements
- [ ] Configurable question count (not just 5)
- [ ] Difficulty level selection
- [ ] OCR support for scanned PDFs
- [ ] Distributed caching (Redis) for scalability
- [ ] Rate limiting per user/IP
- [ ] Question type variety
- [ ] Batch PDF processing
- [ ] Export to different formats

## Deployment Notes
- Update OpenAI API key in PrivateValues before deployment
- Ensure sufficient memory for PDF processing
- Configure security header password
- Monitor API quota for OpenAI
- Consider adding rate limiting for production
- Update CORS settings if needed for your frontend

## Files Modified
1. `study.ai.api/study.ai.api.csproj`
2. `study.ai.api/Models/PrivateValues.cs`
3. `study.ai.api/Startup.cs`
4. `study.ai.api/Controllers/mcTestData/MCTestDataController.cs`
5. `PdfKnowledgeBase.Lib/Services/ChatGptService.cs`
6. `PdfKnowledgeBase.Lib/PdfKnowledgeBase.Lib.csproj`

## Files Created
1. `study.ai.api/Controllers/mcTestData/README_PDF_UPLOAD.md`
2. `MIGRATION_SUMMARY.md` (this file)

---

**Migration completed successfully!** ✅

The PDF upload feature is now integrated into the API and ready for testing.

