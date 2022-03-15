using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using CodeRower.CCP.Controllers.Models;
using Transactions.Facade;
using Transactions.Domain.Models;
using Transaction = CodeRower.CCP.Controllers.Models.Transaction;
using CodeRower.CCP.Controllers.Models.Common;
using CodeRower.CCP.Controllers.Models.Transfers;
using Npgsql;
using System.Data;
using NpgsqlTypes;

namespace CodeRower.CCP.Services
{
    public interface ITransactionsService
    {
        Task<WalletTransactionResponse> AddTransaction(Guid tenantId, TransactionRequest request);
        Task<TransactionsRoot> GetTransactionReport(Guid tenantId, TransactionFilter? filter, QueryOptions? queryOptions, bool report);
        Task<dynamic> GetCurrentBalance(Guid tenantId);
        Task<List<AutoCompleteResponse>> GetTransactionTypes(Guid tenantId);
        Task<List<AutoCompleteResponse>> GetCurrencies(Guid tenantId);
        Task<Transaction> GetTransactionById(Guid tenantId, string id);
        Task<List<TransactionTypeBalance>> GetBalancesByTransactionTypes(Guid tenantId, List<string>? TransactionTypes, string customerId = null, bool? isCredit = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task ExecuteFarmingMintingAsync(Guid tenantId, string relativeUri, string typeOfExecution);
        Task AddToTransactionBooks(Guid tenantId, Guid userId, CoinsTransferToCPRequest transferRequest, string bearerToken);
    }

    public class TransactionsService : ITransactionsService
    {
        private readonly IRestApiFacade _restApiFacade;
        private readonly ITenantService _tenantService;
        private readonly IConfiguration _configuration;

        public TransactionsService(IRestApiFacade restApiFacade, ITenantService tenantService, IConfiguration configuration)
        {
            _restApiFacade = restApiFacade;
            _tenantService = tenantService;
            _configuration = configuration;
        }

        public async Task<List<AutoCompleteResponse>> GetTransactionTypes(Guid tenantId)
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var walletTenantId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletSecret").Value;
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{walletHost}api/tenant/{walletTenantId}/transaction-type/get-autocomplete"),
                null,
                new
                {
                    application_id = walletTenantId,
                    client_id = clientId,
                    client_secret = clientSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<List<AutoCompleteResponse>>(responseMessage);
        }

        public async Task<List<AutoCompleteResponse>> GetCurrencies(Guid tenantId)
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var walletTenantId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletSecret").Value;
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{walletHost}api/tenant/{walletTenantId}/currency/get-autocomplete"),
                null,
                new
                {
                    application_id = walletTenantId,
                    client_id = clientId,
                    client_secret = clientSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<List<AutoCompleteResponse>>(responseMessage);
        }

        public async Task<Transaction> GetTransactionById(Guid tenantId, string id)
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var walletTenantId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletSecret").Value;

            Uri uri = new Uri($"{walletHost}api/tenant/{walletTenantId}/get-transaction/{id}");


            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                uri,
                null,
                new
                {
                    application_id = walletTenantId,
                    client_id = clientId,
                    client_secret = clientSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<Transaction>(responseMessage);
        }

        public async Task<List<TransactionTypeBalance>> GetBalancesByTransactionTypes(Guid tenantId, List<string>? TransactionTypes,
            string customerId = null, bool? isCredit = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var queryString = string.Empty;

            if (TransactionTypes?.Count > 0)
            {
                foreach (var item in TransactionTypes)
                {
                    queryString = $"{queryString}filter[transactionTypes][]={item}&";
                }
            }

            if (!string.IsNullOrWhiteSpace(customerId))
            {
                queryString = $"{queryString}filter[userId]={customerId}&";
            }

            if (isCredit.HasValue)
            {
                queryString = $"{queryString}filter[isCredit]={isCredit.Value}&";
            }

            if (fromDate.HasValue)
            {
                queryString = $"{queryString}filter[fromDate]={fromDate.Value.Date}&";
            }

            if (toDate.HasValue)
            {
                queryString = $"{queryString}filter[toDate]={toDate.Value.Date}&";
            }

            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var walletTenantId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletSecret").Value;

            Uri uri = new Uri($"{walletHost}api/tenant/{walletTenantId}/get-balances-for-transaction-types?{queryString}");


            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                uri,
                null,
                new
                {
                    application_id = walletTenantId,
                    client_id = clientId,
                    client_secret = clientSecret
                }).ConfigureAwait(false);

            var result = JsonConvert.DeserializeObject<List<TransactionTypeBalance>>(responseMessage);

            if (result?.Count > 0)
            {

                string[] arr = new string[3] { "WALLET", "LOCKED", "UNLOCKED" };
                result.Add(new TransactionTypeBalance
                {
                    TransactionType = "TOTAL",
                    Amount = result.Sum(item => Array.Exists(arr, element => element == item.TransactionType) ? item.Amount : 0),
                    VirtualValue = result.Sum(item => Array.Exists(arr, element => element == item.TransactionType) ? item.VirtualValue : 0),
                    Currency = result.First().Currency
                });
            }
            return result;
        }

        public async Task<WalletTransactionResponse> AddTransaction(Guid tenantId, TransactionRequest request)
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var walletTenantId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletSecret").Value;

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{walletHost}api/tenant/{walletTenantId}/execute-currency-transaction"),
                null,
                new
                {
                    application_id = walletTenantId,
                    client_id = clientId,
                    client_secret = clientSecret,
                    data = new
                    {
                        transactionType = request.TransactionType,
                        currency = request.Currency.ToString(),
                        payeeId = request.PayeeId,
                        payerId = request.PayerId,
                        currentBalanceFor = request.CurrentBalanceFor,
                        amount = request.Amount,
                        reference = request.Reference,
                        isCredit = request.IsCredit
                    }
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<WalletTransactionResponse>(responseMessage);
        }

        public async Task<TransactionsRoot> GetTransactionReport(Guid tenantId, TransactionFilter? filter, QueryOptions? queryOptions, bool report)
        {
            var queryString = string.Empty;

            if (!string.IsNullOrWhiteSpace(filter?.TransactionType))
            {
                queryString = $"{queryString}filter[transactionType]={filter?.TransactionType}&";
            }

            if (filter?.TransactionTypes?.Count > 0)
            {
                foreach (var item in filter.TransactionTypes)
                {
                    queryString = $"{queryString}filter[transactionTypes][]={item}&";
                }
            }
            if (filter?.IsCredit.HasValue ?? false)
            {
                queryString = $"{queryString}filter[isCredit]={filter?.IsCredit.Value.ToString().ToLowerInvariant()}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.Reference))
            {
                queryString = $"{queryString}filter[reference]={filter?.Reference}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.PaymentMethod))
            {
                queryString = $"{queryString}filter[paymentMethod]={filter?.PaymentMethod}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.Remark))
            {
                queryString = $"{queryString}filter[remark]={filter?.Remark}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.ProductId))
            {
                queryString = $"{queryString}filter[productId]={filter?.ProductId}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.ProductName))
            {
                queryString = $"{queryString}filter[productName]={filter?.ProductName}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.Sku))
            {
                queryString = $"{queryString}filter[sku]={filter?.Sku}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.PayerId))
            {
                queryString = $"{queryString}filter[payerId]={filter?.PayerId}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.PayerName))
            {
                queryString = $"{queryString}filter[payerName]={filter?.PayerName}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.PayeeId))
            {
                queryString = $"{queryString}filter[payeeId]={filter?.PayeeId}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.PayeeName))
            {
                queryString = $"{queryString}filter[payeeName]={filter?.PayeeName}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.OnBehalfOfId))
            {
                queryString = $"{queryString}filter[onBehalfOfId]={filter?.OnBehalfOfId}&";
            }
            if (!string.IsNullOrWhiteSpace(filter?.BaseTransaction))
            {
                queryString = $"{queryString}filter[baseTransaction]={filter?.BaseTransaction}&";
            }
            queryString = $"{queryString}offset={queryOptions?.Offset}&";
            queryString = $"{queryString}limit={queryOptions?.Limit}&";
            queryString = $"{queryString}orderBy={queryOptions?.OrderBy}&";

            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var walletTenantId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletSecret").Value;

            Uri uri = null;
            if (report)
            {
                uri = new Uri($"{walletHost}api/tenant/{walletTenantId}/get-transaction-report?{queryString}");
            }
            else
            {
                uri = new Uri($"{walletHost}api/tenant/{walletTenantId}/get-transaction?{queryString}");
            }

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                uri,
                null,
                new
                {
                    application_id = walletTenantId,
                    client_id = clientId,
                    client_secret = clientSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<TransactionsRoot>(responseMessage, new StringEnumConverter());
        }

        public async Task<dynamic> GetCurrentBalance(Guid tenantId)
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var walletTenantId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletSecret").Value;

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{walletHost}api/tenant/{walletTenantId}/get-transaction-report"),
                null,
                new
                {
                    application_id = walletTenantId,
                    client_id = clientId,
                    client_secret = clientSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<dynamic>(responseMessage, new StringEnumConverter());
        }

        public async Task ExecuteFarmingMintingAsync(Guid tenantId, string relativeUri, string typeOfExecution)
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var walletTenantId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection($"AppSettings:{tenantId}:CCCWalletSecret").Value;

            var appTenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(typeOfExecution))
            {
                dynamic executeData = null;
                if ("FARM".Equals(typeOfExecution, StringComparison.InvariantCultureIgnoreCase)
                    && appTenantInfo.FarmingDailyUnlockPercent.HasValue)
                {
                    executeData = new
                    {
                        farmingDailyUnlockPercent = appTenantInfo.FarmingDailyUnlockPercent,
                        currency = CodeRower.CCP.Controllers.Models.Enums.Currency.COINS.ToString()
                    };
                }
                else if ("MINT".Equals(typeOfExecution, StringComparison.InvariantCultureIgnoreCase)
                    && appTenantInfo.MintRewardsDailyPercent.HasValue)
                {
                    executeData = new
                    {
                        mintRewardsDailyPercent = appTenantInfo.MintRewardsDailyPercent,
                        currency = CodeRower.CCP.Controllers.Models.Enums.Currency.COINS.ToString()
                    };
                }

                if (executeData != null)
                {
                    await _restApiFacade.SendAsync(HttpMethod.Post,
                        new Uri($"{walletHost}api/tenant/{walletTenantId}/{relativeUri}"),
                        null,
                        new
                        {
                            application_id = walletTenantId,
                            client_id = clientId,
                            client_secret = clientSecret,
                            data = executeData
                        }).ConfigureAwait(false);
                }
            }
        }

        public async Task AddToTransactionBooks(Guid tenantId, Guid userId, CoinsTransferToCPRequest transferRequest, string bearerToken)
        {
            var query = "addtransaction";
            var id = string.Empty;

            // Add transaction
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, userId);
                    cmd.Parameters.AddWithValue("amount_", NpgsqlDbType.Numeric, transferRequest.Amount);
                    cmd.Parameters.AddWithValue("amount_", NpgsqlDbType.Numeric, transferRequest.Amount);
                    cmd.Parameters.AddWithValue("is_credit", NpgsqlDbType.Boolean, false);

                    if (conn.State != ConnectionState.Open) conn.Open();

                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        id = Convert.ToString(reader["transactionId"]);
                    }
                }
            }

            // invoke cp-withdrawal
            var nodeHost = _configuration.GetSection("AppSettings:NodeHost").Value;
            var tokenComponents = bearerToken.Split(' ');
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{nodeHost}api/tenant/{tenantId.ToString()}/cp-withdrawal"),
                new Dictionary<string, string>() { { tokenComponents[0], tokenComponents[1] } },
                new
                {
                    data = new
                    {
                        amount = transferRequest.Amount,
                        currency = "USDT.TRC20"
                    }
                }).ConfigureAwait(false);

            // update response in db
            query = "updatetransaction";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("transaction_id", NpgsqlDbType.Uuid, new Guid(id));
                    cmd.Parameters.AddWithValue("wallet_response", NpgsqlDbType.Json, responseMessage);

                    if (conn.State != ConnectionState.Open) conn.Open();

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
    }
}