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
        private readonly ITenantService _tenantService;

        public SchedulerController(ITransactionsService transactionsService, IMiningService miningService,
            ITenantService tenantService)
        {
            _transactionsService = transactionsService;
            _miningService = miningService;
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
                var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);
                var airDropReward = tenantInfo.DailyCoinRewardForAirDropUser.Value / tenantInfo.LatestRateInUSD;
                var miningCoin = tenantInfo.DailyCoinRewardForPoolUser.Value / tenantInfo.LatestRateInUSD;

                foreach (var license in minedLicenses)
                {
                    var creditTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                    {
                        Amount = license.LicenseType.Equals(LicenseType.AIRDROP) ? airDropReward : miningCoin,
                        IsCredit = true,
                        Reference = $"Mined Coins via {license.LicenseType} license - {license.LicenseNumber}",
                        PayerId = tenantInfo.WalletTenantId,
                        PayeeId = license.CustomerId.ToString(),
                        TransactionType = "MINED",
                        Currency = Currency.COINS,
                        CurrentBalanceFor = license.CustomerId.ToString()
                    }).ConfigureAwait(false);

                    transactions.Add(creditTran);

                    //var debitTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                    //{
                    //    Amount = license.LicenseType.Equals(LicenseType.AIRDROP) ? airDropReward : miningCoin,
                    //    IsCredit = false,
                    //    Reference = $"Mined Coins via {license.LicenseType} license - {license.LicenseNumber}",
                    //    PayerId = license.CustomerId.ToString(),
                    //    PayeeId = tenantInfo.WalletTenantId,
                    //    TransactionType = "MINED",
                    //    Currency = Currency.COINS,
                    //    CurrentBalanceFor = tenantInfo.WalletTenantId
                    //}).ConfigureAwait(false);

                    //transactions.Add(debitTran);

                    //creditTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                    //{
                    //    Amount = license.LicenseType.Equals(LicenseType.AIRDROP) ? airDropReward : miningCoin,
                    //    IsCredit = true,
                    //    Reference = $"LOCKED Mined Coins via {license.LicenseType} license - {license.LicenseNumber}",
                    //    PayerId = tenantInfo.WalletTenantId,
                    //    PayeeId = license.CustomerId.ToString(),
                    //    TransactionType = "LOCKED",
                    //    Currency = Currency.COINS,
                    //    CurrentBalanceFor = license.CustomerId.ToString()
                    //}).ConfigureAwait(false);

                    //transactions.Add(creditTran);
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