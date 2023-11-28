using Newtonsoft.Json;
using study.ai.api.Models.mcTestData;

namespace study.ai.api.Logic.ai
{
    public class MockPromptService : IAIPrompt
    {
        public async Task<MCTestData> GenerateTestJsonAsync(string testDescription)
        {
            var exampleJsonFilePath = Environment.CurrentDirectory + @"\testData\apiTestData.json";
            var exampleJson = await File.ReadAllTextAsync(exampleJsonFilePath);

            var result = JsonConvert.DeserializeObject<MCTestData>(exampleJson);
            if (result is null) { throw new Exception(); }

            return result;
        }
    }
}
