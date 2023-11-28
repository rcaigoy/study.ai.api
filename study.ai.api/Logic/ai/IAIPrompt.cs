using study.ai.api.Models.mcTestData;

namespace study.ai.api.Logic.ai
{
    public interface IAIPrompt
    {
        public Task<MCTestData> GenerateTestJsonAsync(string testDescription);
    }
}
