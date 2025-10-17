using Microsoft.AspNetCore.Mvc;
using study.ai.api.Logic.ai;
using study.ai.api.Models.mcTestData;
using study.ai.api.Models;

namespace study.ai.api.Controllers.testData
{
    [ApiController]
    [Route("[controller]")]
    public class MCTestDataController : ControllerBase
    {
        // POST method to receive and return MCTestData
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
    }
}
