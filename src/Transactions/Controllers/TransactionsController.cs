using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Transactions.Controllers.Models;
using Transactions.Controllers.Models.Common;
using Transactions.Services;

namespace Transactions.AddControllers
{
    [Authorize]
    [Route("api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly ITransactionsService _transactionsService;

        public TransactionsController(ITransactionsService transactionsService)
        {
            _transactionsService = transactionsService;
        }

        [HttpGet("tenants/{TenantId}/transactions")]
        public async Task<IActionResult> GetTransactions([FromRoute, Required]string TenantId,
            [FromQuery] TransactionFilter? Filter = null,
            [FromQuery] QueryOptions? Query = null)
        {
            if(Query == null)
            {
                Query = new QueryOptions() { OrderBy = "createdAt_DESC" };
            }
            var transactions = await _transactionsService.GetTransactions(TenantId, Filter, Query).ConfigureAwait(false);
            transactions.Limit = Query.Limit;
            transactions.Offset = Query.Offset;
            return Ok(transactions);
        }

        [HttpPost("tenants/{TenantId}/transactions")]
        public async Task<IActionResult> AddTransactions([FromRoute, Required] string TenantId, 
            [FromBody, Required] TransactionRequest Request)
        {
            var transactionResponse = await _transactionsService.AddTransactions(TenantId, Request).ConfigureAwait(false);
            return Ok(transactionResponse);
        }
    }
}