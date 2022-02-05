using System.Net;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Common;
using CodeRower.CCP.Controllers.Models.Enums;
using CodeRower.CCP.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Transactions.Controllers.Models;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Route("api/farming")]
    public class FarmingController : Controller
    {
        private readonly ITransactionsService _transactionsService;

        public FarmingController(ITransactionsService transactionsService)
        {
            _transactionsService = transactionsService;
        }

        [HttpGet("unlock")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> UnlockAsync()
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
            
            List<Transaction> transactions = new List<Transaction>();
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
                        PayeeId = payeeId,
                        TransactionType = "FARM",
                        Currency = Currency.COINS
                    }).ConfigureAwait(false);

                    if(debitTran?.Id.HasValue ?? false)
                    {
                        transactions.Add(debitTran);

                        // Now credit into UNLOCKED
                        var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                        {
                            Amount = amountToUnlock,
                            IsCredit = true,
                            Reference = "Automated credit",
                            Remark = "Automated credit from unlocked farm coins",
                            PayeeId = payeeId,
                            TransactionType = "UNLOCKED",
                            Currency = Currency.COINS
                        }).ConfigureAwait(false);

                        if(creditTran?.Id.HasValue ?? false)
                        {
                            transactions.Add(creditTran);
                        }
                    }
                }
            }            
            return transactions.Any() 
                ? Ok(new ListResponse<Transaction> { Rows = transactions }) 
                : NoContent();
        }
    }
}