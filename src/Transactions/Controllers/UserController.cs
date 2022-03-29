using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodeRower.CCP.Services;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Common;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Route("api/tenant/{tenantId}/users")]
    public class UserController : Controller
    {
        private readonly IUsersService _userService;

        public UserController(IUsersService userService)
        {
            _userService = userService;
        }

        [HttpGet("referrals")]
        [Authorize]
        public async Task<IActionResult> GetReferralsAsync([FromRoute, Required] Guid tenantId, [FromQuery] QueryOptions? QueryOptions = null)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var result = await _userService.GetReferralUsers(tenantId, new Guid(userId)).ConfigureAwait(false);

            var pagedResult = new PagedResponse<UserReferral>()
            {
                Rows = result?.Skip(QueryOptions?.Offset ?? 0).Take(QueryOptions?.Limit ?? 10),
                Count = result.Count,
                Offset = QueryOptions?.Offset ?? 0,
                Limit = QueryOptions?.Limit ?? 10
            };
            return Ok(pagedResult);
        }

    }
}