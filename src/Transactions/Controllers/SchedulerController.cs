using System.ComponentModel.DataAnnotations;
using System.Net;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Common;
using CodeRower.CCP.Controllers.Models.Enums;
using CodeRower.CCP.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Route("api/tenant/{tenantId}/scheduler")]
    public class SchedulerController : Controller
    {
        private readonly ITransactionsService _transactionsService;
        private readonly IMiningService _miningService;
        private readonly IConfiguration _configuration;

        public SchedulerController(ITransactionsService transactionsService, IMiningService miningService, IConfiguration configuration)
        {
            _transactionsService = transactionsService;
            _miningService = miningService;
            _configuration = configuration;
        }

        [HttpGet("execute-mining")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> EndMiningAsync([FromRoute, Required] Guid tenantId)
        {
            var minedLicenses = await _miningService.EndMiningAsync(tenantId).ConfigureAwait(false);
            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();
            if(minedLicenses?.Any() ?? false)
            {
                var walletTenant = _configuration.GetSection("AppSettings:CCCWalletTenant").Value;
                foreach(var license in minedLicenses)
                {
                    var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                    {
                        Amount = 1m,
                        IsCredit = true,
                        Reference = license.LicenseId,
                        PayerId = walletTenant,
                        PayeeId = license.CustomerId,
                        TransactionType = "MINED",
                        Currency = Currency.COINS,
                        CurrentBalanceFor = license.CustomerId
                    }).ConfigureAwait(false);

                    transactions.Add(creditTran);

                    var debitTran = await _transactionsService.AddTransaction(new TransactionRequest
                    {
                        Amount = 1m,
                        IsCredit = false,
                        Reference = license.LicenseId,
                        PayerId = license.CustomerId,
                        PayeeId = walletTenant,
                        TransactionType = "MINED",
                        Currency = Currency.COINS,
                        CurrentBalanceFor = walletTenant
                    }).ConfigureAwait(false);

                    transactions.Add(debitTran);

                    creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                    {
                        Amount = 1m,
                        IsCredit = true,
                        Reference = license.LicenseId,
                        PayerId = walletTenant,
                        PayeeId = license.CustomerId,
                        TransactionType = "LOCKED",
                        Currency = Currency.COINS,
                        CurrentBalanceFor = license.CustomerId
                    }).ConfigureAwait(false);

                    transactions.Add(creditTran);
                }
            }
            return transactions.Any() ? Ok(transactions) : NoContent();
        }

        [HttpGet("execute-minting")]
        public async Task<IActionResult> ExecuteMintingAsync([FromRoute, Required] Guid tenantId)
        {
            await _transactionsService.ExecuteFarmingMintingAsync("cloud-chain-technology/execute-minting", "MINT", tenantId).ConfigureAwait(false);
            return Ok();
        }

        [HttpGet("execute-farming")]
        public async Task<IActionResult> ExecuteFarmingAsync([FromRoute, Required] Guid tenantId)
        {
            await _transactionsService.ExecuteFarmingMintingAsync("cloud-chain-technology/execute-farming", "FARM", tenantId).ConfigureAwait(false);
            return Ok();
        }

    }
}