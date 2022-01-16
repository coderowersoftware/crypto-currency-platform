using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Transactions.Controllers.Models;
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
            [FromQuery] string TransactionId,
            [FromQuery] string TransactionType,
            [FromQuery] List<decimal> AmountRange,
            [FromQuery] string Currency,
            [FromQuery] List<decimal> VirtualValueRange,
            [FromQuery] bool? IsCredit,
            [FromQuery] string Reference,
            [FromQuery] string PaymentMethod,
            [FromQuery] string Remark,
            [FromQuery] string Description,
            [FromQuery] string ProductId,
            [FromQuery] string ProductName,
            [FromQuery] string Sku,
            [FromQuery] string PayerId,
            [FromQuery] string PayerName,
            [FromQuery] string OnBehalfOfId,
            [FromQuery] string OnBehalfOfName,
            [FromQuery] string AdditionalData,
            [FromQuery] string BaseTransaction,
            [FromQuery] List<string> CreatedAtRange,
            [FromQuery] int Offset = 0,
            [FromQuery] int Limit = 10,
            [FromQuery] string OrderBy = "createdAt_DESC")
        {
            var transactions = await _transactionsService.GetTransactions(TenantId, TransactionId, TransactionType, AmountRange,
                Currency, VirtualValueRange, IsCredit, Reference, PaymentMethod, Remark, Description, ProductId,
                ProductName, Sku, PayerId, PayerName, OnBehalfOfId, OnBehalfOfName, AdditionalData, BaseTransaction,
                CreatedAtRange, Offset, Limit, OrderBy).ConfigureAwait(false);
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