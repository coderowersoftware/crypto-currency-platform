using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Net;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Common;
using CodeRower.CCP.Services;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/tenant/{tenantId}/transactions")]
    public class TransactionsController : Controller
    {
        private readonly ITransactionsService _transactionsService;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public TransactionsController(ITransactionsService transactionsService,
            IMapper mapper, ILogger<TransactionsController> logger)
        {
            _transactionsService = transactionsService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("transaction-type/autocomplete")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(List<AutoCompleteResponse>))]
        public async Task<IActionResult> GetTransactionTypes()
        {
            var transactionTypes = await _transactionsService.GetTransactionTypes().ConfigureAwait(false);

            return Ok(transactionTypes);
        }

        [HttpGet("currency/autocomplete")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(List<AutoCompleteResponse>))]
        public async Task<IActionResult> GetCurrencies()
        {
            var currencies = await _transactionsService.GetCurrencies().ConfigureAwait(false);

            return Ok(currencies);
        }

        [HttpGet("")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(PagedResponse<Transaction>))]
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

            if (string.IsNullOrWhiteSpace(PayerId))
                PayerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;

            if (string.IsNullOrWhiteSpace(PayeeId))
                PayeeId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;

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
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(Transaction))]
        public async Task<IActionResult> GetTransactionById([FromQuery] string id)
        {
            // TODO: customer id check
            var transaction = await _transactionsService.GetTransactionById(id).ConfigureAwait(false);

            return Ok(transaction);
        }

        [HttpGet("report")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(PagedResponse<Transaction>))]
        public async Task<IActionResult> GetTransactionReport([FromQuery(Name = "Filter[TransactionType]")] string? TransactionType,
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

            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            if (string.IsNullOrWhiteSpace(PayerId))
                PayerId = customerId;

            if (string.IsNullOrWhiteSpace(PayeeId))
                PayeeId = customerId;

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

            var transactionsRoot = await _transactionsService.GetTransactionReport(transactionFilter, QueryOptions, true).ConfigureAwait(false);
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

        [HttpGet("transactiontype-balances")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(List<TransactionTypeBalance>))]
        public async Task<IActionResult> GetBalancesByTransactionTypes([FromQuery(Name = "Filter[TransactionTypes][]")] List<string>? TransactionTypes, [FromQuery(Name = "Filter[FromDate]")] DateTime? FromDate = null, [FromQuery(Name = "Filter[ToDate]")] DateTime? ToDate = null)
        {
            var balances = await _transactionsService.GetBalancesByTransactionTypes(TransactionTypes, fromDate: FromDate, toDate: ToDate).ConfigureAwait(false);
            var listResult = new ListResponse<TransactionTypeBalance>
            {
                Rows = balances
            };
            return Ok(listResult);
        }

        [HttpGet("transactiontype-balances/me")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(List<TransactionTypeBalance>))]
        [Authorize]
        public async Task<IActionResult> GetMyBalanceByTransactionTypes([FromQuery(Name = "Filter[TransactionTypes][]")] List<string>? TransactionTypes, [FromQuery(Name = "Filter[FromDate]")] DateTime? FromDate = null, [FromQuery(Name = "Filter[ToDate]")] DateTime? ToDate = null)
        {
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            var balances = await _transactionsService.GetBalancesByTransactionTypes(TransactionTypes, customerId: customerId, fromDate: FromDate, toDate: ToDate).ConfigureAwait(false);
            var listResult = new ListResponse<TransactionTypeBalance>
            {
                Rows = balances
            };
            return Ok(listResult);
        }

        [HttpGet("transactiontype-credit-record")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(List<TransactionTypeBalance>))]
        public async Task<IActionResult> GetCreditsByTransactionTypes([FromQuery(Name = "Filter[TransactionTypes][]")] List<string>? TransactionTypes, [FromQuery(Name = "Filter[FromDate]")] DateTime? FromDate = null, [FromQuery(Name = "Filter[ToDate]")] DateTime? ToDate = null)
        {
            var balances = await _transactionsService.GetBalancesByTransactionTypes(TransactionTypes, isCredit: true, fromDate: FromDate, toDate: ToDate).ConfigureAwait(false);
            var listResult = new ListResponse<TransactionTypeBalance>
            {
                Rows = balances
            };
            return Ok(listResult);
        }

        [HttpGet("transactiontype-credit-record/me")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(List<TransactionTypeBalance>))]
        public async Task<IActionResult> GetMyCreditsByTransactionTypes([FromQuery(Name = "Filter[TransactionTypes][]")] List<string>? TransactionTypes, [FromQuery(Name = "Filter[FromDate]")] DateTime? FromDate = null, [FromQuery(Name = "Filter[ToDate]")] DateTime? ToDate = null)
        {
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            var balances = await _transactionsService.GetBalancesByTransactionTypes(TransactionTypes, isCredit: true, customerId: customerId, fromDate: FromDate, toDate: ToDate).ConfigureAwait(false);
            var listResult = new ListResponse<TransactionTypeBalance>
            {
                Rows = balances
            };
            return Ok(listResult);
        }

        [HttpGet("transactiontype-debit-record")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(List<TransactionTypeBalance>))]
        public async Task<IActionResult> GetDebitsByTransactionTypes([FromQuery(Name = "Filter[TransactionTypes][]")] List<string>? TransactionTypes, [FromQuery(Name = "Filter[FromDate]")] DateTime? FromDate = null, [FromQuery(Name = "Filter[ToDate]")] DateTime? ToDate = null)
        {
            var balances = await _transactionsService.GetBalancesByTransactionTypes(TransactionTypes, isCredit: false, fromDate: FromDate, toDate: ToDate).ConfigureAwait(false);
            var listResult = new ListResponse<TransactionTypeBalance>
            {
                Rows = balances
            };
            return Ok(listResult);
        }

        [HttpGet("transactiontype-debit-record/me")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(List<TransactionTypeBalance>))]
        public async Task<IActionResult> GetMyDebitsByTransactionTypes([FromQuery(Name = "Filter[TransactionTypes][]")] List<string>? TransactionTypes, [FromQuery(Name = "Filter[FromDate]")] DateTime? FromDate = null, [FromQuery(Name = "Filter[ToDate]")] DateTime? ToDate = null)
        {
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            var balances = await _transactionsService.GetBalancesByTransactionTypes(TransactionTypes, isCredit: false, customerId: customerId, fromDate: FromDate, toDate: ToDate).ConfigureAwait(false);
            var listResult = new ListResponse<TransactionTypeBalance>
            {
                Rows = balances
            };
            return Ok(listResult);
        }

    }
}