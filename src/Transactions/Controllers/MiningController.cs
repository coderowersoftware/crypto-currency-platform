using System.ComponentModel.DataAnnotations;
using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using CodeRower.CCP.Controllers.Models.Mining;
using CodeRower.CCP.Services;

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
        public async Task<IActionResult> MineAsync([FromRoute, Required] Guid tenantId,[FromBody, Required] MineRequestData MineRequest)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            try
            {
                await _miningService.MineAsync(MineRequest.Data.LicenseId.Value, userId, tenantId).ConfigureAwait(false);
            }
            catch(PostgresException ex)
            {
                if(ex.SqlState == "P0001" && ex.Hint == "MiningAlreadyInProgress")
                {
                    return BadRequest(new { ErrorCode = "MiningAlreadyInProgress", Message = $"Mining for license {MineRequest.Data.LicenseId.Value} is already in progress."});
                }
            }
            
            return StatusCode((int) HttpStatusCode.Created);
        }

    }
}