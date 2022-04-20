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
        Task<List<TransactionTypeBalance>> GetBalancesByTransactionTypes(Guid tenantId, List<string>? TransactionTypes, string customerId = null, bool? isCredit = null, DateTime? fromDate = null, DateTime? toDate = null, string userId = null);
        Task ExecuteFarmingMintingAsync(Guid tenantId, string relativeUri, string typeOfExecution);
        Task<WalletTransactionResponse> AddToTransactionBooks(Guid tenantId, Guid userId, CoinsTransferToCPRequest transferRequest, string bearerToken);
        Task<decimal> GetPendingTransactionAmount(Guid tenantId, Guid userId);
        Task<WalletTransactionResponse> SettleWalletToCpWalletTransaction(Guid tenantId, Guid transactionId);
        Task<TransactionBook> GetTransactionBookById(Guid tenantId, Guid transactionId);
        Task UpdateTransactionBook(Guid tenantId, TransactionBook transactionBook);
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
            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{tenantInfo.WalletHost}api/tenant/{tenantInfo.WalletTenantId}/transaction-type/get-autocomplete"),
                null,
                new
                {
                    application_id = tenantInfo.WalletTenantId,
                    client_id = tenantInfo.WalletClientId,
                    client_secret = tenantInfo.WalletSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<List<AutoCompleteResponse>>(responseMessage);
        }

        public async Task<List<AutoCompleteResponse>> GetCurrencies(Guid tenantId)
        {
            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{tenantInfo.WalletHost}api/tenant/{tenantInfo.WalletTenantId}/currency/get-autocomplete"),
                null,
                new
                {
                    application_id = tenantInfo.WalletTenantId,
                    client_id = tenantInfo.WalletClientId,
                    client_secret = tenantInfo.WalletSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<List<AutoCompleteResponse>>(responseMessage);
        }

        public async Task<Transaction> GetTransactionById(Guid tenantId, string id)
        {
            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            Uri uri = new Uri($"{tenantInfo.WalletHost}api/tenant/{tenantInfo.WalletTenantId}/get-transaction/{id}");

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                uri,
                null,
                new
                {
                    application_id = tenantInfo.WalletTenantId,
                    client_id = tenantInfo.WalletClientId,
                    client_secret = tenantInfo.WalletSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<Transaction>(responseMessage);
        }

        public async Task<List<TransactionTypeBalance>> GetBalancesByTransactionTypes(Guid tenantId, List<string>? TransactionTypes,
            string customerId = null, bool? isCredit = null, DateTime? fromDate = null, DateTime? toDate = null, string userId = null)
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

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            Uri uri = new Uri($"{tenantInfo.WalletHost}api/tenant/{tenantInfo.WalletTenantId}/get-balances-for-transaction-types?{queryString}");

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                uri,
                null,
                new
                {
                    application_id = tenantInfo.WalletTenantId,
                    client_id = tenantInfo.WalletClientId,
                    client_secret = tenantInfo.WalletSecret
                }).ConfigureAwait(false);

            var result = JsonConvert.DeserializeObject<List<TransactionTypeBalance>>(responseMessage);

            if (result?.Count > 0)
            {

                string[] arr = new string[5] { "WALLET", "LOCKED", "UNLOCKED", "MINT", "FARM" };
                result.Add(new TransactionTypeBalance
                {
                    TransactionType = "TOTAL",
                    Amount = result.Sum(item => Array.Exists(arr, element => element == item.TransactionType) ? item.Amount : 0),
                    VirtualValue = result.Sum(item => Array.Exists(arr, element => element == item.TransactionType) ? item.VirtualValue : 0),
                    Currency = result.First().Currency
                });

                string[] referral = new string[2] { "COMMISSION", "REFERRAL_SIGNUP_REWARDS" };
                result.Add(new TransactionTypeBalance
                {
                    TransactionType = "TOTAL_REFERRAL",
                    Amount = result.Sum(item => Array.Exists(referral, element => element == item.TransactionType) ? item.Amount : 0),
                    VirtualValue = result.Sum(item => Array.Exists(referral, element => element == item.TransactionType) ? item.VirtualValue : 0),
                    Currency = result.First().Currency
                });

                if (!isCredit.HasValue && !string.IsNullOrEmpty(userId))
                {
                    var pendingAmount = await GetPendingTransactionAmount(tenantId, new Guid(userId)).ConfigureAwait(false);
                    var walletAmount = result.Where(item => item.TransactionType == "WALLET").FirstOrDefault()?.Amount ?? 0;

                    result.Add(new TransactionTypeBalance
                    {
                        TransactionType = "UNSETTLED_BALANCE",
                        Amount = pendingAmount,
                        VirtualValue = 0,
                        Currency = result.First().Currency
                    });

                    result.Add(new TransactionTypeBalance
                    {
                        TransactionType = "EFFECTIVE_BALANCE",
                        Amount = walletAmount - pendingAmount,
                        VirtualValue = 0,
                        Currency = result.First().Currency
                    });

                }
            }


            return result;
        }

        public async Task<WalletTransactionResponse> AddTransaction(Guid tenantId, TransactionRequest request)
        {

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{tenantInfo.WalletHost}api/tenant/{tenantInfo.WalletTenantId}/execute-currency-transaction"),
                null,
                new
                {
                    application_id = tenantInfo.WalletTenantId,
                    client_id = tenantInfo.WalletClientId,
                    client_secret = tenantInfo.WalletSecret,
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

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            Uri uri = null;
            if (report)
            {
                uri = new Uri($"{tenantInfo.WalletHost}api/tenant/{tenantInfo.WalletTenantId}/get-transaction-report?{queryString}");
            }
            else
            {
                uri = new Uri($"{tenantInfo.WalletHost}api/tenant/{tenantInfo.WalletTenantId}/get-transaction?{queryString}");
            }

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                uri,
                null,
                new
                {
                    application_id = tenantInfo.WalletTenantId,
                    client_id = tenantInfo.WalletClientId,
                    client_secret = tenantInfo.WalletSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<TransactionsRoot>(responseMessage, new StringEnumConverter());
        }

        public async Task<dynamic> GetCurrentBalance(Guid tenantId)
        {
            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{tenantInfo.WalletHost}api/tenant/{tenantInfo.WalletTenantId}/get-transaction-report"),
                null,
                new
                {
                    application_id = tenantInfo.WalletTenantId,
                    client_id = tenantInfo.WalletClientId,
                    client_secret = tenantInfo.WalletSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<dynamic>(responseMessage, new StringEnumConverter());
        }

        public async Task ExecuteFarmingMintingAsync(Guid tenantId, string relativeUri, string typeOfExecution)
        {
            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(typeOfExecution))
            {
                dynamic executeData = null;
                if ("FARM".Equals(typeOfExecution, StringComparison.InvariantCultureIgnoreCase)
                    && tenantInfo.FarmingDailyUnlockPercent.HasValue)
                {
                    executeData = new
                    {
                        farmingDailyUnlockPercent = tenantInfo.FarmingDailyUnlockPercent,
                        currency = Controllers.Models.Enums.Currency.COINS.ToString()
                    };
                }
                else if ("MINT".Equals(typeOfExecution, StringComparison.InvariantCultureIgnoreCase)
                    && tenantInfo.MintRewardsDailyPercent.HasValue)
                {
                    executeData = new
                    {
                        mintRewardsDailyPercent = tenantInfo.MintRewardsDailyPercent,
                        currency = Controllers.Models.Enums.Currency.COINS.ToString()
                    };
                }

                if (executeData != null)
                {
                    await _restApiFacade.SendAsync(HttpMethod.Post,
                        new Uri($"{tenantInfo.WalletHost}api/tenant/{tenantInfo.WalletTenantId}/{relativeUri}"),
                        null,
                        new
                        {
                            application_id = tenantInfo.WalletTenantId,
                            client_id = tenantInfo.WalletClientId,
                            client_secret = tenantInfo.WalletSecret,
                            data = executeData
                        }).ConfigureAwait(false);
                }
            }
        }

        public async Task<WalletTransactionResponse> AddToTransactionBooks(Guid tenantId, Guid userId,
            CoinsTransferToCPRequest transferRequest, string bearerToken)
        {
            var query = "addtransaction";
            var id = string.Empty;

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            // Add transaction
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, userId);
                    cmd.Parameters.AddWithValue("amount_", NpgsqlDbType.Numeric, transferRequest.Amount);
                    cmd.Parameters.AddWithValue("is_credit", NpgsqlDbType.Boolean, false);
                    cmd.Parameters.AddWithValue("message_", NpgsqlDbType.Text, transferRequest.Message);
                    cmd.Parameters.AddWithValue("transaction_type", NpgsqlDbType.Text, transferRequest.TransactionType);
                    cmd.Parameters.AddWithValue("fee_amount", NpgsqlDbType.Numeric, transferRequest.FeeAmount);

                    if (conn.State != ConnectionState.Open) conn.Open();

                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        id = Convert.ToString(reader["transactionId"]);
                    }
                }
            }

            // invoke cp-withdrawal
            var tokenComponents = bearerToken.Split(' ');
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{tenantInfo.NodeHost}/api/tenant/{tenantId}/cp-withdrawal"),
                new Dictionary<string, string>() { { tokenComponents[0], tokenComponents[1] } },
                new
                {
                    data = new
                    {
                        amount = transferRequest.Amount,
                        currency = tenantInfo.LicenseCostCurrency,
                        transactionId = id
                    }
                }, true, null, null, bearerToken).ConfigureAwait(false);

            WalletTransactionResponse response = new WalletTransactionResponse() { transactionid = id };
            return response;
        }

        public async Task<TransactionBook> GetTransactionBookById(Guid tenantId, Guid transactionId)
        {
            var query = "gettransactionbyid";
            TransactionBook transactionBook = null;

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("transaction_id", NpgsqlDbType.Uuid, transactionId);

                    if (conn.State != ConnectionState.Open) conn.Open();

                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        transactionBook = new TransactionBook();
                        transactionBook.TransactionBookId = new Guid(Convert.ToString(reader["transactionBookId"]));
                        transactionBook.Amount = Convert.ToDecimal(reader["amount"]);
                        transactionBook.GatewayTransactionId = Convert.ToString(reader["gatewayTransactionId"]);
                        transactionBook.GatewayResponse = Convert.ToString(reader["gatewayResponse"]);
                        transactionBook.CallbackStatus = Convert.ToString(reader["callbackStatus"]);
                        transactionBook.CallbackResponse = Convert.ToString(reader["callbackResponse"]);
                        transactionBook.Status = Convert.ToString(reader["status"]);
                        transactionBook.IsCredit = Convert.ToBoolean(reader["isCredit"]);
                        transactionBook.WalletTransactionStatus = Convert.ToString(reader["walletTransactionStatus"]);
                        transactionBook.WalletResponse = Convert.ToString(reader["walletResponse"]);
                        transactionBook.CreatedAt = Convert.ToDateTime(reader["createdAt"]);

                        if (reader["updatedAt"] != DBNull.Value)
                            transactionBook.UpdatedAt = Convert.ToDateTime(reader["updatedAt"]);

                        transactionBook.FeeAmount = Convert.ToDecimal(reader["feeAmount"]);
                        transactionBook.UserId = Convert.ToString(reader["userId"]);
                        transactionBook.CustomerId = Convert.ToString(reader["customerId"]);
                    }
                }
            }

            return transactionBook;
        }

        public async Task UpdateTransactionBook(Guid tenantId, TransactionBook transactionBook)
        {
            // update response in db
            var query = "updatetransaction";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("transaction_id", NpgsqlDbType.Uuid, transactionBook.TransactionBookId);
                    cmd.Parameters.AddWithValue("wallet_response", NpgsqlDbType.Json, transactionBook.WalletResponse);
                    cmd.Parameters.AddWithValue("wallet_status", NpgsqlDbType.Text, transactionBook.WalletTransactionStatus);

                    if (conn.State != ConnectionState.Open) conn.Open();

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<decimal> GetPendingTransactionAmount(Guid tenantId, Guid userId)
        {
            var query = "getpendingtransactionamount";
            decimal amount = 0;

            // Add transaction
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, userId);

                    if (conn.State != ConnectionState.Open) conn.Open();

                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        if (reader["amount"] != DBNull.Value)
                            amount = Convert.ToDecimal(reader["amount"]);
                    }
                }
            }

            return amount;

        }

        public async Task<WalletTransactionResponse> SettleWalletToCpWalletTransaction(Guid tenantId, Guid transactionId)
        {
            var transaction = await GetTransactionBookById(tenantId, transactionId).ConfigureAwait(false);
            WalletTransactionResponse response = null;

            if (transaction != null)
            {
                response = await AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = transaction.Amount,
                    IsCredit = false,
                    Reference = $"Payment withdraw from wallet",
                    PayerId = transaction.CustomerId,
                    PayeeId = tenantId.ToString(),
                    TransactionType = "WALLET",
                    Currency = Controllers.Models.Enums.Currency.COINS,
                    CurrentBalanceFor = transaction.CustomerId,
                    Remark = transaction.TransactionBookId.ToString()
                }).ConfigureAwait(false);

                var walletFee = await AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = transaction.FeeAmount,
                    IsCredit = false,
                    Reference = $"Fee for transfer from Wallet to CP",
                    PayerId = transaction.CustomerId,
                    PayeeId = tenantId.ToString(),
                    TransactionType = "WALLET_CPWALLET_FEE",
                    Currency = Controllers.Models.Enums.Currency.COINS,
                    CurrentBalanceFor = transaction.CustomerId,
                    Remark = transaction.TransactionBookId.ToString(),
                    BaseTransaction = response.transactionid
                }).ConfigureAwait(false);

                var transactionBook = new TransactionBook()
                {
                    TransactionBookId = transaction.TransactionBookId,
                    WalletResponse = JsonConvert.SerializeObject(response),
                    WalletTransactionStatus = "success"
                };

                await UpdateTransactionBook(tenantId, transactionBook).ConfigureAwait(false);

            }

            return response;
        }
    }
}