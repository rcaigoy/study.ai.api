# PDF Knowledge Base Library and Console Application

This solution provides a comprehensive PDF knowledge base system with two main components:

1. **PdfKnowledgeBase.Lib** - A reusable .NET 8.0 library for PDF text extraction, chunking, embedding generation, and knowledge base management
2. **PdfKnowledgeBase.Console** - An interactive console application for testing and demonstrating the knowledge base functionality

## Features

### Library Features (PdfKnowledgeBase.Lib)
- **Real PDF Text Extraction** using PdfPig library
- **Configurable Text Chunking** with multiple strategies (word-based, sentence-based, paragraph-based, chapter-based)
- **Vector Embeddings** with OpenAI integration and fallback to mock embeddings
- **Session Management** for temporary knowledge bases with configurable expiration
- **ChatGPT Integration** for querying and educational content generation
- **Comprehensive Logging** and error handling
- **Dependency Injection** support with easy configuration

### Console Application Features (PdfKnowledgeBase.Console)
- **Interactive Menu System** with colored output and progress indicators
- **PDF File Upload** with validation and error handling
- **Natural Language Querying** with conversation history
- **Educational Features**:
  - Quiz generation from PDF content
  - Flashcard creation for studying
  - Chapter-based content exploration
- **Session Management**:
  - View session information and statistics
  - Extend session expiration time
  - Delete sessions to free memory
- **Configuration Management** with API key setup wizard
- **Comprehensive Help System**

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- OpenAI API key (optional - works with mock responses)

### Running the Console Application

1. **Clone and build the solution:**
   ```bash
   git clone <repository-url>
   cd study.ai.api
   dotnet build
   ```

2. **Run the console application:**
   ```bash
   dotnet run --project PdfKnowledgeBase.Console
   ```

3. **Configure your OpenAI API key (optional):**
   - The application will prompt you to set up an API key
   - Or add it to `appsettings.json`:
     ```json
     {
       "ChatGpt": {
         "ApiKey": "your-api-key-here"
       }
     }
     ```
   - Or use user secrets:
     ```bash
     dotnet user-secrets set "ChatGpt:ApiKey" "your-api-key-here" --project PdfKnowledgeBase.Console
     ```

4. **Load a PDF document and start querying!**

## Project Structure

```
study.ai.api/
├── PdfKnowledgeBase.Lib/           # Reusable library
│   ├── Services/                   # Core services
│   │   ├── TemporaryKnowledgeService.cs
│   │   ├── PdfTextExtractor.cs
│   │   ├── ChunkingService.cs
│   │   ├── EmbeddingService.cs
│   │   └── ChatGptService.cs
│   ├── Interfaces/                 # Service interfaces
│   ├── Models/                     # Data models
│   ├── DTOs/                       # Data transfer objects
│   └── Extensions/                 # DI extensions
├── PdfKnowledgeBase.Console/       # Console application
│   ├── Services/                   # Console-specific services
│   │   ├── KnowledgeBaseTester.cs
│   │   ├── PdfUploader.cs
│   │   ├── QueryInterface.cs
│   │   ├── SessionManager.cs
│   │   └── ConfigurationHelper.cs
│   ├── Helpers/                    # UI helpers
│   │   └── ConsoleHelper.cs
│   └── appsettings.json           # Configuration
└── study.ai.api/                   # Original API project
```

## Usage Examples

### Using the Library in Your Own Project

1. **Add the library reference:**
   ```bash
   dotnet add reference PdfKnowledgeBase.Lib/PdfKnowledgeBase.Lib.csproj
   ```

2. **Configure services:**
   ```csharp
   services.AddPdfKnowledgeBase(options =>
   {
       options.ChatGptApiKey = "your-api-key";
       options.DefaultSessionExpirationHours = 2;
   });
   ```

3. **Use the services:**
   ```csharp
   public class MyService
   {
       private readonly ITemporaryKnowledgeService _knowledgeService;
       
       public async Task<string> ProcessPdf(Stream pdfStream, string fileName)
       {
           var sessionId = await _knowledgeService.CreateTemporaryKnowledgeBaseAsync(
               pdfStream, fileName, TimeSpan.FromHours(2));
           
           var response = await _knowledgeService.QueryTemporaryKnowledgeAsync(
               sessionId, new DocumentQueryRequest 
               { 
                   Question = "What is the main topic of this document?" 
               });
           
           return response.Answer;
       }
   }
   ```

### Console Application Workflow

1. **Start the application** - You'll see the main menu
2. **Load a PDF** - Select option 1 and provide a PDF file path
3. **Query the document** - Ask questions about the PDF content
4. **Generate educational content** - Create quizzes and flashcards
5. **Manage sessions** - View stats, extend time, or delete sessions

## Configuration

### Library Configuration
The library can be configured through the `PdfKnowledgeBaseOptions` class:

```csharp
services.AddPdfKnowledgeBase(options =>
{
    options.ChatGptApiKey = "your-api-key";
    options.HttpTimeoutSeconds = 30;
    options.DefaultChunkSize = 1000;
    options.DefaultChunkOverlap = 200;
    options.DefaultSessionExpirationHours = 2;
    options.MaxFileSizeMB = 50;
});
```

### Console Application Configuration
Configuration is managed through `appsettings.json`:

```json
{
  "ChatGpt": {
    "ApiKey": "",
    "DefaultModel": "gpt-3.5-turbo",
    "MaxTokens": 1000,
    "Temperature": 0.7,
    "TimeoutSeconds": 30
  },
  "DocumentProcessing": {
    "MaxFileSizeMB": 50,
    "DefaultChunkSize": 1000,
    "DefaultChunkOverlap": 200,
    "DefaultExpirationHours": 2
  }
}
```

## API Reference

### Core Interfaces

#### ITemporaryKnowledgeService
- `CreateTemporaryKnowledgeBaseAsync()` - Create a knowledge base from PDF
- `QueryTemporaryKnowledgeAsync()` - Query the knowledge base
- `ExtendSessionAsync()` - Extend session expiration
- `DeleteSessionAsync()` - Delete a session
- `GenerateQuizAsync()` - Generate quiz questions
- `GenerateFlashcardsAsync()` - Generate flashcards
- `GetChapterContentAsync()` - Get content by chapter

#### IPdfTextExtractor
- `ExtractTextAsync()` - Extract text from PDF files
- `ValidatePdfAsync()` - Validate PDF files

#### IChunkingService
- `ChunkTextAsync()` - Split text into chunks with various strategies

#### IEmbeddingService
- `GenerateEmbeddingAsync()` - Generate vector embeddings
- `CalculateSimilarity()` - Calculate similarity between vectors
- `FindMostSimilar()` - Find most similar chunks

#### IChatGptService
- `SendMessageAsync()` - Send messages to ChatGPT
- `GenerateEmbeddingAsync()` - Generate embeddings
- `IsConfiguredAsync()` - Check if API key is configured

## Educational Features

### Quiz Generation
The system can generate multiple-choice quiz questions from PDF content:
- Configurable difficulty levels (easy/medium/hard)
- Chapter-specific questions
- Detailed explanations for correct answers

### Flashcard Creation
Generate study flashcards with:
- Term/definition pairs
- Context and examples
- Chapter associations

### Chapter-Based Queries
Explore specific sections of documents:
- Find content by chapter name
- Browse chapter contents
- Query within specific sections

## Error Handling and Logging

The library includes comprehensive error handling:
- PDF validation and extraction errors
- API connectivity issues
- Configuration problems
- Memory management

Logging is integrated throughout with different levels:
- Information for normal operations
- Warnings for recoverable issues
- Errors for failures

## Performance Considerations

- **Memory Management**: Sessions are stored in memory cache with configurable expiration
- **Chunking**: Text is chunked for efficient processing and retrieval
- **Embeddings**: Vector embeddings enable semantic search
- **Caching**: HTTP clients and responses are cached appropriately

## Limitations

- **Memory Storage**: Sessions are stored in memory and lost on restart
- **File Size**: Limited to 50MB PDF files by default
- **API Dependencies**: Requires OpenAI API key for full functionality
- **Single User**: Console application is single-user oriented

## Future Enhancements

- Persistent storage options (database, file system)
- Multi-user support
- Batch processing capabilities
- Additional PDF processing libraries
- Web-based interface
- API endpoints for integration

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For questions, issues, or contributions, please:
1. Check the existing issues
2. Create a new issue with detailed information
3. Provide sample code and error messages when reporting bugs