using System.ComponentModel.DataAnnotations;
using System.Net;
using CodeRower.CCP.Controllers.Models.Common;
using CodeRower.CCP.Controllers.Models.Reports;
using CodeRower.CCP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Annotations;
using Transactions.Controllers.Models.Reports;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Route("api/tenant/{tenantId}/reports")]
    // [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportsService _reportsService;
        private readonly IConnectionMultiplexer _redis;

        public ReportsController(IReportsService reportsService, IConnectionMultiplexer redis)
        {
            _reportsService = reportsService;
            _redis = redis;
        }

        [HttpGet("top-miners")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Miner>))]
        public async Task<IActionResult> GetTopMinersAsync([FromRoute, Required] Guid tenantId, [FromQuery] QueryOptions? QueryOptions = null)
        {
            PagedResponse<Miner> pagedResult = null;
            string key = $"{tenantId}_TOPMINERS_{QueryOptions?.Limit}_{QueryOptions?.Offset}";

            var db = _redis.GetDatabase();
            var cache = await db.StringGetAsync(key).ConfigureAwait(false);

            if (string.IsNullOrEmpty(cache))
            {
                MinersReponse topMiners = await _reportsService.GetTopMiners(tenantId, QueryOptions).ConfigureAwait(false);

                pagedResult = new PagedResponse<Miner>()
                {
                    Rows = topMiners.Miners,
                    Count = topMiners.Count,
                    Offset = QueryOptions?.Offset ?? 0,
                    Limit = QueryOptions?.Limit ?? 10
                };

                db.StringSet(key, JsonConvert.SerializeObject(pagedResult), new TimeSpan(0, 15, 0));
            }
            else
            {
                pagedResult = JsonConvert.DeserializeObject<PagedResponse<Miner>>(cache);
            }

            return Ok(pagedResult);
        }

        [HttpGet("licenses")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(Licenses))]
        public async Task<IActionResult> GetLicensesInfoAsync([FromRoute, Required] Guid tenantId)
        {
            var result = await _reportsService.GetLicensesInfoAsync(tenantId).ConfigureAwait(false);
            return result == null ? NoContent() : Ok(result);
        }

        [HttpGet("licenses/purchased")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(PurchasedLicenses))]
        public async Task<IActionResult> GetMyPurchasedLicensesAsync([FromRoute, Required] Guid tenantId)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var result = await _reportsService.GetMyPurchasedLicensesAsync(userId, tenantId).ConfigureAwait(false);
            return result == null ? NoContent() : Ok(result);
        }

        [HttpGet("all-license-details")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(OverallLicenseDetails))]
        [AllowAnonymous]
        public async Task<IActionResult> GetOverallLicenseDetailsAsync([FromRoute, Required] Guid tenantId)
        {
            var result = await _reportsService.GetOverallLicenseDetailsAsync(tenantId).ConfigureAwait(false);
            return result == null ? NoContent() : Ok(result);
        }

        [HttpGet("coin-value-history")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(List<CoinValue>))]
        [AllowAnonymous]
        public async Task<IActionResult> GetCoinValueHistorysAsync([FromRoute, Required] Guid tenantId, [FromQuery(Name = "Filter[StartDate]")] DateTime? StartDate = null,
            [FromQuery(Name = "Filter[EndDate]")] DateTime? EndDate = null)
        {
            var result = await _reportsService.GetCoinValuesAsync(StartDate, EndDate, tenantId).ConfigureAwait(false);
            return result == null ? NoContent() : Ok(result);
        }
    }
}