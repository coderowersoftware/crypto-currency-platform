using System.Data;
using Npgsql;
using NpgsqlTypes;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Enums;
using License = CodeRower.CCP.Controllers.Models.License;
using Newtonsoft.Json;
using Transactions.Domain.Models;

namespace CodeRower.CCP.Services
{
    public interface IMiningService
    {
        Task<License> MineAsync(Guid tenantId, Guid licenseId, string userId);
        Task<IEnumerable<License>?> GetLicensesAsync(Guid tenantId, Guid? licenseId, string customerId);
        Task<License?> GetLicenseByLicenseId(Guid tenantId, Guid userId, Guid licenseId);
        Task<License> ActivateLicenseAsync(Guid tenantId, Guid licenseId, string customerId);
        Task<License> RegisterLicense(Guid tenantId, LicenseRequest data, string customerId, string userId);
        Task<string> AddLicense(Guid tenantId, LicenseBuyRequest data, string userId);
        Task<string> AddPoolLicense(Guid tenantId, LicenseBuyRequest data);
        Task<IEnumerable<License>> EndMiningAsync(Guid tenantId);
        Task<IEnumerable<LicenseLog>> GetLicenseLogsAsync(Guid tenantId, Guid customerId, Guid? licenseId);
        Task<IEnumerable<License>?> GetAllRegisteredLicensesAsync(Guid tenantId);
    }

    public class MiningService : IMiningService
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;
        private readonly ITransactionsService _transactionsService;
        private readonly IAuditLogService _auditLogService;
        public MiningService(IConfiguration configuration, ITenantService tenantService,
            ITransactionsService transactionsService, IAuditLogService auditLogService)
        {
            _configuration = configuration;
            _tenantService = tenantService;
            _transactionsService = transactionsService;
            _auditLogService = auditLogService;
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
            var transaction = await _transactionsService.GetTransactionBookById(tenantId, new Guid(data.TransactionId)).ConfigureAwait(false);

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
                    Currency = Controllers.Models.Enums.Currency.USD,
                    CurrentBalanceFor = customerId,
                    Remark = licenseId
                }).ConfigureAwait(false);


                var buyTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = tenantInfo.LicenseCost,
                    IsCredit = false,
                    Reference = $"Payment for purchase of License - {licenseId}",
                    PayerId = customerId,
                    PayeeId = walletTenant,
                    TransactionType = "PURCHASE_LICENSE",
                    Currency = Controllers.Models.Enums.Currency.USD,
                    CurrentBalanceFor = customerId,
                    BaseTransaction = walletTopUp.transactionid,
                    Remark = licenseId
                }).ConfigureAwait(false);

                var maintenanceTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = maintenanceFee,
                    IsCredit = false,
                    Reference = $"Maintenance fee from user {customerId} for License - {licenseId} , TransactionId - {data.TransactionId}",
                    PayerId = customerId,
                    PayeeId = walletTenant,
                    TransactionType = "MAINTENANCE_FEE",
                    Currency = Controllers.Models.Enums.Currency.USD,
                    CurrentBalanceFor = customerId,
                    BaseTransaction = walletTopUp.transactionid,
                    Remark = licenseId
                }).ConfigureAwait(false);

                var transactionBook = new TransactionBook()
                {
                    TransactionBookId = new Guid(data.TransactionId),
                    WalletResponse = JsonConvert.SerializeObject(walletTopUp),
                    WalletTransactionStatus = "success"
                };

                await _transactionsService.UpdateTransactionBook(tenantId, transactionBook).ConfigureAwait(false);

                return licenseId;

            }
            return null;
        }

        public async Task<License> RegisterLicense(Guid tenantId, LicenseRequest data, string customerId, string userId)
        {
            License result = null;
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

                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    while (reader.Read())
                    {
                        result = MapLicense(reader);
                    }
                }
            }

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            var commissionFeeCreditTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
            {
                Amount = 0,
                IsCredit = true,
                Reference = $"License - {data.LicenseNumber} registered",
                PayerId = tenantInfo.WalletTenantId,
                PayeeId = customerId,
                TransactionType = "REGISTER_LICENSE",
                Currency = Controllers.Models.Enums.Currency.COINS,
                CurrentBalanceFor = customerId,
                Service = "PURCHASE_LICENSE",
                Provider = "ANY",
                Vendor = "CCC",
                ProductId = data.LicenseNumber,
                ExecuteCommissionFor = customerId,
                Remark = $"ExchangeRate @ {tenantInfo.LatestRateInUSD}",
                ExecuteCommissionAmount = tenantInfo.LicenseCost / tenantInfo.LatestRateInUSD

            }).ConfigureAwait(false);

            return result;
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

                        results.Add(MapLicense(reader));
                    }
                }
            }
            return results;
        }
        private License MapLicense(NpgsqlDataReader reader)
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

            return result;
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
        public async Task<License> MineAsync(Guid tenantId, Guid licenseId, string userId)
        {
            License result = null;
            var query = "startmining";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId);
                    cmd.Parameters.AddWithValue("mined_by_id", NpgsqlDbType.Uuid, new Guid(userId));
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    if (conn.State != ConnectionState.Open) conn.Open();

                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    while (reader.Read())
                    {
                        result = MapLicense(reader);
                    }
                }
            }

            return result;
        }
        public async Task<License> ActivateLicenseAsync(Guid tenantId, Guid licenseId, string customerId)
        {
            License result = null;
            var query = "activatelicense";
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("license_id", NpgsqlDbType.Uuid, licenseId);
                    cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid(customerId));
                    if (conn.State != ConnectionState.Open) conn.Open();

                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    while (reader.Read())
                    {
                        result = MapLicense(reader);
                    }
                }
            }

            return result;
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
        public async Task<IEnumerable<License>?> GetAllRegisteredLicensesAsync(Guid tenantId)
        {
            var query = "getallregisteredlicenses";
            List<License> results = new List<License>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        License result = new License();
                        result.CustomerId = new Guid(Convert.ToString(reader["customerid"]));
                        result.CustomerName = Convert.ToString(reader["customername"]);
                        result.LicenseId = new Guid(Convert.ToString(reader["licenseid"]));
                        result.LicenseNumber = Convert.ToString(reader["licenseNumber"]);
                        result.ParentCustomerId = Convert.ToString(reader["parentcustomerid"]);
                        result.ParentCustomerName = Convert.ToString(reader["parentcustomername"]);
                        result.RegisteredAt = Convert.ToDateTime(reader["registeredAt"]);

                        results.Add(result);
                    }
                }
            }

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            var exchangeRates = await GetExchangeRates(tenantId);

            foreach (var item in results)
            {
                try
                {
                    var id = await InsertTransaction(item, exchangeRates, tenantInfo);
                    //var id = await SyncCommission(item, exchangeRates, tenantInfo);
                    var str = string.Empty;
                }
                catch(Exception ex)
                {

                }


            }

            return results;
        }

        private async Task<string> SyncCommission(License item, IEnumerable<ExchangeRate> exchangeRates, TenantInfo tenantInfo)
        {
            var query1 = "cloudchaintechnology_synccommission";
            string transactionId = null;
            var exchangeRate = exchangeRates.FirstOrDefault(rate => rate.CreatedAt <= item.RegisteredAt)?.ValueInUSD ?? 1;

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration["AppSettings:ConnectionStrings:Postgres_WALLET"]))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query1, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(tenantInfo.WalletTenantId));
                    cmd.Parameters.AddWithValue("currency_", NpgsqlDbType.Unknown, "COINS");
                    cmd.Parameters.AddWithValue("amount_", NpgsqlDbType.Numeric, 15 / exchangeRate);
                    cmd.Parameters.AddWithValue("transactiontype_identifier", NpgsqlDbType.Text, "COMMISSION");
                    cmd.Parameters.AddWithValue("is_credit", NpgsqlDbType.Boolean, true);
                    cmd.Parameters.AddWithValue("reference_", NpgsqlDbType.Varchar, $"Commission for License - {item.LicenseNumber} registered by user");
                    cmd.Parameters.AddWithValue("created_by_id", NpgsqlDbType.Uuid, new Guid("e4b592dc-9076-4b09-a65f-344a88371af2"));
                    cmd.Parameters.AddWithValue("created_at", NpgsqlDbType.TimestampTz, item.RegisteredAt);
                    cmd.Parameters.AddWithValue("updated_by_id", NpgsqlDbType.Uuid, new Guid("e4b592dc-9076-4b09-a65f-344a88371af2"));
                    cmd.Parameters.AddWithValue("updated_at", NpgsqlDbType.TimestampTz, item.RegisteredAt);
                    cmd.Parameters.AddWithValue("payer_id", NpgsqlDbType.Varchar, tenantInfo.WalletTenantId);
                    cmd.Parameters.AddWithValue("remark_", NpgsqlDbType.Varchar, $"ExchangeRate @ {exchangeRate}");

                    if (!string.IsNullOrWhiteSpace(item.ParentCustomerId))
                    {
                        cmd.Parameters.AddWithValue("payee_id", NpgsqlDbType.Varchar, item.ParentCustomerId);
                        cmd.Parameters.AddWithValue("payee_name", NpgsqlDbType.Varchar, item.ParentCustomerName);
                    }

                    cmd.Parameters.AddWithValue("current_balance_for", NpgsqlDbType.Varchar, item.ParentCustomerId);
                    cmd.Parameters.AddWithValue("onbehalfof_id", NpgsqlDbType.Varchar, item.CustomerId.ToString());
                    cmd.Parameters.AddWithValue("onbehalfof_name", NpgsqlDbType.Varchar, item.CustomerName);
                    cmd.Parameters.AddWithValue("additional_data", NpgsqlDbType.Varchar, "DataCorrection-Prod");
                    cmd.Parameters.AddWithValue("product_id", NpgsqlDbType.Varchar, item.LicenseNumber);
                    //cmd.Parameters.AddWithValue("service_", NpgsqlDbType.Text, "");
                    //cmd.Parameters.AddWithValue("vendor_", NpgsqlDbType.Text, "PAYPOINT");
                    //cmd.Parameters.AddWithValue("provider_", NpgsqlDbType.Text, "BANKS");


                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        transactionId = Convert.ToString(reader["transactionid"]);
                    }
                }

            }

            return transactionId;
        }

        private async Task<string> InsertTransaction(License item, IEnumerable<ExchangeRate> exchangeRates, TenantInfo tenantInfo)
        {
            var query1 = "cloudchaintechnology_inserttransaction";
            string transactionId = null;
            var exchangeRate = exchangeRates.FirstOrDefault(rate => rate.CreatedAt <= item.RegisteredAt)?.ValueInUSD ?? 1;

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration["AppSettings:ConnectionStrings:Postgres_WALLET"]))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query1, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, new Guid(tenantInfo.WalletTenantId));
                    cmd.Parameters.AddWithValue("currency_", NpgsqlDbType.Unknown, "COINS");
                    cmd.Parameters.AddWithValue("amount_", NpgsqlDbType.Numeric, 0);
                    cmd.Parameters.AddWithValue("transactiontype_identifier", NpgsqlDbType.Text, "REGISTER_LICENSE");
                    cmd.Parameters.AddWithValue("is_credit", NpgsqlDbType.Boolean, true);
                    cmd.Parameters.AddWithValue("reference_", NpgsqlDbType.Varchar, $"License - {item.LicenseNumber} registered");
                    cmd.Parameters.AddWithValue("created_by_id", NpgsqlDbType.Uuid, new Guid("e4b592dc-9076-4b09-a65f-344a88371af2"));
                    cmd.Parameters.AddWithValue("created_at", NpgsqlDbType.TimestampTz, item.RegisteredAt);
                    cmd.Parameters.AddWithValue("updated_by_id", NpgsqlDbType.Uuid, new Guid("e4b592dc-9076-4b09-a65f-344a88371af2"));
                    cmd.Parameters.AddWithValue("updated_at", NpgsqlDbType.TimestampTz, item.RegisteredAt);
                    cmd.Parameters.AddWithValue("payer_id", NpgsqlDbType.Varchar, tenantInfo.WalletTenantId);
                    cmd.Parameters.AddWithValue("remark_", NpgsqlDbType.Varchar, $"ExchangeRate @ {exchangeRate}");
                    cmd.Parameters.AddWithValue("payee_id", NpgsqlDbType.Varchar, item.CustomerId.ToString());
                    cmd.Parameters.AddWithValue("payee_name", NpgsqlDbType.Varchar, item.CustomerName);

                    cmd.Parameters.AddWithValue("current_balance_for", NpgsqlDbType.Varchar, item.CustomerId.ToString());
                    cmd.Parameters.AddWithValue("onbehalfof_id", NpgsqlDbType.Varchar, item.CustomerId.ToString());
                    cmd.Parameters.AddWithValue("onbehalfof_name", NpgsqlDbType.Varchar, item.CustomerName);
                    cmd.Parameters.AddWithValue("additional_data", NpgsqlDbType.Varchar, "DataCorrection-Prod-30-5");
                    cmd.Parameters.AddWithValue("product_id", NpgsqlDbType.Varchar, item.LicenseNumber);
                    cmd.Parameters.AddWithValue("service_", NpgsqlDbType.Text, "PURCHASE_LICENSE");
                    cmd.Parameters.AddWithValue("vendor_", NpgsqlDbType.Text, "CCC");
                    cmd.Parameters.AddWithValue("provider_", NpgsqlDbType.Text, "ANY");
                    cmd.Parameters.AddWithValue("execute_commission_for", NpgsqlDbType.Text, item.CustomerId.ToString());
                    cmd.Parameters.AddWithValue("execute_commission_amount", NpgsqlDbType.Numeric, tenantInfo.LicenseCost / exchangeRate);


                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        transactionId = Convert.ToString(reader["transactionid"]);
                    }
                }

            }

            return transactionId;
        }
        public async Task<IEnumerable<ExchangeRate>> GetExchangeRates(Guid tenantId)
        {
            var query = "getexchangerates";
            List<ExchangeRate> results = new List<ExchangeRate>();
            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        ExchangeRate result = new ExchangeRate();
                        result.Id = new Guid(Convert.ToString(reader["id"]));
                        result.ValueInUSD = Convert.ToDecimal(reader["valueInUSD"]);
                        result.CreatedAt = Convert.ToDateTime(reader["createdAt"]);
                        result.CreatedById = Convert.ToString(reader["createdById"]);

                        results.Add(result);
                    }
                }
            }

            return results;
        }
    }
}