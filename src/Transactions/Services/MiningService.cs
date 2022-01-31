using System.Data;
using Npgsql;
using NpgsqlTypes;
using Transactions.Controllers.Models.Enums;
using Transactions.Controllers.Models.Mining;

namespace Transactions.Services
{
    public interface IMiningService
    {
        Task MineAsync(Guid licenseId);
        Task<IEnumerable<Mining>?> GetMininReportAsync(Guid? licenseId, bool isCurrent);

        Task ActivateLicenseAsync(Guid licenseId);
    }

    public class MiningService : IMiningService
    {
        private readonly IConfiguration _configuration;

        public MiningService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<Mining>?> GetMininReportAsync(Guid? licenseId, bool isCurrent)
        {
            var query = "getminingreport";
            List<Mining> results = new List<Mining>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, new Guid("b746b411-c799-4d5d-8003-f236e236a1fa")); // TODO: to be picked from auth token
                    cmd.Parameters.AddWithValue("is_current", NpgsqlDbType.Boolean, isCurrent);
                    if(licenseId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId.Value);
                    }

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while(reader.Read())
                    {
                        Mining result = new Mining();
                        result.UserId = new Guid(Convert.ToString(reader["userid"]));
                        result.LicenseId = new Guid(Convert.ToString(reader["licenseid"]));
                        result.StartDate = Convert.ToDateTime(reader["startDate"]);
                        if (reader["endDate"] == DBNull.Value)
                        {
                            result.MiningStatus = MiningStatus.InProgress;
                        }
                        else
                        {
                            result.MiningStatus = MiningStatus.Completed;
                        }
                        results.Add(result);
                    }
                }
            }
            return results;
        }

        public async Task MineAsync(Guid licenseId)
        {
            var query = "minelicense";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId);
                    cmd.Parameters.AddWithValue("created_by_id", NpgsqlDbType.Uuid, new Guid("b746b411-c799-4d5d-8003-f236e236a1fa")); // TODO: to be taken from token later
                    if (conn.State != ConnectionState.Open) conn.Open();

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task ActivateLicenseAsync(Guid licenseId)
        {
            var query = "activatelicense";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId);
                    // TODO: Security flaw. we should have an additional input parameter here (taken from Token not in request)
                    // using this additional parameter, verify if license belong to the user who is activating it.
                    // Use customer id, preferably. Validate tenant as well.
                    if (conn.State != ConnectionState.Open) conn.Open();

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
    }
}