using System.ComponentModel.DataAnnotations;
using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using CodeRower.CCP.Controllers.Models.Mining;
using CodeRower.CCP.Services;
using Swashbuckle.AspNetCore.Annotations;
using CodeRower.CCP.Controllers.Models;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Route("api/tenant/{tenantId}/mining")]
    public class MiningController : Controller
    {
        private readonly IMiningService _miningService;
        private readonly IMapper _mapper;

        public MiningController(IMiningService miningService,
            IMapper mapper)
        {
            _miningService = miningService;
            _mapper = mapper;
        }

        [HttpPost("begin")]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(License))]
        public async Task<IActionResult> MineAsync([FromRoute, Required] Guid tenantId,[FromBody, Required] MineRequestData MineRequest)
        {
            return BadRequest(new { ErrorCode = "Maintenance", Message = "The portal is under maintenance, all the transactions will be blocked until 2 PM, 5th June (Sunday)." });

            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            License result = null;
            try
            {
                result = await _miningService.MineAsync(tenantId, MineRequest.Data.LicenseId.Value, userId).ConfigureAwait(false);
            }
            catch(PostgresException ex)
            {
                if(ex.SqlState == "P0001" && ex.Hint == "MiningAlreadyInProgress")
                {
                    return BadRequest(new { ErrorCode = "MiningAlreadyInProgress", Message = $"Mining for license {MineRequest.Data.LicenseId.Value} is already in progress."});
                }
            }

            return Ok(result);
        }

    }
}