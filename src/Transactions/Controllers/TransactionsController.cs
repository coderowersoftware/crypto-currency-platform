using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Transactions.Controllers.Models;
using Transactions.Controllers.Models.Common;
using Transactions.Services;

namespace Transactions.AddControllers
{
    [ApiController]
    //[Authorize]
    [Route("api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly ITransactionsService _transactionsService;
        private readonly IMapper _mapper;

        public TransactionsController(ITransactionsService transactionsService,
            IMapper mapper)
        {
            _transactionsService = transactionsService;
            _mapper = mapper;
        }

        [HttpGet("transaction-report")]
        public async Task<IActionResult> GetTransactionReport([FromQuery] TransactionFilter? Filter = null,
            [FromQuery] QueryOptions? QueryOptions = null)
        {
            var defaultOrderBy = "createdAt_DESC";
            if (QueryOptions == null)
            {
                QueryOptions = new QueryOptions() { OrderBy = defaultOrderBy };
            }
            else if (string.IsNullOrWhiteSpace(QueryOptions.OrderBy))
            {
                QueryOptions.OrderBy = defaultOrderBy;
            }
            var transactionsRoot = await _transactionsService.GetTransactionReport(Filter, QueryOptions).ConfigureAwait(false);
            var transactions = _mapper.Map<List<Transaction>>(transactionsRoot?.Rows);
            var pagedResult = new PagedResponse<Transaction>()
            {
                Rows = transactions,
                Count = transactionsRoot?.Count ?? 0,
                Offset = QueryOptions.Offset,
                Limit = QueryOptions.Limit
            };
            return Ok(pagedResult);
        }

        // [HttpGet("current-balance")]
        // public async Task<IActionResult> GetCurrentBalance()
        // {
        //     var currentBalance = await _transactionsService.GetCurrentBalance().ConfigureAwait(false);
        //     return Ok(currentBalance);
        // }

        [HttpPost("")]
        public async Task<IActionResult> AddTransaction([FromBody, Required] TransactionRequest Request)
        {
            var transactionResponse = await _transactionsService.AddTransaction(Request).ConfigureAwait(false);
            return Ok(transactionResponse);
        }

        [HttpPost("wallet")]
        public async Task<IActionResult> AddWalletTransaction([FromBody, Required] TransactionRequestData data)
        {
            var transactionResponse = await _transactionsService.InsertTransactions(data.Data).ConfigureAwait(false);
            return Ok(transactionResponse);
        }

        [HttpPost("wallet-balance")]
        public async Task<IActionResult> GetBalanceByIdentifierForCurrency([FromQuery, Required] string identifier, [FromQuery, Required] string currency)
        {
            var transactionResponse = await _transactionsService.GetBalanceByIdentifierForCurrency(identifier,currency).ConfigureAwait(false);
            return Ok(transactionResponse);
        }
    }
}