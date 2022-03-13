using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodeRower.CCP.Services;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Route("api/tenant/{tenantId}/sms")]
    public class SmsController : Controller
    {
        private readonly ISmsService _smsService;

        public SmsController(ISmsService userService)
        {
            _smsService = userService;
        }

        [HttpGet("send")]
        [Authorize]
        public async Task<IActionResult> SendAsync([FromRoute, Required] Guid tenantId, [FromQuery, Required] string service)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var result = await _smsService.SendAsync(tenantId, new Guid(userId), service).ConfigureAwait(false);

            return result ? Ok(result) : BadRequest();
        }

        [HttpGet("verify")]
        [Authorize]
        public async Task<IActionResult> VerifyAsync([FromRoute, Required] Guid tenantId, [FromQuery, Required] string otp, [FromQuery, Required] string service)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var result = await _smsService.VerifyAsync(tenantId, new Guid(userId), otp, service).ConfigureAwait(false);


            return result ? Ok(result) : BadRequest();
        }

    }
}