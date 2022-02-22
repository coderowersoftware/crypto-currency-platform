using System.ComponentModel.DataAnnotations;
using System.Net;
using CodeRower.CCP.Controllers.Models.Common;
using CodeRower.CCP.Controllers.Models.Reports;
using CodeRower.CCP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Route("api/tenant/{tenantId}/reports")]
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportsService _reportsService;
        
        public ReportsController(IReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [HttpGet("top-miners")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Miner>))]
        public async Task<IActionResult> GetTopMinersAsync([FromRoute, Required] Guid tenantId)
        {
            var topMiners = await _reportsService.GetTopMiners(tenantId).ConfigureAwait(false);
            var listResult = new ListResponse<Miner>()
            {
                Rows = topMiners
            };
            return Ok(listResult);
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
        public async Task<IActionResult> GetCoinValueHistorysAsync([FromRoute, Required] Guid tenantId,[FromQuery(Name = "Filter[StartDate]")] DateTime? StartDate = null,
            [FromQuery(Name = "Filter[EndDate]")] DateTime? EndDate = null)
        {
            var result = await _reportsService.GetCoinValuesAsync(StartDate, EndDate, tenantId).ConfigureAwait(false);
            return result == null ? NoContent() : Ok(result);
        }
    }
}