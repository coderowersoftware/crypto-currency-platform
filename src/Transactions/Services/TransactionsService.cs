using Newtonsoft.Json;
using Transactions.Controllers.Models;
using Transactions.Facade;

namespace Transactions.Services
{
    public interface ITransactionsService
    {
        Task<TransactionResponse> AddTransactions(string tenantId, TransactionRequest request);
        Task<TransactionsResponse> GetTransactions(string tenantId, string transactionId, string transactionType, List<decimal> amountRange, string currency, List<decimal> virtualValueRange, bool? isCredit, string reference, string paymentMethod, string remark, string description, string productId, string productName, string sku, string payerId, string payerName, string onBehalfOfId, string onBehalfOfName, string additionalData, string baseTransaction, List<string> createdAtRange, int offset, int limit, string orderBy);
    }

    public class TransactionsService : ITransactionsService
    {
        private readonly IRestApiFacade _restApiFacade;
        private readonly IConfiguration _configuration;

        public TransactionsService(IRestApiFacade restApiFacade, IConfiguration configuration)
        {
            _restApiFacade = restApiFacade;
            _configuration = configuration;
        }

        public async Task<TransactionResponse> AddTransactions(string tenantId, TransactionRequest request)
        {
            var walletHost = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:WalletHost").Value);
            var email = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:WalletEmail").Value);
            var password = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:WalletPassword").Value);
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post, 
                new Uri($"filter[{walletHost}api/tenant/{tenantId}/transaction"),
                null,
                request,
                true, 
                new Uri($"filter[{walletHost}api/auth/sign-in"), 
                new Dictionary<string, string> { { "email", email }, { "password", password } }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<TransactionResponse>(responseMessage);
        }

        public async Task<TransactionsResponse> GetTransactions(string tenantId, string transactionId, string transactionType, List<decimal> amountRange, string currency, List<decimal> virtualValueRange, bool? isCredit, string reference, string paymentMethod, string remark, string description, string productId, string productName, string sku, string payerId, string payerName, string onBehalfOfId, string onBehalfOfName, string additionalData, string baseTransaction, List<string> createdAtRange, int offset, int limit, string orderBy)
        {
            var queryString = string.Empty;
            if(!string.IsNullOrWhiteSpace(transactionId))
            {
                queryString += $"filter[{nameof(transactionId)}]={transactionId}&";
            }
            if(amountRange?.Any() ?? false)
            {
                foreach(var amount in amountRange)
                    queryString += $"filter[{nameof(amountRange)}][]={amount}&";
            }
            if(!string.IsNullOrWhiteSpace(transactionId))
            {
                queryString += $"filter[{nameof(transactionId)}]={transactionId}&";
            }
            if(!string.IsNullOrWhiteSpace(currency))
            {
                queryString += $"filter[{nameof(currency)}]={currency}&";
            }
            if(virtualValueRange?.Any() ?? false)
            {
                foreach(var value in virtualValueRange)
                    queryString += $"filter[{nameof(virtualValueRange)}][]={value}&";
            }
            if(isCredit.HasValue)
            {
                queryString += $"filter[{nameof(isCredit)}]={isCredit.Value.ToString()}&";
            }
            if(!string.IsNullOrWhiteSpace(reference))
            {
                queryString += $"filter[{nameof(reference)}]={reference}&";
            }
            if(!string.IsNullOrWhiteSpace(paymentMethod))
            {
                queryString += $"filter[{nameof(paymentMethod)}]={paymentMethod}&";
            }
            if(!string.IsNullOrWhiteSpace(remark))
            {
                queryString += $"filter[{nameof(remark)}]={remark}&";
            }
            if(!string.IsNullOrWhiteSpace(description))
            {
                queryString += $"filter[{nameof(description)}]={description}&";
            }
            if(!string.IsNullOrWhiteSpace(productId))
            {
                queryString += $"filter[{nameof(productId)}]={productId}&";
            }
            if(!string.IsNullOrWhiteSpace(productName))
            {
                queryString += $"filter[{nameof(productName)}]={productName}&";
            }
            if(!string.IsNullOrWhiteSpace(sku))
            {
                queryString += $"filter[{nameof(sku)}]={sku}&";
            }
            if(!string.IsNullOrWhiteSpace(payerId))
            {
                queryString += $"filter[{nameof(payerId)}]={payerId}&";
            }
            if(!string.IsNullOrWhiteSpace(payerName))
            {
                queryString += $"filter[{nameof(payerName)}]={payerName}&";
            }
            if(!string.IsNullOrWhiteSpace(onBehalfOfId))
            {
                queryString += $"filter[{nameof(onBehalfOfId)}]={onBehalfOfId}&";
            }
            if(!string.IsNullOrWhiteSpace(onBehalfOfName))
            {
                queryString += $"filter[{nameof(onBehalfOfName)}]={onBehalfOfName}&";
            }
            if(!string.IsNullOrWhiteSpace(additionalData))
            {
                queryString += $"filter[{nameof(additionalData)}]={additionalData}&";
            }
            if(!string.IsNullOrWhiteSpace(baseTransaction))
            {
                queryString += $"filter[{nameof(baseTransaction)}]={baseTransaction}&";
            }
            if(createdAtRange?.Any() ?? false)
            {
                foreach(var createdAt in createdAtRange)
                    queryString += $"filter[{nameof(createdAtRange)}][]={createdAt}&";
            }
            queryString += $"{nameof(offset)}={offset}&";
            queryString += $"{nameof(limit)}={limit}&";
            queryString += $"{nameof(orderBy)}={orderBy}&";

            var walletHost = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:WalletHost").Value);
            var email = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:WalletEmail").Value);
            var password = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:WalletPassword").Value);
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Get,
                new Uri($"{walletHost}api/tenant/{tenantId}/transaction?{queryString}"),
                null,
                null,
                true, 
                new Uri($"{walletHost}api/auth/sign-in"), 
                new Dictionary<string, string> { { "email", email }, { "password", password } }).ConfigureAwait(false);
            
            return JsonConvert.DeserializeObject<TransactionsResponse>(responseMessage);
        }
    }
}