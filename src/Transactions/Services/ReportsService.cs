using System.Data;
using CodeRower.CCP.Controllers.Models.Reports;
using Npgsql;
using NpgsqlTypes;
using Transactions.Services;

namespace CodeRower.CCP.Services
{
    public interface IReportsService
    {
        Task<IEnumerable<Miner>> GetTopMiners();
        Task<IEnumerable<LicenseLog>> GetLicensesLogsAsync(Guid? licenseId, string? customerId);
    }

    public class ReportsService : IReportsService
    {
        private readonly ITransactionsService _transactionsService;
        private readonly IConfiguration _configuration;

        public ReportsService(ITransactionsService transactionsService,
            IConfiguration configuration)
        {
            _configuration = configuration;
            _transactionsService = transactionsService;
        }

        public async Task<IEnumerable<Miner>> GetTopMiners()
        {
            var query = "get_top_miners";

            List<Miner> topMiners = new List<Miner>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:Tenant").Value));
                    
                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while(reader.Read())
                    {
                        Miner result = new Miner();
                        result.UserId = Convert.ToString(reader["user_id"]);
                        result.Name = Convert.ToString(reader["first_name"]);
                        result.DisplayName = Convert.ToString(reader["full_name"]);
                        result.Image = Convert.ToString(reader["image_url"]);
                        result.LicensesCount = Convert.ToInt32(reader["licenses_count"]);
                        result.Variance = 39.69m;
                        topMiners.Add(result);
                    }
                }
            }

            // Update Locked and unlocked coin amounts
            // List<Task> transactionTypeBalances = new List<Task>();
            foreach(var miner in topMiners)
            {
                var amounts = await _transactionsService.GetBalancesByTransactionTypes(new List<string> { "LOCKED", "UNLOCKED" }, miner.UserId).ConfigureAwait(false);
                miner.LockedAmount = amounts?.FirstOrDefault(amt => "LOCKED".Equals(amt.TransactionType.Trim(), StringComparison.InvariantCultureIgnoreCase))?.Amount ?? 0;
                miner.UnlockedAmount = amounts?.FirstOrDefault(amt => "UNLOCKED".Equals(amt.TransactionType.Trim(), StringComparison.InvariantCultureIgnoreCase))?.Amount ?? 0;
                // transactionTypeBalances.Add(_transactionsService.GetBalancesByTransactionTypes(new List<string> { "LOCKED", "UNLOCKED" }, miner.UserId)
                //     .ContinueWith(task => 
                //     {
                //         var response = task.Result;
                //         miner.LockedAmount = response?.FirstOrDefault(amt => "LOCKED".Equals(amt.TransactionType.Trim(), StringComparison.InvariantCultureIgnoreCase))?.Amount ?? 0;
                // miner.UnlockedAmount = response?.FirstOrDefault(amt => "UNLOCKED".Equals(amt.TransactionType.Trim(), StringComparison.InvariantCultureIgnoreCase))?.Amount ?? 0;
                //     }));
            }
            //await Task.WhenAll(transactionTypeBalances).ConfigureAwait(false);
            return topMiners.DistinctBy(tm => tm.UserId);
        }

        public async Task<IEnumerable<LicenseLog>?> GetLicensesLogsAsync(Guid? licenseId, string customerId)
        {
            // var query = "getlicenseslogs";
            // List<LicenseLog> results = new List<LicenseLog>();
            // using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            // {
            //     using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
            //     {
            //         cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid(customerId));
            //         if (licenseId.HasValue)
            //         {
            //             cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId.Value);
            //         }

            //         if (conn.State != ConnectionState.Open) conn.Open();
            //         var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

            //         while (reader.Read())
            //         {
            //             LicenseLog result = new LicenseLog();
            //             result.CustomerId = new Guid(Convert.ToString(reader["customerid"]));
            //             result.LicenseId = new Guid(Convert.ToString(reader["licenseid"]));
            //             result.Title = Convert.ToString(reader["title"]);
            //             result.MiningStartedAt = Convert.ToDateTime(reader["createdat"]);

            //             if (reader["minedat"] == DBNull.Value)
            //             {
            //                 result.MiningStatus = MiningStatus.in_progress;
            //             }
            //             else
            //             {
            //                 result.MiningStatus = MiningStatus.completed;
            //             }
            //             result.MinedBy = Convert.ToString(reader["createdbyname"]);
            //             results.Add(result);
            //         }
            //     }
            // }
            // return results;
            return null;
        }
    }
}