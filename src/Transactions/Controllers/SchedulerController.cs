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
    [Route("api/scheduler")]
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
        public async Task<IActionResult> EndMiningAsync()
        {
            var minedLicenses = await _miningService.EndMiningAsync().ConfigureAwait(false);
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
        public async Task<IActionResult> ExecuteMintingAsync()
        {
            await _transactionsService.ExecuteFarmingMintingAsync("cloud-chain-technology/execute-minting", "MINT").ConfigureAwait(false);
            return Ok();
        }

        [HttpGet("execute-farming")]
        public async Task<IActionResult> ExecuteFarmingAsync()
        {
            await _transactionsService.ExecuteFarmingMintingAsync("cloud-chain-technology/execute-farming", "FARM").ConfigureAwait(false);
            return Ok();
        }

        [HttpGet("settle-farmed-coins")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        [Obsolete("To be removed later")]
        public async Task<IActionResult> SettleFarmedCoinsAsync()
        {
            // get current FARM balance
            var farmedBalances = await _transactionsService
                .GetTransactionReport(new TransactionFilter
                {
                    TransactionTypes = new List<string> { "FARM" },
                    IsCredit = true
                }, new QueryOptions 
                { 
                    Offset = 0, 
                    Limit = int.MaxValue
                }, true).ConfigureAwait(false);
            
            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();
            var walletTenant = _configuration.GetSection("AppSettings:CCCWalletTenant").Value;
            // group by payee id
            if(farmedBalances?.Count > 0)
            {
                var groupedBalances = from t in farmedBalances.Rows
                                        group t by t.PayeeId;

                foreach(var balance in groupedBalances)
                {
                    var payeeId = balance.Key;
                    var amount = balance.Where(b => b.CreatedAt.AddSeconds(86400) <= DateTime.Now).Sum(b => (decimal)((dynamic)b.Amounts).COINS);
                    var amountToUnlock = amount * 0.01m;

                    // Debit from FARM
                    var debitTran = await _transactionsService.AddTransaction(new TransactionRequest
                    {
                        Amount = amountToUnlock,
                        IsCredit = false,
                        Reference = "Automated debit",
                        Remark = "Automated debit to unlock coins",
                        PayerId = walletTenant,
                        PayeeId = payeeId,
                        TransactionType = "FARM",
                        Currency = Currency.COINS
                    }).ConfigureAwait(false);

                    if(!string.IsNullOrWhiteSpace(debitTran?.transactionid))
                    {
                        transactions.Add(debitTran);

                        // Now credit into UNLOCKED
                        var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                        {
                            Amount = amountToUnlock,
                            IsCredit = true,
                            Reference = "Automated credit",
                            Remark = "Automated credit from unlocked farm coins",
                            PayerId = walletTenant,
                            PayeeId = payeeId,
                            TransactionType = "UNLOCKED",
                            Currency = Currency.COINS
                        }).ConfigureAwait(false);

                        if(!string.IsNullOrWhiteSpace(creditTran?.transactionid))
                        {
                            transactions.Add(creditTran);
                        }
                    }
                }
            }            
            return transactions.Any() 
                ? Ok(new ListResponse<WalletTransactionResponse> { Rows = transactions }) 
                : NoContent();
        }
    }
}