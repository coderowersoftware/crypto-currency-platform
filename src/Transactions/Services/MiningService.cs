using System.Data;
using Npgsql;
using NpgsqlTypes;
using Transactions.Controllers.Models;
using Transactions.Controllers.Models.Enums;
using License = Transactions.Controllers.Models.License;

namespace Transactions.Services
{
    public interface IMiningService
    {
        Task MineAsync(Guid licenseId, string userId);
        Task<IEnumerable<License>?> GetLicensesAsync(Guid? licenseId);

        Task<IEnumerable<LicenseLog>?> GetLicensesLogsAsync(Guid? licenseId);

        Task ActivateLicenseAsync(Guid licenseId);
        Task EndMiningAsync();
    }

    public class MiningService : IMiningService
    {
        private readonly IConfiguration _configuration;

        public MiningService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<License>?> GetLicensesAsync(Guid? licenseId)
        {
            var query = "getlicenses";
            List<License> results = new List<License>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid("3d0b7184-f155-4eb4-9f29-0005c99dcd48")); // TODO: to be picked from auth token
                    if(licenseId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId.Value);
                    }

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while(reader.Read())
                    {
                        License result = new License();
                        result.CustomerId = new Guid(Convert.ToString(reader["customerid"]));
                        result.LicenseId = new Guid(Convert.ToString(reader["licenseid"]));
                        result.Title = Convert.ToString(reader["title"]);
                        var activatedOn = reader["activatedon"];
                        if(activatedOn != DBNull.Value)
                        {
                            result.ActivationDate = Convert.ToDateTime(activatedOn);
                        }
                        var expiresOn = reader["expireson"];
                        if(expiresOn != DBNull.Value)
                        {
                            result.ExpirationDate = Convert.ToDateTime(expiresOn);
                        }

                        if(result.ActivationDate.HasValue
                            && result.ExpirationDate >= DateTime.Now)
                        {
                            if (reader["minedat"] == DBNull.Value)
                            {
                                result.MiningStatus = MiningStatus.InProgress;
                            }
                            else
                            {
                                result.MiningStatus = MiningStatus.Completed;
                            }
                        }
                        results.Add(result);
                    }
                }
            }
            return results;
        }

        public async Task<IEnumerable<LicenseLog>?> GetLicensesLogsAsync(Guid? licenseId)
        {
            var query = "getlicenseslogs";
            List<LicenseLog> results = new List<LicenseLog>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid("3d0b7184-f155-4eb4-9f29-0005c99dcd48")); // TODO: to be picked from auth token
                    if(licenseId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId.Value);
                    }

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while(reader.Read())
                    {
                        LicenseLog result = new LicenseLog();
                        result.CustomerId = new Guid(Convert.ToString(reader["customerid"]));
                        result.LicenseId = new Guid(Convert.ToString(reader["licenseid"]));
                        result.Title = Convert.ToString(reader["title"]);
                        result.MiningStartedAt = Convert.ToDateTime(reader["createdat"]);
                        if (reader["minedat"] == DBNull.Value)
                        {
                            result.MiningStatus = MiningStatus.InProgress;
                        }
                        else
                        {
                            result.MiningStatus = MiningStatus.Completed;
                        }
                        result.MinedBy = Convert.ToString(reader["createdbyname"]);
                        results.Add(result);
                    }
                }
            }
            return results;
        }

        public async Task MineAsync(Guid licenseId, string userId)
        {
            var query = "startmining";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId);
                    cmd.Parameters.AddWithValue("mined_by_id", NpgsqlDbType.Uuid, new Guid(userId));
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:Tenant").Value));
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
                    cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid("3d0b7184-f155-4eb4-9f29-0005c99dcd48")); // TODO: to be taken from token later
                    if (conn.State != ConnectionState.Open) conn.Open();

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task EndMiningAsync()
        {
            var query = "finishmining";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:Tenant").Value));
                    if (conn.State != ConnectionState.Open) conn.Open();

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
    }
}