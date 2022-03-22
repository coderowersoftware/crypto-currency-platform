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
        private readonly ITenantService _tenantService;

        public SchedulerController(ITransactionsService transactionsService, IMiningService miningService, 
            IConfiguration configuration, ITenantService tenantService)
        {
            _transactionsService = transactionsService;
            _miningService = miningService;
            _configuration = configuration;
            _tenantService = tenantService;
        }

        [HttpGet("execute-mining")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> EndMiningAsync([FromRoute, Required] Guid tenantId)
        {
            var minedLicenses = await _miningService.EndMiningAsync(tenantId).ConfigureAwait(false);
            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();
            if (minedLicenses?.Any() ?? false)
            {
                var appTenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

                var walletTenant = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletTenant").Value;
                foreach (var license in minedLicenses)
                {
                    var creditTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                    {
                        Amount = license.LicenseType.Equals(LicenseType.AIRDROP) ? appTenantInfo.OfferDailyCoinRewardForAirDropUserForDays.Value : 1m,
                        IsCredit = true,
                        Reference = $"Mined Coins via {license.LicenseType} license - {license.LicenseNumber}",
                        PayerId = walletTenant,
                        PayeeId = license.CustomerId.ToString(),
                        TransactionType = "MINED",
                        Currency = Currency.COINS,
                        CurrentBalanceFor = license.CustomerId.ToString()
                    }).ConfigureAwait(false);

                    transactions.Add(creditTran);

                    var debitTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                    {
                        Amount = license.LicenseType.Equals(LicenseType.AIRDROP) ? appTenantInfo.OfferDailyCoinRewardForAirDropUserForDays.Value : 1m,
                        IsCredit = false,
                        Reference = $"Mined Coins via {license.LicenseType} license - {license.LicenseNumber}",
                        PayerId = license.CustomerId.ToString(),
                        PayeeId = walletTenant,
                        TransactionType = "MINED",
                        Currency = Currency.COINS,
                        CurrentBalanceFor = walletTenant
                    }).ConfigureAwait(false);

                    transactions.Add(debitTran);

                    creditTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                    {
                        Amount = license.LicenseType.Equals("AIRDROP") ? 0.1m : 1m,
                        IsCredit = true,
                        Reference = $"LOCKED Mined Coins via {license.LicenseType} license - {license.LicenseNumber}",
                        PayerId = walletTenant,
                        PayeeId = license.CustomerId.ToString(),
                        TransactionType = "LOCKED",
                        Currency = Currency.COINS,
                        CurrentBalanceFor = license.CustomerId.ToString()
                    }).ConfigureAwait(false);

                    transactions.Add(creditTran);
                }
            }
            return transactions.Any() ? Ok(transactions) : NoContent();
        }

        [HttpGet("execute-minting")]
        public async Task<IActionResult> ExecuteMintingAsync([FromRoute, Required] Guid tenantId)
        {
            await _transactionsService.ExecuteFarmingMintingAsync(tenantId, "cloud-chain-technology/execute-minting", "MINT").ConfigureAwait(false);
            return Ok();
        }

        [HttpGet("execute-farming")]
        public async Task<IActionResult> ExecuteFarmingAsync([FromRoute, Required] Guid tenantId)
        {
            await _transactionsService.ExecuteFarmingMintingAsync(tenantId, "cloud-chain-technology/execute-farming", "FARM").ConfigureAwait(false);
            return Ok();
        }

    }
}