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

        [Authorize]
        [HttpPost("buy")]
        public async Task<IActionResult> BuyAirdropLicense([FromRoute, Required] Guid tenantId, [FromBody, Required] LicenseBuyRequestData Data)
        {
            return BadRequest(new { ErrorCode = "Maintenance", Message = "The portal is under maintenance, all the transactions will be blocked until 2 PM, 5th June (Sunday)." });

            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;

            if (Data.Data.AuthKey == "b0126d73-c22a-4275-b4b6-bfca60ac3eaf")
            {
                Data.Data.LicenseType = Models.Enums.LicenseType.AIRDROP;

                var id = await _miningService.AddLicense(tenantId, Data.Data, userId).ConfigureAwait(false);

                return Ok(new { licenseId = id });
            }

            ModelState.AddModelError(nameof(Data.Data.AuthKey), "Auth Key is required.");

            return BadRequest(ModelState);
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> BuyPoolLicense([FromRoute, Required] Guid tenantId, [FromBody, Required] LicenseBuyRequestData Data)
        {
            return BadRequest(new { ErrorCode = "Maintenance", Message = "The portal is under maintenance, all the transactions will be blocked until 2 PM, 5th June (Sunday)." });

            if (Data.Data.AuthKey == "b0126d73-c22a-4275-b4b6-bfca60ac3eaf")
            {
                Data.Data.LicenseType = Models.Enums.LicenseType.POOL;
                var id = await _miningService.AddPoolLicense(tenantId, Data.Data).ConfigureAwait(false);

                return string.IsNullOrEmpty(id) ? BadRequest() : Ok(new { licenseId = id });
            }

            ModelState.AddModelError(nameof(Data.Data.AuthKey), "Auth Key is required.");

            return BadRequest(ModelState);
        }

        [Authorize]
        [HttpPost("register")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(License))]
        public async Task<IActionResult> RegisterLicense([FromRoute, Required] Guid tenantId,
            [FromBody, Required] LicenseRequestData Data)
        {
            return BadRequest(new { ErrorCode = "Maintenance", Message = "The portal is under maintenance, all the transactions will be blocked until 2 PM, 5th June (Sunday)."});

            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            License result = null;
            try
            {
                result = await _miningService.RegisterLicense(tenantId, Data.Data, customerId, userId).ConfigureAwait(false);
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
                else if (ex.SqlState == "P0006" && ex.Hint == "LicenseNotExist")
                {
                    ModelState.AddModelError(nameof(Data.Data.LicenseNumber), "License number invalid.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPatch("{LicenseId}/activate")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(License))]
        public async Task<IActionResult> ActivateLicenseAsync([FromRoute, Required] Guid tenantId, [FromRoute, Required] Guid LicenseId)
        {
            return BadRequest(new { ErrorCode = "Maintenance", Message = "The portal is under maintenance, all the transactions will be blocked until 2 PM, 5th June (Sunday)." });

            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            License result = null;

            try
            {
                result = await _miningService.ActivateLicenseAsync(tenantId, LicenseId, customerId).ConfigureAwait(false);
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "P0001" && ex.Hint == "LicenseAlreadyActivated")
                {
                    return BadRequest(new { ErrorCode = "LicenseAlreadyActivated", Message = $"License {LicenseId} cannot be activated." });
                }
            }

            return Ok(result);
        }

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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


        //[HttpGet("all-internal")]
        //public async Task<IActionResult> GetAllRegisteredLicenses([FromRoute, Required] Guid tenantId)
        //{

        //    var results = await _miningService.GetAllRegisteredLicensesAsync(tenantId).ConfigureAwait(false);

        //    return Ok(results);
        //}
    }
}