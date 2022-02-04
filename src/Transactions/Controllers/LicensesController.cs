using System.ComponentModel.DataAnnotations;
using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Swashbuckle.AspNetCore.Annotations;
using Transactions.Controllers.Models;
using Transactions.Controllers.Models.Common;
using Transactions.Services;

namespace Transactions.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/licenses")]
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
        public async Task<IActionResult> BuyLicense([FromRoute, Required] LicenseBuyRequestData Data)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var id = await _miningService.AddLicense(Data.Data, userId).ConfigureAwait(false);

            return Ok(new { licenseId = id });
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterLicense([FromRoute, Required] LicenseRequestData Data)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            await _miningService.RegisterLicense(Data.Data, customerId, userId).ConfigureAwait(false);

            return StatusCode((int)HttpStatusCode.Created);
        }


        [HttpPatch("{LicenseId}/activate")]
        public async Task<IActionResult> ActivateLicenseAsync([FromRoute, Required] Guid LicenseId)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            try
            {
                await _miningService.ActivateLicenseAsync(LicenseId, userId).ConfigureAwait(false);
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
        public async Task<IActionResult> GetLicensesAsync([FromQuery(Name = "Filter[LicenseId]")] Guid? LicenseId,
            [FromQuery] QueryOptions? QueryOptions = null)
        {
            if (QueryOptions == null) QueryOptions = new QueryOptions();
            var results = await _miningService.GetLicensesAsync(LicenseId).ConfigureAwait(false);
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
        public async Task<IActionResult> GetLicensesLogsAsync([FromQuery(Name = "Filter[LicenseId]")] Guid? LicenseId,
            [FromQuery] QueryOptions? QueryOptions = null)
        {
            if (QueryOptions == null) QueryOptions = new QueryOptions();
            var results = await _miningService.GetLicensesLogsAsync(LicenseId).ConfigureAwait(false);
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