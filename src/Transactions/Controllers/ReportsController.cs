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
    [Route("api/reports")]
    public class ReportsController : Controller
    {
        private readonly IReportsService _reportsService;
        
        public ReportsController(IReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [HttpGet("top-miners")]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Miner>))]
        public async Task<IActionResult> GetTopMinersAsync()
        {
            var topMiners = await _reportsService.GetTopMiners().ConfigureAwait(false);
            var listResult = new ListResponse<Miner>()
            {
                Rows = topMiners
            };
            return Ok(listResult);
        }

        [HttpGet("licenses")]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<LicenseLog>))]
        public async Task<IActionResult> GetLicensesLogsAsync([FromQuery(Name = "Filter[LicenseId]")] Guid? LicenseId,
            [FromQuery] QueryOptions? QueryOptions = null)
        {
            if (QueryOptions == null) QueryOptions = new QueryOptions();

            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;

            var results = await _reportsService.GetLicensesLogsAsync(LicenseId, customerId).ConfigureAwait(false);
            var pagedResult = new PagedResponse<LicenseLog>()
            {
                Rows = results?.Skip(QueryOptions.Offset).Take(QueryOptions.Limit),
                Count = results?.Count() ?? 0,
                Offset = QueryOptions.Offset,
                Limit = QueryOptions.Limit
            };
            return Ok(pagedResult);
        }
    }
}