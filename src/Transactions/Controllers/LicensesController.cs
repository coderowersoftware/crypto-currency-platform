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
        private readonly ISmsService _smsService;


        public LicensesController(IMiningService miningService,
            IMapper mapper, ISmsService smsService)
        {
            _miningService = miningService;
            _mapper = mapper;
            _smsService = smsService;
        }

        [HttpPost("buy")]
        public async Task<IActionResult> BuyLicense([FromRoute, Required] Guid tenantId, [FromBody, Required] LicenseBuyRequestData Data)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            var id = await _miningService.AddLicense(tenantId, Data.Data, userId, customerId).ConfigureAwait(false);

            return Ok(new { licenseId = id });
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterLicense([FromRoute, Required] Guid tenantId, 
            [FromBody, Required] LicenseRequestData Data)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            try
            {
                await _miningService.RegisterLicense(tenantId, Data.Data, customerId, userId).ConfigureAwait(false);
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "P0003" && ex.Hint == "LicensesLimitReached")
                {
                    ModelState.AddModelError(nameof(Data.Data.LicenseNumber), "Licenses limit reached. No more licenses can be added.");
                }
                else if (ex.SqlState == "P0004" && ex.Hint == "LicenseExpired")
                {
                    ModelState.AddModelError(nameof(Data.Data.LicenseNumber), "Expired License cannnot be registered.");
                }
                else if (ex.SqlState == "P0005" && ex.Hint == "LicenseAlreadyRegistered")
                {
                    ModelState.AddModelError(nameof(Data.Data.LicenseNumber), "License already registered.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
            }

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

        [HttpGet("get-license")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(License))]
        public async Task<IActionResult> GetLicenseById([FromRoute, Required] Guid tenantId, [FromQuery, Required] Guid LicenseId, string otp)
        {
            var userId = new Guid(User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value);
            var result = await _miningService.GetLicenseByLicenseId(tenantId, userId, LicenseId).ConfigureAwait(false);

            if (!(await _smsService.VerifyAsync(tenantId, userId, otp, "get-license").ConfigureAwait(false)))
            {
                ModelState.AddModelError(nameof(otp), "Invalid Otp.");
                return BadRequest(ModelState);
            }

            return Ok(result);
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