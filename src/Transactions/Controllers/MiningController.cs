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
    }
}