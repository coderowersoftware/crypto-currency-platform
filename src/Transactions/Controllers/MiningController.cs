using System.ComponentModel.DataAnnotations;
using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Transactions.Controllers.Models.Common;
using Transactions.Controllers.Models.Mining;
using Transactions.Services;

namespace Transactions.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/mining")]
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

        [HttpPost("")]
        public async Task<IActionResult> MineAsync([FromBody, Required]MineRequest MineRequest)
        {
            try
            {
                await _miningService.MineAsync(MineRequest.LicenseId.Value).ConfigureAwait(false);
            }
            catch(PostgresException ex)
            {
                if(ex.SqlState == "P0001" && ex.Hint == "MiningAlreadyInProgress")
                {
                    return BadRequest(new { ErrorCode = "MiningAlreadyInProgress", Message = $"Mining for license {MineRequest.LicenseId.Value} is already in progress."});
                }
            }
            
            return StatusCode((int) HttpStatusCode.Created);
        }

        [HttpGet("get-report")]
        public async Task<IActionResult> GetMiningReportAsync([FromQuery] Guid? LicenseId, 
            [FromQuery] bool IsCurrent = false,
            [FromQuery] QueryOptions? QueryOptions = null)
        {
            if(QueryOptions == null) QueryOptions = new QueryOptions();
            var results = await _miningService.GetMininReportAsync(LicenseId, IsCurrent).ConfigureAwait(false);
            var pagedResult = new PagedResponse<Mining>()
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