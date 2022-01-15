using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Transactions.AddControllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : Controller
    {
        [HttpGet("users"), Authorize]
        public IActionResult GetUserName()
        {
            return Ok(User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value);
        }
    }
}