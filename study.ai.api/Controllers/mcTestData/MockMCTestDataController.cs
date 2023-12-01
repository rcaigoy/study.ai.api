using Microsoft.AspNetCore.Mvc;
using study.ai.api.Logic.ai;
using study.ai.api.Models.mcTestData;

namespace study.ai.api.Controllers.testData
{
    [ApiController]
    [Route("[controller]")]
    public class MockMCTestDataController : ControllerBase
    {
        // POST method to receive and return MCTestData
        [HttpPost]
        public async Task<ActionResult<MCTestData>> PostMCTestData([FromBody] string testDescription)
        {
            if (string.IsNullOrWhiteSpace(testDescription))
            {
                return BadRequest("Test data is null.");
            }

            var mockPromptService = new MockPromptService();
            var testData = await mockPromptService.GenerateTestJsonAsync(testDescription);

            return Ok(testData);
        }

        // POST method to receive and return MCTestData
        [HttpGet]
        public async Task<ActionResult<MCTestData>> GetMCTestData()
        {
            var mockPromptService = new MockPromptService();
            var testData = await mockPromptService.GenerateTestJsonAsync(string.Empty);

            return Ok(testData);
        }
    }
}
