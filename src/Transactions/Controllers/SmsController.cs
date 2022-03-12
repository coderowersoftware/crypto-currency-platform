using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodeRower.CCP.Services;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Common;

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
        public async Task<IActionResult> SendAsync([FromRoute, Required] Guid tenantId)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var result = await _smsService.SendAsync(tenantId, new Guid(userId)).ConfigureAwait(false);

            return result ? Ok(result) : BadRequest();
        }

        [HttpGet("verify")]
        [Authorize]
        public async Task<IActionResult> SendAsync([FromRoute, Required] Guid tenantId, [FromQuery, Required] string otp)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var result = await _smsService.VerifyAsync(tenantId, new Guid(userId), otp).ConfigureAwait(false);


            return result ? Ok(result) : BadRequest();
        }

    }
}