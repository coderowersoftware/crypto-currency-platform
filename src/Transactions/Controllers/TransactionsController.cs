using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Transactions.Controllers.Models;
using Transactions.Controllers.Models.Common;
using Transactions.Services;

namespace Transactions.AddControllers
{
    [ApiController]
    [Authorize]
    [Route("api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly ITransactionsService _transactionsService;

        public TransactionsController(ITransactionsService transactionsService)
        {
            _transactionsService = transactionsService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilter? Filter = null,
            [FromQuery] QueryOptions? Query = null)
        {
            if(Query == null)
            {
                Query = new QueryOptions() { OrderBy = "createdAt_DESC" };
            }
            else if(string.IsNullOrWhiteSpace(Query.OrderBy))
            {
                Query.OrderBy = "createdAt_DESC";
            }
            var transactions = await _transactionsService.GetTransactions(Filter, Query).ConfigureAwait(false);
            transactions.Limit = Query.Limit;
            transactions.Offset = Query.Offset;
            return Ok(transactions);
        }

        [HttpPost("")]
        public async Task<IActionResult> AddTransactions([FromBody, Required] TransactionRequest Request)
        {
            var transactionResponse = await _transactionsService.AddTransactions(Request).ConfigureAwait(false);
            return Ok(transactionResponse);
        }
    }
}