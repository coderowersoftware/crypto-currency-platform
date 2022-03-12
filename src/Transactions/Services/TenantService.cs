using System.Data;
using CodeRower.CCP.Controllers.Models;
using Npgsql;
using NpgsqlTypes;

namespace CodeRower.CCP.Services
{
    public interface ITenantService
    {
        Task<TenantInfo> GetTenantInfo(Guid tenantId);
    }

    public class TenantService : ITenantService
    {
        private readonly IConfiguration _configuration;

        public TenantService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<TenantInfo> GetTenantInfo(Guid tenantId)
        {
            var query = "get_tenant_info";

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                TenantInfo? tenantInfo = null;

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        tenantInfo = new TenantInfo();
                        tenantInfo.Id = Convert.ToString(reader["id"]);
                        tenantInfo.Name = Convert.ToString(reader["name"]);
                        if (reader["mint_rewards_daily_percent"] != DBNull.Value)
                            tenantInfo.MintRewardsDailyPercent = Convert.ToDecimal(reader["mint_rewards_daily_percent"]);
                        if (reader["farming_daily_unlock_percent"] != DBNull.Value)
                            tenantInfo.FarmingDailyUnlockPercent = Convert.ToDecimal(reader["farming_daily_unlock_percent"]);
                        if (reader["unlockToWalletFeePct"] != DBNull.Value)
                            tenantInfo.UnlockToWalletFeePct = Convert.ToDecimal(reader["unlockToWalletFeePct"]);
                        if (reader["dailyCoinRewardForAirDropUser"] != DBNull.Value)
                            tenantInfo.DailyCoinRewardForAirDropUser = Convert.ToDecimal(reader["dailyCoinRewardForAirDropUser"]);
                        if (reader["offerDailyCoinRewardForAirDropUserForDays"] != DBNull.Value)
                            tenantInfo.OfferDailyCoinRewardForAirDropUserForDays = Convert.ToDecimal(reader["offerDailyCoinRewardForAirDropUserForDays"]);
                        if (reader["walletToWalletFeePct"] != DBNull.Value)
                            tenantInfo.WalletToWalletFeePct = Convert.ToDecimal(reader["walletToWalletFeePct"]);
                        if (reader["coinPaymentWalletWithdrawalFeePct"] != DBNull.Value)
                            tenantInfo.CoinPaymentWalletWithdrawalFeePct = Convert.ToDecimal(reader["coinPaymentWalletWithdrawalFeePct"]);
                        if (reader["bankAccountWithdrawalFeePct"] != DBNull.Value)
                            tenantInfo.BankAccountWithdrawalFeePct = Convert.ToDecimal(reader["bankAccountWithdrawalFeePct"]);
                        if (reader["minWithdrawalLimitInUSD"] != DBNull.Value)
                            tenantInfo.MinWithdrawalLimitInUSD = Convert.ToDecimal(reader["minWithdrawalLimitInUSD"]);

                        tenantInfo.LicenseCost = Convert.ToDecimal(reader["licenseCost"]);
                        tenantInfo.LicenseCostCurrency = Convert.ToString(reader["licenseCostCurrency"]);
                        tenantInfo.MonthlyMaintenancePct = Convert.ToDecimal(reader["monthlyMaintenancePct"]);

                    }
                }
                return tenantInfo;
            }
        }
    }
}