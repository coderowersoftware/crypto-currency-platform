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
    }
}