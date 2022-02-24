using System.Data;
using CodeRower.CCP.Controllers.Models.Common;
using CodeRower.CCP.Controllers.Models.Reports;
using Npgsql;
using NpgsqlTypes;

namespace CodeRower.CCP.Services
{
    public interface IReportsService
    {
        Task<List<Miner>> GetTopMiners(Guid tenantId, QueryOptions? queryOptions);
        Task<Licenses?> GetLicensesInfoAsync(Guid tenantId);
        Task<OverallLicenseDetails?> GetOverallLicenseDetailsAsync(Guid tenantId);
        Task<PurchasedLicenses> GetMyPurchasedLicensesAsync(string? userId, Guid tenantId);
        Task<IEnumerable<CoinValue>> GetCoinValuesAsync(DateTime? startDate, DateTime? endDate, Guid tenantId);
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

        public async Task<List<Miner>> GetTopMiners(Guid tenantId, QueryOptions? queryOptions)
        {
            var query = "get_top_miners";

            List<Miner> topMiners = new List<Miner>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    
                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while(reader.Read())
                    {
                        Miner result = new Miner();
                        result.UserId = Convert.ToString(reader["user_id"]);
                        result.CustomerId = Convert.ToString(reader["customerId"]);
                        result.Name = Convert.ToString(reader["first_name"]);
                        result.DisplayName = Convert.ToString(reader["full_name"]);
                        result.Image = Convert.ToString(reader["image_url"]);
                        result.LicensesCount = Convert.ToInt32(reader["licenses_count"]);
                        result.Variance = 39.69m;
                        topMiners.Add(result);
                    }
                }
            }

            topMiners = topMiners.Skip(queryOptions?.Offset ?? 0).Take(queryOptions?.Limit ?? 3).ToList();
            // Update Locked and unlocked coin amounts
            // List<Task> transactionTypeBalances = new List<Task>();
            foreach (var miner in topMiners)
            {
                var amounts = await _transactionsService.GetBalancesByTransactionTypes(tenantId, new List<string> { "LOCKED", "UNLOCKED" }, miner.CustomerId).ConfigureAwait(false);
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
            return topMiners;
        }

        public async Task<Licenses?> GetLicensesInfoAsync(Guid tenantId)
        {
            var query = "get_licenses_info";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                Licenses? result = null;

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        result = new Licenses();
                        result.Total = Convert.ToInt32(reader["total_licenses"]);
                        result.Unutilized = Convert.ToInt32(reader["unutilized_licenses"]);
                        result.Used = Convert.ToInt32(reader["used_licenses"]);
                        result.Remaining = Convert.ToInt32(reader["remaining_licenses"]);
                        result.Purchased = Convert.ToInt32(reader["total_purchased"]);
                    }
                }
                return result;
            }
        }

        public async Task<OverallLicenseDetails?> GetOverallLicenseDetailsAsync(Guid tenantId)
        {
            OverallLicenseDetails? result = new OverallLicenseDetails();
            var licenseInfo = GetLicensesInfoAsync(tenantId);
            var farmMintWalletBalances = _transactionsService.GetBalancesByTransactionTypes(tenantId, new List<string> { "FARM", "MINT", "WALLET" });

            var query = "get_overall_licenses_details";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        result.TotalUsers = Convert.ToInt32(reader["total_users"]);
                        result.LicenseUsers = Convert.ToInt32(reader["total_license_users"]);
                        result.TotalAirdropUsers = Convert.ToInt32(reader["total_airdrop_users"]);
                        result.PoolLicenseMiners = Convert.ToInt32(reader["total_pool_license_miners"]);
                        result.ActiveLicenseMiners = Convert.ToInt32(reader["total_active_license_miners"]);
                    }
                }
            }

            var licenseInfoResult = await licenseInfo.ConfigureAwait(false);
            result.Total = licenseInfoResult?.Total ?? 0;
            result.Unutilized = licenseInfoResult?.Unutilized ?? 0;
            result.Used = licenseInfoResult?.Used ?? 0;
            result.Remaining = licenseInfoResult?.Remaining ?? 0;
            result.Purchased = licenseInfoResult?.Purchased ?? 0;
            
            var balancesResult = await farmMintWalletBalances.ConfigureAwait(false);
            result.CoinsInFarming = balancesResult?.FirstOrDefault(b => b.TransactionType == "FARM")?.Amount ?? 0;
            result.CoinsInMinting = balancesResult?.FirstOrDefault(b => b.TransactionType == "MINT")?.Amount ?? 0;
            result.CoinsInWallet = balancesResult?.FirstOrDefault(b => b.TransactionType == "WALLET")?.Amount ?? 0;
            return result;
        }

        public async Task<PurchasedLicenses> GetMyPurchasedLicensesAsync(string? userId, Guid tenantId)
        {
            var query = "get_purchased_licenses";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                PurchasedLicenses? result = null;

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, new Guid(userId));

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        result = new PurchasedLicenses();
                        result.TotalLicenses = Convert.ToInt32(reader["total_licenses"]);
                        result.TotalPurchased = Convert.ToInt32(reader["licenses_purchased"]);
                        result.TotalUsed = Convert.ToInt32(reader["licenses_used"]);
                    }
                }
                return result;
            }
        }

        public async Task<IEnumerable<CoinValue>> GetCoinValuesAsync(DateTime? startDate, DateTime? endDate, Guid tenantId)
        {
            var query = "get_coin_value_price_history";
            List<CoinValue> coinValues = new List<CoinValue>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    if(startDate.HasValue)
                        cmd.Parameters.AddWithValue("from_date", NpgsqlDbType.Date, startDate);
                    if(endDate.HasValue)
                        cmd.Parameters.AddWithValue("to_date", NpgsqlDbType.Date, endDate);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        CoinValue coinValue = new CoinValue();
                        coinValue.Date = Convert.ToDateTime(reader["created_at"]);
                        if(reader["value_usd"] != DBNull.Value)
                            coinValue.Value = Convert.ToDecimal(reader["value_usd"]);
                        
                        coinValues.Add(coinValue);
                    }
                }
            }
            var total = coinValues.Count();
            for(int ctr = 1; ctr < total; ctr++)
            {
                if(!coinValues[ctr].Value.HasValue
                    && coinValues[ctr-1].Value.HasValue)
                {
                    coinValues[ctr].Value = coinValues[ctr-1].Value;
                }
            }
            return coinValues;
        }
    }
}