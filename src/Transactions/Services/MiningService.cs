using System.Data;
using Npgsql;
using NpgsqlTypes;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Enums;
using License = CodeRower.CCP.Controllers.Models.License;
using Newtonsoft.Json;

namespace CodeRower.CCP.Services
{
    public interface IMiningService
    {
        Task MineAsync(Guid tenantId, Guid licenseId, string userId);
        Task<IEnumerable<License>?> GetLicensesAsync(Guid tenantId, Guid? licenseId, string customerId);
        Task<License?> GetLicenseByLicenseId(Guid tenantId, Guid userId, Guid licenseId);
        Task ActivateLicenseAsync(Guid tenantId, Guid licenseId, string customerId);
        Task RegisterLicense(Guid tenantId, LicenseRequest data, string customerId, string userId);
        Task<string> AddLicense(Guid tenantId, LicenseBuyRequest data, string userId);
        Task<string> AddPoolLicense(Guid tenantId, LicenseBuyRequest data);
        Task<IEnumerable<License>> EndMiningAsync(Guid tenantId);
        Task<IEnumerable<LicenseLog>> GetLicenseLogsAsync(Guid tenantId, Guid customerId, Guid? licenseId);
    }

    public class MiningService : IMiningService
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;
        private readonly ITransactionsService _transactionsService;
        private readonly IUsersService _userService;
        public MiningService(IConfiguration configuration, ITenantService tenantService,
            ITransactionsService transactionsService, IUsersService userService)
        {
            _configuration = configuration;
            _tenantService = tenantService;
            _transactionsService = transactionsService;
            _userService = userService;
        }

        public async Task<string> AddLicense(Guid tenantId, LicenseBuyRequest data, string userId)
        {
            var query = "addlicense";
            var id = string.Empty;
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("transaction_id", NpgsqlDbType.Text, data.TransactionId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, new Guid(userId));
                    cmd.Parameters.AddWithValue("license_type", NpgsqlDbType.Text, data.LicenseType.ToString());

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

        public async Task<string> AddPoolLicense(Guid tenantId, LicenseBuyRequest data)
        {
            var transaction = await _transactionsService.GetTransactionBookById(tenantId, data.TransactionId).ConfigureAwait(false);

            if (transaction != null)
            {
                var licenseId = await AddLicense(tenantId, data, transaction.UserId).ConfigureAwait(false);

                var customerId = transaction.CustomerId;
                var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);
                var walletTenant = tenantInfo.WalletTenantId;
                var maintenanceFee = tenantInfo.LicenseCost * tenantInfo.MonthlyMaintenancePct / 100;

                var walletTopUp = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = transaction.Amount,
                    IsCredit = true,
                    Reference = $"Payment added to wallet for purchase of License - {licenseId}",
                    PayerId = walletTenant,
                    PayeeId = customerId,
                    TransactionType = "PAYMENT",
                    Currency = Currency.USD,
                    CurrentBalanceFor = customerId,
                    Remark = licenseId
                }).ConfigureAwait(false);


                var buyTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = tenantInfo.LicenseCost,
                    IsCredit = false,
                    Reference = $"Payment added to wallet for purchase of License - {licenseId}",
                    PayerId = customerId,
                    PayeeId = walletTenant,
                    TransactionType = "PURCHASE_LICENSE",
                    Currency = Currency.USD,
                    CurrentBalanceFor = customerId,
                    BaseTransaction = walletTopUp.transactionid,
                    Remark = licenseId
                }).ConfigureAwait(false);

                var maintenanceTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = maintenanceFee,
                    IsCredit = false,
                    Reference = $"Payment received from user {customerId} for purchase of License - {licenseId} , TransactionId - {data.TransactionId}",
                    PayerId = customerId,
                    PayeeId = walletTenant,
                    TransactionType = "MAINTENANCE_FEE",
                    Currency = Currency.USD,
                    CurrentBalanceFor = customerId,
                    BaseTransaction = walletTopUp.transactionid,
                    Remark = licenseId
                }).ConfigureAwait(false);

                var transactionBook = new TransactionBook()
                {
                    TransactionBookId = data.TransactionId,
                    WalletResponse = JsonConvert.SerializeObject(walletTopUp),
                    WalletTransactionStatus = "success"
                };

                await _transactionsService.UpdateTransactionBook(tenantId, transactionBook).ConfigureAwait(false);

                return licenseId;

            }
            return null;
        }

        public async Task RegisterLicense(Guid tenantId, LicenseRequest data, string customerId, string userId)
        {
            var query = "registerlicense";

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("license_number", NpgsqlDbType.Text, data.LicenseNumber);
                    cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid(customerId));
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, new Guid(userId));
                    if (conn.State != ConnectionState.Open) conn.Open();

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);
            var ownerInfo = await _userService.GetUserInfoAsync(tenantId, userId, true);

            if (ownerInfo != null)
            {
                var commissionFee = tenantInfo.LicenseCost * tenantInfo.LicenseCommissionPct / 100;
                var commissionFeeInCoins = commissionFee / tenantInfo.LatestRateInUSD;

                var commissionFeeCreditTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = commissionFeeInCoins,
                    IsCredit = true,
                    Reference = $"Commission for License - {data.LicenseNumber} registered by user",
                    PayerId = tenantInfo.WalletTenantId,
                    PayeeId = ownerInfo.CustomerId,
                    TransactionType = "COMMISSION",
                    Currency = Currency.COINS,
                    CurrentBalanceFor = ownerInfo.CustomerId
                }).ConfigureAwait(false);

                var commissionFeeDebitTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = commissionFeeInCoins,
                    IsCredit = false,
                    Reference = $"Commission for License - {data.LicenseNumber} registered by user",
                    PayerId = ownerInfo.CustomerId,
                    PayeeId = tenantInfo.WalletTenantId,
                    TransactionType = "COMMISSION",
                    Currency = Currency.COINS,
                    CurrentBalanceFor = tenantInfo.WalletTenantId
                }).ConfigureAwait(false);

                var unlockFeeTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = commissionFeeInCoins,
                    IsCredit = true,
                    Reference = $"Commission UNLOCKED for License - {data.LicenseNumber} registered by user",
                    PayerId = tenantInfo.WalletTenantId,
                    PayeeId = ownerInfo.CustomerId,
                    TransactionType = "UNLOCKED",
                    Currency = Currency.COINS,
                    CurrentBalanceFor = ownerInfo.CustomerId
                }).ConfigureAwait(false);
            }
        }
        public async Task<IEnumerable<License>?> GetLicensesAsync(Guid tenantId, Guid? licenseId, string customerId)
        {
            var query = "getlicenses";
            List<License> results = new List<License>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
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
                        result.LicenseType = (LicenseType)Enum.Parse(typeof(LicenseType), Convert.ToString(reader["licenseType"]));
                        result.Title = Convert.ToString(reader["title"]);
                        result.Mined = Convert.ToDecimal(reader["total_mined"]);
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
                        var registeredAt = reader["registeredAt"];
                        if (registeredAt != DBNull.Value)
                        {
                            result.RegisteredAt = Convert.ToDateTime(registeredAt);
                        }
                        var customerLicenseLogId = reader["customerLicenseLogId"];

                        if (result.ActivationDate.HasValue)
                        {
                            if (result.ExpirationDate < DateTime.Now)
                            {
                                result.MiningStatus = MiningStatus.expired;
                            }
                            else if (customerLicenseLogId == DBNull.Value)
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
        public async Task<License?> GetLicenseByLicenseId(Guid tenantId, Guid userId, Guid licenseId)
        {
            var query = "getlicensebyid";
            License result = null;
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, userId);
                    cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        result = new License();

                        result.LicenseId = new Guid(Convert.ToString(reader["licenseid"]));
                        result.LicenseType = (LicenseType)Enum.Parse(typeof(LicenseType), Convert.ToString(reader["licenseType"]));
                        result.LicenseNumber = Convert.ToString(reader["licenseNumber"]);
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
                        var registeredAt = reader["registeredAt"];
                        if (registeredAt != DBNull.Value)
                        {
                            result.RegisteredAt = Convert.ToDateTime(registeredAt);
                        }
                    }
                }
            }
            return result;
        }
        public async Task MineAsync(Guid tenantId, Guid licenseId, string userId)
        {
            var query = "startmining";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId);
                    cmd.Parameters.AddWithValue("mined_by_id", NpgsqlDbType.Uuid, new Guid(userId));
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    if (conn.State != ConnectionState.Open) conn.Open();

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
        public async Task ActivateLicenseAsync(Guid tenantId, Guid licenseId, string customerId)
        {
            var query = "activatelicense";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId);
                    cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid(customerId));
                    if (conn.State != ConnectionState.Open) conn.Open();

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
        public async Task<IEnumerable<License>> EndMiningAsync(Guid tenantId)
        {
            var query = "endmining";
            var minedLicenses = new List<License>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        var minedLicense = new License();
                        minedLicense.CustomerId = new Guid(Convert.ToString(reader["customerid"]));
                        minedLicense.LicenseId = new Guid(Convert.ToString(reader["licenseid"]));
                        minedLicense.LicenseType = (LicenseType)Enum.Parse(typeof(LicenseType), Convert.ToString(reader["licensetype"]));
                        minedLicense.LicenseNumber = Convert.ToString(reader["licenseNumber"]);
                        minedLicenses.Add(minedLicense);
                    }
                }
            }
            return minedLicenses;
        }
        public async Task<IEnumerable<LicenseLog>> GetLicenseLogsAsync(Guid tenantId, Guid customerId, Guid? licenseId)
        {
            var query = "get_license_log_report";
            var licenseLogs = new List<LicenseLog>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, customerId);
                    if (licenseId.HasValue) cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId);
                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        var log = new LicenseLog();
                        log.LicenseId = Guid.Parse(Convert.ToString(reader["id"]));
                        log.Title = Convert.ToString(reader["title"]);
                        log.ActivatedOn = Convert.ToDateTime(reader["activated_on"]);
                        if (reader["expires_on"] != DBNull.Value) log.ExpiresOn = Convert.ToDateTime(reader["expires_on"]);
                        log.Status = Convert.ToString(reader["status"]);
                        log.LicenseType = (LicenseType)Enum.Parse(typeof(LicenseType), Convert.ToString(reader["license_type"]));
                        log.MiningStartedAt = Convert.ToDateTime(reader["created_at"]);
                        if (reader["mined_at"] != DBNull.Value) log.MiningEndedAt = Convert.ToDateTime(reader["mined_at"]);

                        licenseLogs.Add(log);
                    }
                }
            }
            return licenseLogs;
        }
    }
}