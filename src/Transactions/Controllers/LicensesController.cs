using System.ComponentModel.DataAnnotations;
using System.Net;
using AutoMapper;
using CodeRower.CCP.Controllers.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Swashbuckle.AspNetCore.Annotations;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Services;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/tenant/{tenantId}/licenses")]
    public class LicensesController : Controller
    {
        private readonly IMiningService _miningService;
        private readonly IMapper _mapper;

        public LicensesController(IMiningService miningService,
            IMapper mapper)
        {
            _miningService = miningService;
            _mapper = mapper;
        }

        [HttpPost("buy")]
        public async Task<IActionResult> BuyLicense([FromRoute, Required] Guid tenantId, [FromBody, Required] LicenseBuyRequestData Data)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var id = await _miningService.AddLicense(tenantId, Data.Data, userId).ConfigureAwait(false);

            return Ok(new { licenseId = id });
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterLicense([FromRoute, Required] Guid tenantId, [FromBody, Required] LicenseRequestData Data)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            await _miningService.RegisterLicense(tenantId, Data.Data, customerId, userId).ConfigureAwait(false);

            return StatusCode((int)HttpStatusCode.Created);
        }


        [HttpPatch("{LicenseId}/activate")]
        public async Task<IActionResult> ActivateLicenseAsync([FromRoute, Required] Guid tenantId, [FromRoute, Required] Guid LicenseId)
        {
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            try
            {
                await _miningService.ActivateLicenseAsync(tenantId, LicenseId, customerId).ConfigureAwait(false);
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "P0001" && ex.Hint == "LicenseAlreadyActivated")
                {
                    return BadRequest(new { ErrorCode = "LicenseAlreadyActivated", Message = $"License {LicenseId} cannot be activated." });
                }
            }

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        [HttpGet("")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(PagedResponse<License>))]
        public async Task<IActionResult> GetLicensesAsync([FromRoute, Required] Guid tenantId, [FromQuery(Name = "Filter[LicenseId]")] Guid? LicenseId,
            [FromQuery] QueryOptions? QueryOptions = null)
        {
            if (QueryOptions == null) QueryOptions = new QueryOptions();

            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            var results = await _miningService.GetLicensesAsync(tenantId, LicenseId, customerId).ConfigureAwait(false);
            var pagedResult = new PagedResponse<License>()
            {
                Rows = results?.Skip(QueryOptions.Offset).Take(QueryOptions.Limit),
                Count = results?.Count() ?? 0,
                Offset = QueryOptions.Offset,
                Limit = QueryOptions.Limit
            };
            return Ok(pagedResult);
        }

        [HttpGet("logs")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(PagedResponse<LicenseLog>))]
        public async Task<IActionResult> GetLicenseLogs([FromRoute, Required] Guid tenantId,
            [FromQuery(Name = "Filter[LicenseId]")] Guid? licenseId = null,
            [FromQuery] QueryOptions? QueryOptions = null)
        {
            if (QueryOptions == null) QueryOptions = new QueryOptions();

            var customerId = Guid.Parse(User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value);
            var result = await _miningService.GetLicenseLogsAsync(tenantId, customerId, licenseId).ConfigureAwait(false);

            var pagedResult = new PagedResponse<LicenseLog>()
            {
                Rows = result?.Skip(QueryOptions.Offset).Take(QueryOptions.Limit),
                Count = result?.Count() ?? 0,
                Offset = QueryOptions.Offset,
                Limit = QueryOptions.Limit
            };
            return Ok(pagedResult);
        }
    }
}