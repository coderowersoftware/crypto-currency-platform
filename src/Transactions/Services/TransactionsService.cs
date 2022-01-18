using Newtonsoft.Json;
using Transactions.Controllers.Models;
using Transactions.Controllers.Models.Common;
using Transactions.Facade;

namespace Transactions.Services
{
    public interface ITransactionsService
    {
        Task<TransactionResponse> AddTransactions(string tenantId, TransactionRequest request);
        Task<PagedResponse<List<TransactionResponse>>> GetTransactions(string tenantId, TransactionFilter filter, QueryOptions query);
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
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var email = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:CCC_USER").Value);
            var password = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:CCC_PASS").Value);
            var tenant = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:CCC_TENANT").Value);
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post, 
                new Uri($"filter[{walletHost}api/tenant/{tenantId}/transaction"),
                null,
                request,
                true, 
                new Uri($"filter[{walletHost}api/auth/sign-in"), 
                new Dictionary<string, string> { { "email", email }, { "password", password }, { "tenantId", tenant } }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<TransactionResponse>(responseMessage);
        }

        public async Task<PagedResponse<List<TransactionResponse>>> GetTransactions(string tenantId, TransactionFilter filter, QueryOptions query)
        {
            var queryString = string.Empty;
            if(!string.IsNullOrWhiteSpace(filter?.TransactionId))
            {
                queryString += $"filter[transactionId]={filter.TransactionId}&";
            }
            if(filter?.AmountRange?.Any() ?? false)
            {
                foreach(var amount in filter.AmountRange)
                    queryString += $"filter[amountRange][]={amount}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.Currency))
            {
                queryString += $"filter[currency]={filter.Currency}&";
            }
            if(filter?.VirtualValueRange?.Any() ?? false)
            {
                foreach(var value in filter?.VirtualValueRange)
                    queryString += $"filter[virtualValueRange][]={value}&";
            }
            if(filter?.IsCredit.HasValue ?? false)
            {
                queryString += $"filter[isCredit]={filter?.IsCredit.Value.ToString()}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.Reference))
            {
                queryString += $"filter[reference]={filter.Reference}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.PaymentMethod))
            {
                queryString += $"filter[paymentMethod]={filter.PaymentMethod}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.Remark))
            {
                queryString += $"filter[remark]={filter.Remark}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.Description))
            {
                queryString += $"filter[description]={filter.Description}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.ProductId))
            {
                queryString += $"filter[productId]={filter.ProductId}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.ProductName))
            {
                queryString += $"filter[productName]={filter.ProductName}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.Sku))
            {
                queryString += $"filter[sku]={filter.Sku}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.PayerId))
            {
                queryString += $"filter[payerId]={filter.PayerId}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.PayerName))
            {
                queryString += $"filter[payerName]={filter.PayerName}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.OnBehalfOfId))
            {
                queryString += $"filter[onBehalfOfId]={filter.OnBehalfOfId}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.OnBehalfOfName))
            {
                queryString += $"filter[onBehalfOfName]={filter.OnBehalfOfName}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.AdditionalData))
            {
                queryString += $"filter[additionalData]={filter.AdditionalData}&";
            }
            if(!string.IsNullOrWhiteSpace(filter?.BaseTransaction))
            {
                queryString += $"filter[baseTransaction]={filter.BaseTransaction}&";
            }
            if(filter?.CreatedAtRange?.Any() ?? false)
            {
                foreach(var createdAt in filter.CreatedAtRange)
                    queryString += $"filter[createdAtRange][]={createdAt}&";
            }
            queryString += $"offset={query?.Offset}&";
            queryString += $"limit={query?.Limit}&";
            queryString += $"orderBy={query?.OrderBy}&";

            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var email = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:CCC_USER").Value);
            var password = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:CCC_PASS").Value);
            var tenant = Environment.GetEnvironmentVariable(_configuration.GetSection("AppSettings:CCC_TENANT").Value);
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Get,
                new Uri($"{walletHost}api/tenant/{tenantId}/transaction?{queryString}"),
                null,
                null,
                true, 
                new Uri($"{walletHost}api/auth/sign-in"), 
                new Dictionary<string, string> { { "email", email }, { "password", password }, { "tenantId", tenant } }).ConfigureAwait(false);
            
            return JsonConvert.DeserializeObject<PagedResponse<List<TransactionResponse>>>(responseMessage);
        }
    }
}