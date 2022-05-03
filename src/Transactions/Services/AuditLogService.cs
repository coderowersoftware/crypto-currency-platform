using System.Data;
using Npgsql;
using NpgsqlTypes;
using Transactions.Domain.Models;

namespace CodeRower.CCP.Services
{
    public interface IAuditLogService
    {
        Task<string> AddAuditLog(AuditLog log);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly IConfiguration _configuration;
        public AuditLogService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> AddAuditLog(AuditLog log)
        {
            var query = "addauditlog";
            string logId= null;

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, log.TenantId);
                    cmd.Parameters.AddWithValue("entity_name", NpgsqlDbType.Text, log.EntityName);
                    cmd.Parameters.AddWithValue("entity_id", NpgsqlDbType.Uuid, log.EntityId);
                    cmd.Parameters.AddWithValue("action_", NpgsqlDbType.Text, log.Action);
                    cmd.Parameters.AddWithValue("values_", NpgsqlDbType.Json, log.Values);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, log.UserId);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        logId = Convert.ToString(reader["otp"]);
                    }
                }
            }

            return logId;

        }
    }
}