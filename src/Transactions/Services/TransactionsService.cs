using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Transactions.Controllers.Models;
using Transactions.Controllers.Models.Common;
using Transactions.Facade;
using Transactions.Domain.Models;
using Transaction = Transactions.Controllers.Models.Transaction;

namespace Transactions.Services
{
    public interface ITransactionsService
    {
        Task<Transaction> AddTransaction(TransactionRequest request);
        Task<TransactionsRoot> GetTransactionReport(TransactionFilter? filter, QueryOptions? queryOptions);
        Task<dynamic> GetCurrentBalance();
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

        public async Task<Transaction> AddTransaction(TransactionRequest request)
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var tenantId = _configuration.GetSection("CCC_WALLET_TENANT").Value;
            var clientId = _configuration.GetSection("CCC_WALLET_CLIENT_ID").Value;
            var clientSecret = _configuration.GetSection("CCC_WALLET_SECRET").Value;
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post, 
                new Uri($"{walletHost}api/tenant/{tenantId}/transaction"),
                null,
                new 
                {
                    application_id = tenantId, 
                    client_id = clientId, 
                    client_secret = clientSecret, 
                    data = request
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<Transaction>(responseMessage);
        }

        public async Task<TransactionsRoot> GetTransactionReport(TransactionFilter? filter, QueryOptions? queryOptions)
        {
            var queryString = string.Empty;
            if(!string.IsNullOrWhiteSpace(filter?.TransactionId))
            {
                queryString = $"{queryString}filter[transactionId]={filter?.TransactionId}&";
            }
            queryString = $"{queryString}offset={queryOptions?.Offset}&";
            queryString = $"{queryString}limit={queryOptions?.Limit}&";
            queryString = $"{queryString}orderBy={queryOptions?.OrderBy}&";

            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var tenantId = _configuration.GetSection("CCC_WALLET_TENANT").Value;
            var clientId = _configuration.GetSection("CCC_WALLET_CLIENT_ID").Value;
            var clientSecret = _configuration.GetSection("CCC_WALLET_SECRET").Value;
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post, 
                new Uri($"{walletHost}api/tenant/{tenantId}/get-transaction-report?{queryString}"),
                null,
                new 
                {
                    application_id = tenantId, 
                    client_id = clientId, 
                    client_secret = clientSecret
                }).ConfigureAwait(false);
            
            return JsonConvert.DeserializeObject<TransactionsRoot>(responseMessage, new StringEnumConverter());
        }

        public async Task<dynamic> GetCurrentBalance()
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var tenantId = _configuration.GetSection("CCC_WALLET_TENANT").Value;
            var clientId = _configuration.GetSection("CCC_WALLET_CLIENT_ID").Value;
            var clientSecret = _configuration.GetSection("CCC_WALLET_SECRET").Value;
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post, 
                new Uri($"{walletHost}api/tenant/{tenantId}/get-transaction-report"),
                null,
                new 
                {
                    application_id = tenantId, 
                    client_id = clientId, 
                    client_secret = clientSecret
                }).ConfigureAwait(false);
            
            return JsonConvert.DeserializeObject<dynamic>(responseMessage, new StringEnumConverter());
        }
    }
}