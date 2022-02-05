using System.ComponentModel.DataAnnotations;
using System.Net;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Common;
using CodeRower.CCP.Controllers.Models.Enums;
using CodeRower.CCP.Controllers.Models.Transfers;
using CodeRower.CCP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Transactions.Controllers.Models;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Route("api/transfers")]
    public class TransfersController : Controller
    {
        private readonly ITransactionsService _transactionsService;

        public TransfersController(ITransactionsService transactionsService)
        {
            _transactionsService = transactionsService;
        }

        [HttpPost("unlocked")]
        [Authorize]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferUnlockedCoinsAsync([FromBody, Required] UnlockedCoinsTransferRequest TransferRequest)
        {
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            List<Transaction> transactions = new List<Transaction>();

            // Debit from account
            var debitTran = await _transactionsService.AddTransaction(new TransactionRequest
            {
                Amount = TransferRequest.Amount,
                IsCredit = false,
                Reference = $"Transfer to payee {TransferRequest.ToCustomerId}",
                PayerId = customerId,
                PayeeId = TransferRequest.ToCustomerId,
                TransactionType = "UNLOCKED",
                Currency = Currency.COINS
            }).ConfigureAwait(false);

            if(debitTran?.Id.HasValue ?? false)
            {
                transactions.Add(debitTran);

                // Credit to other account
                var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                {
                    Amount = TransferRequest.Amount,
                    IsCredit = true,
                    Reference = debitTran.Id.Value.ToString(),
                    PayerId = customerId,
                    PayeeId = TransferRequest.ToCustomerId,
                    TransactionType = "UNLOCKED",
                    Currency = Currency.COINS
                }).ConfigureAwait(false);

                if(creditTran?.Id.HasValue ?? false)
                {
                    transactions.Add(creditTran);
                }
            }

            return transactions.Any() ? Ok(new ListResponse<Transaction> { Rows = transactions }) : NoContent();
        }
    }
}