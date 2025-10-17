# PDF Knowledge Base Console Application

A production-ready console application for interactive PDF document analysis and AI-powered knowledge base management.

## 🚀 Features

### Core Functionality
- **PDF Document Loading**: Upload and process PDF documents with real text extraction
- **Interactive Querying**: Natural language questions about document content
- **AI-Powered Summaries**: Generate comprehensive chapter summaries using ChatGPT
- **Educational Tools**: Create quizzes and flashcards from document content
- **Session Management**: Manage temporary knowledge bases with configurable expiration

### Production-Ready Features
- **Structured Logging**: Serilog integration with file and console output
- **Configuration Management**: Environment-specific settings and user secrets
- **Error Handling**: Comprehensive error handling with graceful degradation
- **Health Checks**: Built-in health monitoring capabilities
- **Performance Monitoring**: Session statistics and processing metrics

## 📋 Requirements

- .NET 8.0 Runtime
- OpenAI API Key (configured via PrivateValues)
- Windows/Linux/macOS compatible

## 🛠️ Installation & Setup

### 1. Clone and Build
```bash
git clone <repository-url>
cd study.ai.api/PdfKnowledgeBase.Console
dotnet restore
dotnet build
```

### 2. Configuration
The application uses PrivateValues for API key configuration. Ensure your ChatGPT API key is set in the `study.ai.api.Models.PrivateValues` class.

### 3. Run the Application
```bash
# Development mode
dotnet run

# Production mode
dotnet run --environment Production

# With custom configuration
dotnet run --ChatGpt:TimeoutSeconds=60 --DocumentProcessing:MaxFileSizeMB=100
```

## 📖 Usage

### Main Menu Options

1. **Load PDF Document**: Upload and process a PDF file
2. **Query Knowledge Base**: Ask questions about the loaded document
3. **Generate Quiz Questions**: Create multiple-choice questions from content
4. **Generate Flashcards**: Create study flashcards
5. **Query by Chapter**: Explore specific sections of the document
6. **Summarize Chapter**: Generate AI-powered chapter summaries
7. **Manage Session**: Extend, delete, or view session information
8. **Show Session Information**: View detailed statistics and metrics
9. **Help**: Display usage instructions
0. **Exit**: Close the application

### Example Workflow

1. **Load a PDF**: Select option 1 and provide the path to your PDF file
2. **Ask Questions**: Use option 2 to ask natural language questions
3. **Generate Summary**: Use option 6 to get AI-generated chapter summaries
4. **Create Study Materials**: Use options 3-4 for educational content

## ⚙️ Configuration

### Environment-Specific Settings

#### Development (`appsettings.json`)
```json
{
  "ChatGpt": {
    "TimeoutSeconds": 30,
    "MaxTokens": 1000
  },
  "DocumentProcessing": {
    "MaxFileSizeMB": 10,
    "DefaultExpirationHours": 2
  }
}
```

#### Production (`appsettings.Production.json`)
```json
{
  "ChatGpt": {
    "TimeoutSeconds": 60,
    "MaxTokens": 2000
  },
  "DocumentProcessing": {
    "MaxFileSizeMB": 50,
    "DefaultExpirationHours": 4
  },
  "Application": {
    "LogLevel": "Information",
    "EnableDetailedLogging": false
  }
}
```

### Configuration Sources (Priority Order)
1. Command line arguments
2. Environment variables
3. `appsettings.{Environment}.json`
4. `appsettings.json`
5. User secrets (Development only)

## 📊 Logging

### Log Outputs
- **Console**: Real-time colored output with timestamps
- **File**: Daily rolling logs in `logs/` directory
- **Retention**: 7 days of log files

### Log Levels
- **Information**: General application flow
- **Warning**: Non-critical issues
- **Error**: Application errors with stack traces
- **Fatal**: Critical errors causing application termination

### Log Format
```
[2024-01-15 14:30:25.123 +00:00 INF] Successfully generated summary for chapter 'Introduction'
```

## 🔧 Advanced Features

### Chapter Summarization
- **AI-Powered**: Uses ChatGPT for intelligent summarization
- **Configurable Length**: Specify word count limits
- **Fallback Mode**: Basic summarization when API unavailable
- **Context-Aware**: Maintains document structure and key concepts

### Session Management
- **Automatic Expiration**: Configurable session timeouts
- **Memory Management**: Efficient caching with cleanup
- **Statistics Tracking**: Query counts, response times, token usage
- **Manual Extension**: Extend session lifetime as needed

### Error Handling
- **Graceful Degradation**: Continues operation with reduced functionality
- **User-Friendly Messages**: Clear error descriptions
- **Automatic Recovery**: Retry mechanisms for transient failures
- **Detailed Logging**: Comprehensive error tracking for debugging

## 🏗️ Architecture

### Project Structure
```
PdfKnowledgeBase.Console/
├── Services/              # Core application services
├── Helpers/               # Utility classes
├── logs/                  # Log files (created at runtime)
├── appsettings.json       # Development configuration
├── appsettings.Production.json  # Production configuration
└── Program.cs             # Application entry point
```

### Key Components
- **KnowledgeBaseTester**: Main application orchestrator
- **ConsoleHelper**: User interface utilities
- **PdfUploader**: File handling and validation
- **QueryInterface**: Interactive querying system
- **SessionManager**: Session lifecycle management
- **ConfigurationHelper**: Configuration validation

## 🚀 Production Deployment

### Environment Setup
```bash
# Set environment
export ASPNETCORE_ENVIRONMENT=Production

# Configure logging
export Serilog__MinimumLevel__Default=Information

# Run with production settings
dotnet run --environment Production
```

### Monitoring
- **Health Checks**: Built-in endpoint for monitoring
- **Log Analysis**: Parse structured logs for insights
- **Performance Metrics**: Track session statistics
- **Error Rates**: Monitor failure patterns

### Security Considerations
- **API Key Protection**: Store securely in PrivateValues
- **File Upload Limits**: Configurable size restrictions
- **Session Isolation**: Separate knowledge bases per session
- **Input Validation**: Comprehensive input sanitization

## 🐛 Troubleshooting

### Common Issues

#### "API Key Not Configured"
- Ensure PrivateValues.ChatGPTApiKey is set
- Check configuration source priority

#### "PDF Processing Failed"
- Verify file is a valid PDF
- Check file size limits
- Ensure sufficient memory available

#### "Session Expired"
- Extend session time in configuration
- Use session management to extend active sessions

### Debug Mode
```bash
# Enable detailed logging
dotnet run --environment Development --Logging:LogLevel:Default=Debug
```

## 📈 Performance Optimization

### Memory Management
- **Automatic Cleanup**: Expired sessions removed automatically
- **Chunked Processing**: Large documents processed in chunks
- **Efficient Caching**: Optimized memory usage patterns

### Response Times
- **Parallel Processing**: Concurrent operations where possible
- **Caching Strategy**: Intelligent caching of embeddings and responses
- **Timeout Configuration**: Adjustable timeouts for different operations

## 🤝 Contributing

### Development Setup
1. Clone the repository
2. Install .NET 8.0 SDK
3. Configure API keys in PrivateValues
4. Run tests and build

### Code Standards
- Follow C# naming conventions
- Include XML documentation
- Use structured logging
- Handle exceptions gracefully

## 📄 License

This project is part of the study.ai.api solution. See the main repository for license information.

## 🔗 Related Projects

- **PdfKnowledgeBase.Lib**: Core library for PDF knowledge base functionality
- **study.ai.api**: Main API project with additional AI services

---

**Version**: 1.0.0  
**Last Updated**: 2024-01-15  
**Maintainer**: PDF Knowledge Base Team
