using System.Data;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Common;
using CodeRower.CCP.Controllers.Models.Enums;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using Transactions.Facade;

namespace CodeRower.CCP.Services
{
    public interface IUsersService
    {
        Task<UserInfo?> GetUserInfoAsync(Guid tenantId, string userId, bool ownerInfo = false);
        Task<List<UserReferral>> GetReferralUsers(Guid tenantId, Guid userId);
        Task<List<UserCommission>> GetReferralCommission(Guid tenantId, QueryOptions? queryOptions, string customerId, string levelIdentifier);
    }

    public class UsersService : IUsersService
    {
        private readonly IConfiguration _configuration;
        private readonly IRestApiFacade _restApiFacade;
        private readonly ITenantService _tenantService;

        public UsersService(IConfiguration configuration, IRestApiFacade restApiFacade, ITenantService tenantService)
        {
            _configuration = configuration;
            _restApiFacade = restApiFacade;
            _tenantService = tenantService;
        }

        public async Task<UserInfo?> GetUserInfoAsync(Guid tenantId, string userId, bool ownerInfo = false)
        {
            var query = "get_user_info";

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                UserInfo? userInfo = null;

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, new Guid(userId));
                    cmd.Parameters.AddWithValue("owner_info", NpgsqlDbType.Boolean, ownerInfo);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        userInfo = new UserInfo();
                        userInfo.Id = Convert.ToString(reader["id"]);
                        userInfo.FullName = Convert.ToString(reader["full_name"]);
                        userInfo.AccountPin = Convert.ToString(reader["account_pin"]);
                        userInfo.CustomerId = Convert.ToString(reader["customerId"]);
                    }
                }
                return userInfo;
            }
        }

        public async Task<List<UserReferral>> GetReferralUsers(Guid tenantId, Guid userId)
        {
            var query = "get_referrals";

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                List<UserReferral> referral = new List<UserReferral>();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, userId);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        var user = new UserReferral();
                        user.ReferralCode = Convert.ToString(reader["referralCode"]);
                        user.LicenseType = (LicenseType)Enum.Parse(typeof(LicenseType), Convert.ToString(reader["licenseType"]));
                        user.CreatedAt = Convert.ToDateTime(reader["createdAt"]);
                        user.NumberOfLicenses = Convert.ToInt32(reader["numberOfLicenses"]);
                        referral.Add(user);
                    }
                }

                return referral;
            }
        }

        public async Task<List<UserCommission>> GetReferralCommission(Guid tenantId, QueryOptions? queryOptions, string customerId, string levelIdentifier)
        {
            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            Uri uri = new Uri($"{tenantInfo.WalletHost}api/tenant/{tenantInfo.WalletTenantId}/get-commission-for-levels?limit={queryOptions.Limit}&offset={queryOptions.Offset}&orderByRank=DESC&orderByCount=DESC");

            var levelData = new
            {
                userId = customerId,
                level = levelIdentifier
            };

            var responseMessage = await _restApiFacade.SendAsync(HttpMethod.Post,
                uri,
                null,
                new
                {
                    application_id = tenantInfo.WalletTenantId,
                    client_id = tenantInfo.WalletClientId,
                    client_secret = tenantInfo.WalletSecret,
                    data = levelData
                }).ConfigureAwait(false);

            var result = JsonConvert.DeserializeObject<List<UserCommission>>(responseMessage);

            return result;
        }
    }
}