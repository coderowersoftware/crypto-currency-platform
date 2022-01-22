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
        Task<TransactionsRoot> GetTransactionReport(TransactionFilter? filter, QueryOptions? queryOptions);
        Task<dynamic> GetCurrentBalance();
        Task<TransactionResponse> InsertTransactions(TransactionRequest request);

        Task<IdentifierProfileBalance> GetBalanceByIdentifierForCurrency(string identifier, string currency);
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
            var tenantId = _configuration.GetSection("AppSettings:CCC_WALLET_TENANT").Value;
            var clientId = _configuration.GetSection("AppSettings:CCC_WALLET_CLIENT_ID").Value;
            var clientSecret = _configuration.GetSection("AppSettings:CCC_WALLET_SECRET").Value;
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
            if (!string.IsNullOrWhiteSpace(filter?.TransactionId))
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

        public async Task<TransactionResponse> InsertTransactions(TransactionRequest request)
        {
            var query = "insertvirtualvaluetransaction";
            TransactionResponse response =  null;
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

        public async Task<IdentifierProfileBalance>  GetBalanceByIdentifierForCurrency(string identifier, string currency)
        {
            var query = "getbalancebyidentifierforcurrency";
            IdentifierProfileBalance response = null;
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:CCC_WALLET_TENANT").Value));
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

  //                  currency enum_currencies,

  //                "name" text, 
		//		  "isActive" boolean, 
		//denomination text,
  //      "denominator" integer, 
		//"denominationSeparator" text, 
		//"virtualValueConversion" integer, 
		//"defaultStepAmount" numeric, 
		//"defaultStepVirtualValue" integer ,  
		//"currentAmount" numeric, 
		//"currentVirtualValue" numeric, 
		//"isBlocked" boolean, 
		//"minTransactionAmount" numeric, 
		//"maxTransactionAmount" numeric, 
		//"minTransactionVirtualValue" integer, 
		//"maxTransactionVirtualValue" integer, 
		//"stepAmount" numeric, 
		//"stepVirtualValue" integer
                }

            }
            return response;
        }
    }
}