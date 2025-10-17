using study.ai.api;
using study.ai.api.Logic.ai;
using study.ai.api.Models;

namespace study.ai.testconsole
{
    public class Program
    {
        public static async Task Main()
        {

            var gptService = new GPTService(PrivateValues.ChatGPTApiKey);

            //var testDescription = Console.ReadLine();
            var testDescription = "Rick and morty.";

            if (string.IsNullOrWhiteSpace(testDescription)) { return; }

            var mcTest = await gptService.GenerateTestJsonAsync(testDescription);
        }
    }
}