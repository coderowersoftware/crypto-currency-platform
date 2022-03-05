using System.Data;
using CodeRower.CCP.Controllers.Models;
using Npgsql;
using NpgsqlTypes;

namespace CodeRower.CCP.Services
{
    public interface IUsersService
    {
        Task<UserInfo?> GetUserInfoAsync(Guid tenantId, string userId);
    }

    public class UsersService : IUsersService
    {
        private readonly IConfiguration _configuration;

        public UsersService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public async Task<UserInfo?> GetUserInfoAsync(Guid tenantId, string userId)
        {
            var query = "get_user_info";

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                UserInfo? userInfo = null;

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, new Guid(userId));

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        userInfo = new UserInfo();
                        userInfo.Id = Convert.ToString(reader["id"]);
                        userInfo.FullName = Convert.ToString(reader["full_name"]);
                        userInfo.AccountPin = Convert.ToString(reader["account_pin"]);
                    }
                }
                return userInfo;
            }
        }
    }
}