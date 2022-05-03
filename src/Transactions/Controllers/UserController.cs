using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodeRower.CCP.Services;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Common;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

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
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<UserReferral>))]
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


        [HttpGet("referrals-commission")]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<UserCommission>))]
        public async Task<IActionResult> GetReferralCommission([FromRoute, Required] Guid tenantId, [FromQuery, Required] string levelIdentifier, [FromQuery] QueryOptions? QueryOptions = null)
        {
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            var referralCommissions = await _userService.GetReferralCommission(tenantId, QueryOptions, customerId, levelIdentifier).ConfigureAwait(false);

            var pagedResult = new PagedResponse<UserCommission>()
            {
                Rows = referralCommissions?.Skip(QueryOptions.Offset).Take(QueryOptions.Limit),
                Count = referralCommissions?.Count(),
                Offset = QueryOptions?.Offset ?? 0,
                Limit = QueryOptions?.Limit ?? 10
            };

            return Ok(pagedResult);
        }

    }
}