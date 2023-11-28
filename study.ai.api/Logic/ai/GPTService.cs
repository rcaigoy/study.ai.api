using Newtonsoft.Json;
using study.ai.api.Models.mcTestData;
using System.Net.Http.Headers;
using System.Text;

namespace study.ai.api.Logic.ai
{
    public class GPTService : IAIPrompt
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private string _exampleJson;

        public GPTService(string apiKey)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            var exampleJsonFilePath = Environment.CurrentDirectory + @"\testData\apiTestData.json";
            _exampleJson = File.ReadAllText(exampleJsonFilePath);
        }

        public async Task<MCTestData> GenerateTestJsonAsync(string testDescription)
        {
            var prompt = $"Give me a 5 question multiple choice test about the following:  {testDescription}.\nUse this json example as format for a result:\n{_exampleJson}";
            var requestData = new
            {
                model = "gpt-3.5-turbo-1106",
                response_format = new { type = "json_object" },
                messages = new[]
                {
                new { role = "system", content = "You are a helpful assistant designed to output JSON." },
                new { role = "user", content = prompt }
            }
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                // Extract the content part from the response
                var jsonContent = jsonResponse?.choices[0]?.message?.content;
                var json = jsonContent?.ToString()?.Replace("\\n", "")?.Replace("\\", "");

                if (jsonContent is null)
                {
                    throw new Exception();
                }

                // Deserialize into MCTestData
                var testData = JsonConvert.DeserializeObject<MCTestData>(json);
                return testData;
            }

            throw new Exception();
        }
    }
}
