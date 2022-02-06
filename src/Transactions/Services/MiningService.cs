using System.Data;
using Npgsql;
using NpgsqlTypes;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Enums;
using License = CodeRower.CCP.Controllers.Models.License;

namespace Transactions.Services
{
    public interface IMiningService
    {
        Task MineAsync(Guid licenseId, string userId);
        Task<IEnumerable<License>?> GetLicensesAsync(Guid? licenseId, string customerId);
        Task ActivateLicenseAsync(Guid licenseId, string customerId);
        Task RegisterLicense(LicenseRequest data,string customerId, string userId);
        Task<string> AddLicense(LicenseBuyRequest data, string userId);
        Task EndMiningAsync();
    }

    public class MiningService : IMiningService
    {
        private readonly IConfiguration _configuration;

        public MiningService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> AddLicense(LicenseBuyRequest data, string userId)
        {
            var query = "addlicense";
            var id = string.Empty;
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:Tenant").Value));
                    cmd.Parameters.AddWithValue("transaction_id", NpgsqlDbType.Text, data.TransactionId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, new Guid(userId));
                    if (conn.State != ConnectionState.Open) conn.Open();

                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        id = Convert.ToString(reader["licenseId"]);
                    }
                }

            }
            return id;

        }

        public async Task RegisterLicense(LicenseRequest data, string customerId, string userId)
        {
            var query = "registerlicense";

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, data.LicenseId);
                    cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid(customerId));
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(_configuration.GetSection("AppSettings:Tenant").Value));
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, new Guid(userId));
                    if (conn.State != ConnectionState.Open) conn.Open();

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<IEnumerable<License>?> GetLicensesAsync(Guid? licenseId, string customerId)
        {
            var query = "getlicenses_v2";
            List<License> results = new List<License>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid(customerId));
                    if (licenseId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId.Value);
                    }

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        License result = new License();
                        result.CustomerId = new Guid(Convert.ToString(reader["customerid"]));
                        result.LicenseId = new Guid(Convert.ToString(reader["licenseid"]));
                        result.Title = Convert.ToString(reader["title"]);
                        result.Mined = Convert.ToInt32(reader["total_mined"]);
                        var activatedOn = reader["activatedon"];
                        if (activatedOn != DBNull.Value)
                        {
                            result.ActivationDate = Convert.ToDateTime(activatedOn);
                        }
                        var expiresOn = reader["expireson"];
                        if (expiresOn != DBNull.Value)
                        {
                            result.ExpirationDate = Convert.ToDateTime(expiresOn);
                        }
                        var customerLicenseLogId = reader["customerLicenseLogId"];

                        if (result.ActivationDate.HasValue)
                        {
                            if (result.ExpirationDate < DateTime.Now)
                            {
                                result.MiningStatus = MiningStatus.expired;
                            }
                            else if(customerLicenseLogId == DBNull.Value)
                            {
                                result.MiningStatus = MiningStatus.completed;
                            }
                            else if (reader["minedat"] == DBNull.Value)
                            {
                                result.MiningStatus = MiningStatus.in_progress;
                            }
                            else
                            {
                                result.MiningStatus = MiningStatus.completed;
                            }
                        }
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

        public async Task ActivateLicenseAsync(Guid licenseId, string customerId)
        {
            var query = "activatelicense";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId);
                    cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid(customerId));
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