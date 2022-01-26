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

        [HttpGet("transaction-type/autocomplete")]
        public async Task<IActionResult> GetTransactionTypes()
        {
            var transactionTypes = await _transactionsService.GetTransactionTypes().ConfigureAwait(false);

            return Ok(transactionTypes);
        }

        [HttpGet("currency/autocomplete")]
        public async Task<IActionResult> GetCurrencies()
        {
            var currencies = await _transactionsService.GetCurrencies().ConfigureAwait(false);

            return Ok(currencies);
        }

        [HttpGet("")]
        public async Task<IActionResult> GetTransactions([FromQuery(Name = "Filter[TransactionType]")] string? TransactionType, 
            [FromQuery(Name = "Filter[TransactionTypes][]")] List<string>? TransactionTypes, 
            [FromQuery(Name = "Filter[IsCredit]")] bool? IsCredit, 
            [FromQuery(Name = "Filter[Reference]")] string? Reference, 
            [FromQuery(Name = "Filter[PaymentMethod]")] string? PaymentMethod,
            [FromQuery(Name = "Filter[Remark]")] string? Remark,
            [FromQuery(Name = "Filter[Description]")] string? Description,
            [FromQuery(Name = "Filter[ProductId]")] string? ProductId,
            [FromQuery(Name = "Filter[ProductName]")] string? ProductName,
            [FromQuery(Name = "Filter[Sku]")] string? Sku,
            [FromQuery(Name = "Filter[PayerId]")] string? PayerId,
            [FromQuery(Name = "Filter[PayerName]")] string? PayerName,
            [FromQuery(Name = "Filter[PayeeId]")] string? PayeeId,
            [FromQuery(Name = "Filter[PayeeName]")] string? PayeeName,
            [FromQuery(Name = "Filter[OnBehalfOfId]")] string? OnBehalfOfId,
            [FromQuery(Name = "Filter[OnBehalfOfName]")] string? OnBehalfOfName,
            [FromQuery(Name = "Filter[BaseTransaction]")] string? BaseTransaction,
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
            var transactionFilter = new TransactionFilter
            {
                TransactionType = TransactionType,
                TransactionTypes = TransactionTypes,
                IsCredit = IsCredit,
                Reference = Reference,
                PaymentMethod = PaymentMethod,
                Remark = Remark,
                Description = Description,
                ProductId = ProductId,
                ProductName = ProductName,
                Sku = Sku,
                PayerId = PayerId,
                PayerName = PayerName,
                PayeeId = PayeeId,
                PayeeName = PayeeName,
                OnBehalfOfId = OnBehalfOfId,
                OnBehalfOfName = OnBehalfOfName,
                BaseTransaction = BaseTransaction
            };
            var transactionsRoot = await _transactionsService.GetTransactionReport(transactionFilter, QueryOptions, false).ConfigureAwait(false);
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

        [HttpGet("id")]
        public async Task<IActionResult> GetTransactionById([FromQuery] string id)
        {
            var transaction = await _transactionsService.GetTransactionById(id).ConfigureAwait(false);
           
            return Ok(transaction);
        }

        [HttpGet("report")]
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
            var transactionsRoot = await _transactionsService.GetTransactionReport(Filter, QueryOptions, true).ConfigureAwait(false);
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
            var transactionResponse = await _transactionsService.GetBalanceByIdentifierForCurrency(identifier, currency).ConfigureAwait(false);
            return Ok(transactionResponse);
        }
    }
}