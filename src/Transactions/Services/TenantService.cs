using System.Data;
using CodeRower.CCP.Controllers.Models;
using Npgsql;
using NpgsqlTypes;

namespace CodeRower.CCP.Services
{
    public interface ITenantService
    {
        Task<TenantInfo> GetTenantInfo();
    }

    public class TenantService : ITenantService
    {
        private readonly IConfiguration _configuration;

        public TenantService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<TenantInfo> GetTenantInfo()
        {
            var query = "get_tenant_info";

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                TenantInfo? tenantInfo = null;

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:Tenant").Value));

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        tenantInfo = new TenantInfo();
                        tenantInfo.Id = Convert.ToString(reader["id"]);
                        tenantInfo.Name = Convert.ToString(reader["name"]);
                        if(reader["mint_rewards_daily_percent"] != DBNull.Value)
                        tenantInfo.MintRewardsDailyPercent = Convert.ToDecimal(reader["mint_rewards_daily_percent"]);
                        if(reader["farming_daily_unlock_percent"] != DBNull.Value)
                            tenantInfo.FarmingDailyUnlockPercent = Convert.ToDecimal(reader["farming_daily_unlock_percent"]);
                        if(reader["fee_transfer_to_wallet"] != DBNull.Value)
                            tenantInfo.WalletTransferFee = Convert.ToDecimal(reader["fee_transfer_to_wallet"]);
                    }
                }
                return tenantInfo;
            }
        }
    }
}