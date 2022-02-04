using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Transactions.Controllers.Models;
using Transactions.Controllers.Models.Common;
using Transactions.Facade;
using Transactions.Domain.Models;
using Transaction = Transactions.Controllers.Models.Transaction;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace Transactions.Services
{
    public interface ITransactionsService
    {
        Task<Transaction> AddTransaction(TransactionRequest request);
        Task<TransactionsRoot> GetTransactionReport(TransactionFilter? filter, QueryOptions? queryOptions, bool report);
        Task<dynamic> GetCurrentBalance();
        Task<TransactionResponse> InsertTransactions(TransactionRequest request);
        Task<IdentifierProfileBalance> GetBalanceByIdentifierForCurrency(string identifier, string currency);
        Task<List<AutoCompleteResponse>> GetTransactionTypes();
        Task<List<AutoCompleteResponse>> GetCurrencies();
        Task<Transaction> GetTransactionById(string id);
        Task<List<TransactionTypeBalance>> GetBalancesByTransactionTypes(List<string>? TransactionTypes);
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

        public async Task<List<AutoCompleteResponse>> GetTransactionTypes()
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var tenantId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection("AppSettings:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection("AppSettings:CCCWalletSecret").Value;
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{walletHost}api/tenant/{tenantId}/transaction-type/get-autocomplete"),
                null,
                new
                {
                    application_id = tenantId,
                    client_id = clientId,
                    client_secret = clientSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<List<AutoCompleteResponse>>(responseMessage);
        }

        public async Task<List<AutoCompleteResponse>> GetCurrencies()
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var tenantId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection("AppSettings:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection("AppSettings:CCCWalletSecret").Value;
            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                new Uri($"{walletHost}api/tenant/{tenantId}/currency/get-autocomplete"),
                null,
                new
                {
                    application_id = tenantId,
                    client_id = clientId,
                    client_secret = clientSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<List<AutoCompleteResponse>>(responseMessage);
        }

        public async Task<Transaction> GetTransactionById(string id)
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var tenantId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection("AppSettings:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection("AppSettings:CCCWalletSecret").Value;

            Uri uri = new Uri($"{walletHost}api/tenant/{tenantId}/get-transaction/{id}");


            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                uri,
                null,
                new
                {
                    application_id = tenantId,
                    client_id = clientId,
                    client_secret = clientSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<Transaction>(responseMessage);
        }

        public async Task<List<TransactionTypeBalance>> GetBalancesByTransactionTypes(List<string>? TransactionTypes)
        {
            var queryString = string.Empty;

            if (TransactionTypes?.Count > 0)
            {
                foreach (var item in TransactionTypes)
                {
                    queryString = $"{queryString}filter[transactionTypes][]={item}&";
                }
            }

            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var tenantId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection("AppSettings:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection("AppSettings:CCCWalletSecret").Value;

            Uri uri = new Uri($"{walletHost}api/tenant/{tenantId}/get-balances-for-transaction-types?{queryString}");


            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                uri,
                null,
                new
                {
                    application_id = tenantId,
                    client_id = clientId,
                    client_secret = clientSecret
                }).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<List<TransactionTypeBalance>>(responseMessage);
        }


        public async Task<Transaction> AddTransaction(TransactionRequest request)
        {
            var walletHost = _configuration.GetSection("AppSettings:WalletHost").Value;
            var tenantId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection("AppSettings:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection("AppSettings:CCCWalletSecret").Value;
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

        public async Task<TransactionsRoot> GetTransactionReport(TransactionFilter? filter, QueryOptions? queryOptions, bool report)
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
            var tenantId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value;
            var clientId = _configuration.GetSection("AppSettings:CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection("AppSettings:CCCWalletSecret").Value;

            Uri uri = null;
            if (report)
            {
                uri = new Uri($"{walletHost}api/tenant/{tenantId}/get-transaction-report?{queryString}");
            }
            else
            {
                uri = new Uri($"{walletHost}api/tenant/{tenantId}/get-transaction?{queryString}");
            }

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                uri,
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
            var tenantId = _configuration.GetSection("CCCWalletTenant").Value;
            var clientId = _configuration.GetSection("CCCWalletClientId").Value;
            var clientSecret = _configuration.GetSection("CCCWalletSecret").Value;
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

        public async Task<TransactionResponse> InsertTransactions(TransactionRequest request)
        {
            var query = "insertvirtualvaluetransaction";
            TransactionResponse response = null;
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid("923d95cb-3be0-41a4-997b-a41d2d657467"));
                    cmd.Parameters.AddWithValue("currency_", NpgsqlDbType.Unknown, request.Currency);
                    //cmd.Parameters.AddWithValue("amount_", NpgsqlDbType.Numeric, request.Amount);
                    cmd.Parameters.AddWithValue("transactiontype_identifier", NpgsqlDbType.Text, request.TransactionType);
                    cmd.Parameters.AddWithValue("is_credit", NpgsqlDbType.Boolean, request.IsCredit);
                    cmd.Parameters.AddWithValue("reference_", NpgsqlDbType.Varchar, request.Reference);
                    cmd.Parameters.AddWithValue("created_by_id", NpgsqlDbType.Uuid, new Guid("f7539e04-fe57-4afa-9d76-2e5f87b8ae42"));
                    cmd.Parameters.AddWithValue("created_at", NpgsqlDbType.TimestampTz, DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("updated_by_id", NpgsqlDbType.Uuid, new Guid("f7539e04-fe57-4afa-9d76-2e5f87b8ae42"));
                    cmd.Parameters.AddWithValue("updated_at", NpgsqlDbType.TimestampTz, DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("payer_id", NpgsqlDbType.Varchar, request.PayerId);
                    //cmd.Parameters.AddWithValue("payer_name", NpgsqlDbType.Varchar, request.PayerName);
                    cmd.Parameters.AddWithValue("current_balance_for", NpgsqlDbType.Varchar, request.CurrentBalanceFor);
                    cmd.Parameters.AddWithValue("onbehalfof_id", NpgsqlDbType.Varchar, request.OnBehalfOfId);
                    cmd.Parameters.AddWithValue("virtual_value", NpgsqlDbType.Numeric, request.VirtualValue);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        response = new TransactionResponse();
                        response.TransactionId = Convert.ToString(reader["transactionid"]);
                        response.Currency = Convert.ToString(reader["currency"]);
                        response.CurrentAmount = Convert.ToDecimal(reader["currentAmount"]);
                        response.CurrentVirtualValue = Convert.ToDecimal(reader["currentVirtualValue"]);

                    }
                }

            }
            return response;

        }

        public async Task<IdentifierProfileBalance> GetBalanceByIdentifierForCurrency(string identifier, string currency)
        {
            var query = "getbalancebyidentifierforcurrency";
            IdentifierProfileBalance response = null;
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:CCCWalletTenant").Value));
                    cmd.Parameters.AddWithValue("currency_", NpgsqlDbType.Unknown, currency);
                    cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Unknown, identifier);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        response = new IdentifierProfileBalance();
                        response.TransactionId = Convert.ToString(reader["transactionid"]);
                        response.Currency = Convert.ToString(reader["currency"]);
                        response.CurrentAmount = Convert.ToDecimal(reader["currentAmount"]);
                        response.CurrentVirtualValue = Convert.ToDecimal(reader["currentVirtualValue"]);

                    }

                }

            }
            return response;
        }
    }
}