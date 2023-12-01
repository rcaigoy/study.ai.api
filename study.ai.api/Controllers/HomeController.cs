using Microsoft.AspNetCore.Mvc;

namespace study.ai.api.Controllers
{
    [ApiController]
    [Route("")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> GetHomePage()
        {
            return Ok("Hello home page");
        }
    }
}
