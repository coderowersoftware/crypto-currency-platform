using System.ComponentModel.DataAnnotations;
using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Transactions.Controllers.Models;
using Transactions.Controllers.Models.Common;
using Transactions.Controllers.Models.Mining;
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

        [HttpPatch("{LicenseId}/activate")]
        public async Task<IActionResult> ActivateLicenseAsync([FromRoute, Required]Guid LicenseId)
        {
            try
            {
                await _miningService.ActivateLicenseAsync(LicenseId).ConfigureAwait(false);
            }
            catch(PostgresException ex)
            {
                if(ex.SqlState == "P0001" && ex.Hint == "LicenseAlreadyActivated")
                {
                    return BadRequest(new { ErrorCode = "LicenseAlreadyActivated", Message = $"License {LicenseId} cannot be activated."});
                }
            }
            
            return StatusCode((int) HttpStatusCode.NoContent);
        }
    }
}