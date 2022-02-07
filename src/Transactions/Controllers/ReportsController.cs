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
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(Licenses))]
        public async Task<IActionResult> GetLicensesInfoAsync()
        {
            var result = await _reportsService.GetLicensesInfoAsync().ConfigureAwait(false);
            return result == null ? NoContent() : Ok(result);
        }

        [HttpGet("all-license-details")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(OverallLicenseDetails))]
        [AllowAnonymous]
        public async Task<IActionResult> GetOverallLicenseDetailsAsync()
        {
            var result = await _reportsService.GetOverallLicenseDetailsAsync().ConfigureAwait(false);
            return result == null ? NoContent() : Ok(result);
        }
    }
}