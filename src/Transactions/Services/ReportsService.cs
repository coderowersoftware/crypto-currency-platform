using System.Data;
using CodeRower.CCP.Controllers.Models.Reports;
using Npgsql;
using NpgsqlTypes;

namespace CodeRower.CCP.Services
{
    public interface IReportsService
    {
        Task<IEnumerable<Miner>> GetTopMiners();
        Task<Licenses?> GetLicensesInfoAsync();
        Task<OverallLicenseDetails?> GetOverallLicenseDetailsAsync();
        Task<PurchasedLicenses> GetMyPurchasedLicensesAsync(string? userId);
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

        public async Task<Licenses?> GetLicensesInfoAsync()
        {
            var query = "get_licenses_info";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                Licenses? result = null;

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:Tenant").Value));

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

        public async Task<OverallLicenseDetails?> GetOverallLicenseDetailsAsync()
        {
            OverallLicenseDetails? result = new OverallLicenseDetails();
            var licenseInfo = GetLicensesInfoAsync();
            var farmMintWalletBalances = _transactionsService.GetBalancesByTransactionTypes(new List<string> { "FARM", "MINT", "WALLET" });

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

        public async Task<PurchasedLicenses> GetMyPurchasedLicensesAsync(string? userId)
        {
            var query = "get_purchased_licenses";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                PurchasedLicenses? result = null;

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:Tenant").Value));
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
    }
}