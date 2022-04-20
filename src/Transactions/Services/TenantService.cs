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
                        tenantInfo.MintRewardsDailyPercent = Convert.ToDecimal(reader["mint_rewards_daily_percent"]);
                        tenantInfo.FarmingDailyUnlockPercent = Convert.ToDecimal(reader["farming_daily_unlock_percent"]);
                        tenantInfo.UnlockToWalletFeePct = Convert.ToDecimal(reader["unlockToWalletFeePct"]);
                        tenantInfo.DailyCoinRewardForAirDropUser = Convert.ToDecimal(reader["dailyCoinRewardForAirDropUser"]);
                        tenantInfo.OfferDailyCoinRewardForAirDropUserForDays = Convert.ToDecimal(reader["offerDailyCoinRewardForAirDropUserForDays"]);
                        tenantInfo.WalletToWalletFeePct = Convert.ToDecimal(reader["walletToWalletFeePct"]);
                        tenantInfo.CoinPaymentWalletWithdrawalFeePct = Convert.ToDecimal(reader["coinPaymentWalletWithdrawalFeePct"]);
                        tenantInfo.BankAccountWithdrawalFeePct = Convert.ToDecimal(reader["bankAccountWithdrawalFeePct"]);
                        tenantInfo.MinWithdrawalLimitInUSD = Convert.ToDecimal(reader["minWithdrawalLimitInUSD"]);
                        tenantInfo.LicenseCost = Convert.ToDecimal(reader["licenseCost"]);
                        tenantInfo.LicenseCostCurrency = Convert.ToString(reader["licenseCostCurrency"]);
                        tenantInfo.MonthlyMaintenancePct = Convert.ToDecimal(reader["monthlyMaintenancePct"]);
                        tenantInfo.LicenseCommissionPct = Convert.ToDecimal(reader["licenseCommissionPct"]);
                        tenantInfo.LatestRateInUSD = Convert.ToDecimal(reader["latestRateInUSD"]);

                        tenantInfo.WalletTenantId = Convert.ToString(reader["walletTenantId"]);
                        tenantInfo.WalletClientId = Convert.ToString(reader["walletClientId"]);
                        tenantInfo.WalletSecret = Convert.ToString(reader["walletSecret"]);
                        tenantInfo.WalletHost = Convert.ToString(reader["walletHost"]);
                        tenantInfo.NodeHost = Convert.ToString(reader["nodeHost"]);
                        tenantInfo.TwilioAccountSID = Convert.ToString(reader["twilioAccountSID"]);
                        tenantInfo.TwilioAuthToken = Convert.ToString(reader["twilioAuthToken"]);
                        tenantInfo.AuthJWTSecret = Convert.ToString(reader["authJWTSecret"]);
                        tenantInfo.TwilioMessageServiceId = Convert.ToString(reader["twilioMessageServiceId"]);
                        tenantInfo.SendGridApiKey = Convert.ToString(reader["sendGridApiKey"]);
                        tenantInfo.SendGridOtpTemplateId = Convert.ToString(reader["sendGridOtpTemplateId"]);
                        tenantInfo.SendGridEmailFrom = Convert.ToString(reader["sendGridEmailFrom"]);
                        tenantInfo.SmtpEmailPassword = Convert.ToString(reader["smtpEmailPassword"]);
                        tenantInfo.DailyCoinRewardForPoolUser = Convert.ToDecimal(reader["dailyCoinRewardForPoolUser"]);
                    }
                }
                return tenantInfo;
            }
        }
    }
}