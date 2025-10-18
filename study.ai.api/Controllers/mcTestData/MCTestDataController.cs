using Microsoft.AspNetCore.Mvc;
using study.ai.api.Logic.ai;
using study.ai.api.Models.mcTestData;
using study.ai.api.Models;
using PdfKnowledgeBase.Lib.Interfaces;
using PdfKnowledgeBase.Lib.DTOs;
using System.Text.Json;
using Newtonsoft.Json;
using System.Linq;

namespace study.ai.api.Controllers.testData
{
    [ApiController]
    [Route("[controller]")]
    public class MCTestDataController : ControllerBase
    {
        private readonly ITemporaryKnowledgeService _knowledgeService;
        private readonly ILogger<MCTestDataController> _logger;

        public MCTestDataController(
            ITemporaryKnowledgeService knowledgeService,
            ILogger<MCTestDataController> logger)
        {
            _knowledgeService = knowledgeService;
            _logger = logger;
        }

        // POST method to receive and return MCTestData from text description
        [HttpPost]
        public async Task<ActionResult<MCTestData>> PostMCTestData([FromBody] string testDescription)
        {
            if (string.IsNullOrWhiteSpace(testDescription))
            {
                return BadRequest("Test data is null.");
            }

            var gptService = new GPTService(PrivateValues.ChatGPTApiKey);
            var testData = await gptService.GenerateTestJsonAsync(testDescription);

            // Return the processed testData
            return Ok(testData);
        }

        // POST method to upload PDF and generate MCTestData
        [HttpPost("upload-pdf")]
        public async Task<ActionResult<MCTestData>> UploadPdfAndGenerateTest(IFormFile pdfFile)
        {
            try
            {
                // Validate security header
                if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    _logger.LogWarning("PDF upload attempt without Authorization header");
                    return Unauthorized("Missing authorization header");
                }

                var authValue = authHeader.ToString();
                if (!authValue.StartsWith(PrivateValues.HeaderSecurityStart))
                {
                    _logger.LogWarning("PDF upload attempt with invalid authorization header: {Header}", authValue.Substring(0, Math.Min(20, authValue.Length)));
                    return Unauthorized("Invalid authorization");
                }

                // Extract session identifier from header (GUID after the security prefix)
                var sessionPrefix = authValue.Length > PrivateValues.HeaderSecurityStart.Length 
                    ? authValue.Substring(PrivateValues.HeaderSecurityStart.Length)
                    : Guid.NewGuid().ToString();

                // Validate PDF file
                if (pdfFile == null || pdfFile.Length == 0)
                {
                    return BadRequest("No PDF file uploaded");
                }

                // Validate MIME type
                var allowedMimeTypes = new[] { "application/pdf" };
                if (!allowedMimeTypes.Contains(pdfFile.ContentType.ToLower()))
                {
                    _logger.LogWarning("Invalid file type uploaded: {ContentType}", pdfFile.ContentType);
                    return BadRequest($"Invalid file type. Expected PDF but received: {pdfFile.ContentType}");
                }

                // Additional file extension validation
                var fileExtension = Path.GetExtension(pdfFile.FileName).ToLower();
                if (fileExtension != ".pdf")
                {
                    _logger.LogWarning("Invalid file extension: {Extension}", fileExtension);
                    return BadRequest($"Invalid file extension. Expected .pdf but received: {fileExtension}");
                }

                _logger.LogInformation("Processing PDF upload: {FileName}, Size: {Size} bytes, Session: {Session}", 
                    pdfFile.FileName, pdfFile.Length, sessionPrefix);

                // Create temporary knowledge base from PDF
                string sessionId;
                using (var stream = pdfFile.OpenReadStream())
                {
                    sessionId = await _knowledgeService.CreateTemporaryKnowledgeBaseAsync(
                        stream, 
                        pdfFile.FileName, 
                        TimeSpan.FromHours(2));
                }

                _logger.LogInformation("Knowledge base created with session ID: {SessionId}", sessionId);

                // Generate multiple choice test from the PDF content
                var testData = await GenerateMCTestDataFromPdf(sessionId);

                // Clean up the session after generating the test
                await _knowledgeService.DeleteSessionAsync(sessionId);

                _logger.LogInformation("Successfully generated test from PDF: {FileName}", pdfFile.FileName);

                return Ok(testData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PDF upload and generating test");
                return StatusCode(500, $"Error processing PDF: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to generate MCTestData from PDF content using the knowledge base
        /// </summary>
        private async Task<MCTestData> GenerateMCTestDataFromPdf(string sessionId)
        {
            try
            {
                // Get the session info to retrieve document chunks
                var sessionInfo = await _knowledgeService.GetSessionInfoAsync(sessionId);
                if (sessionInfo == null)
                {
                    throw new InvalidOperationException("Session not found");
                }

                // Get the PDF content from the knowledge base
                // We'll query for a summary to get the main content
                var summaryQuery = new DocumentQueryRequest
                {
                    Question = "Please provide a comprehensive summary of the main topics and concepts covered in this document.",
                    MaxChunks = 10,
                    Temperature = 0.3
                };

                var summaryResponse = await _knowledgeService.QueryTemporaryKnowledgeAsync(sessionId, summaryQuery);
                
                if (!summaryResponse.Success || string.IsNullOrWhiteSpace(summaryResponse.Answer))
                {
                    _logger.LogWarning("Failed to get document summary, using alternative approach");
                    // Fallback: use the chunks directly
                    var chunks = summaryResponse.RelevantChunks?.Take(5).Select(c => c.Text).ToList() ?? new List<string>();
                    var fallbackContent = string.Join("\n\n", chunks);
                    return await GenerateTestFromContent(fallbackContent);
                }

                // Use the summary to generate the test
                return await GenerateTestFromContent(summaryResponse.Answer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating MC test data from PDF session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Generates MCTestData from content using GPT with the example JSON format
        /// </summary>
        private async Task<MCTestData> GenerateTestFromContent(string content)
        {
            var gptService = new GPTService(PrivateValues.ChatGPTApiKey);
            var exampleJson = System.IO.File.ReadAllText(Logic.FileHelpers.ExampleJsonFilePath);

            var prompt = $@"Based on the following document content, create a 5 question multiple choice test.

Document Content:
{content}

Use this JSON format exactly for your response:
{exampleJson}

Generate 5 high-quality multiple choice questions that test understanding of the key concepts from the document. Each question should have 4 options (A, B, C, D) with clear descriptions and a correct answer with explanation.";

            var requestData = new
            {
                model = "gpt-3.5-turbo-1106",
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant designed to output JSON. Create educational multiple choice tests based on document content." },
                    new { role = "user", content = prompt }
                }
            };

            using var httpClient = new HttpClient();
            var requestContent = new StringContent(
                JsonConvert.SerializeObject(requestData), 
                System.Text.Encoding.UTF8, 
                "application/json");
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", PrivateValues.ChatGPTApiKey);

            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                var jsonContentStr = jsonResponse?.choices[0]?.message?.content?.ToString();
                
                if (string.IsNullOrWhiteSpace(jsonContentStr))
                {
                    _logger.LogError("Empty response from ChatGPT");
                    throw new Exception("Failed to generate test: Empty response from ChatGPT");
                }

                // Clean up the JSON response
                var cleanedJson = jsonContentStr.Replace("\\n", "").Replace("\\", "");

                _logger.LogInformation("ChatGPT response received, parsing test data...");
                
                var testData = JsonConvert.DeserializeObject<MCTestData>(cleanedJson);
                
                if (testData == null || testData.Questions == null)
                {
                    _logger.LogError("Failed to parse test data from response");
                    throw new Exception("Failed to generate valid test data");
                }

                return testData;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ChatGPT API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                throw new Exception($"ChatGPT API error: {response.StatusCode}");
            }
        }
    }
}
