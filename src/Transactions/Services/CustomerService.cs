using System.Data;
using CodeRower.CCP.Controllers.Models;
using Npgsql;
using NpgsqlTypes;

namespace CodeRower.CCP.Services
{
    public interface ICustomerService
    {
        Task<CustomerInfo?> GetCustomerInfoAsync(string customerId, string walletAddress);
    }

    public class CustomerService : ICustomerService
    {
        private readonly IConfiguration _configuration;

        public CustomerService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<CustomerInfo?> GetCustomerInfoAsync(string customerId, string walletAddress)
        {
            var query = "get_customer_info";

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                CustomerInfo? customerInfo = null;

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    if (!string.IsNullOrEmpty(customerId))
                        cmd.Parameters.AddWithValue("customer_id", NpgsqlDbType.Uuid, new Guid(customerId));

                    if (!string.IsNullOrEmpty(walletAddress))
                        cmd.Parameters.AddWithValue("wallet_address", NpgsqlDbType.Text, walletAddress);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        customerInfo = new CustomerInfo();
                        customerInfo.Id = Convert.ToString(reader["id"]);
                        customerInfo.UserName = Convert.ToString(reader["userName"]);
                        customerInfo.AutoKYCStatus = Convert.ToString(reader["autoKYCStatus"]);
                        customerInfo.ManualKYCStatus = Convert.ToString(reader["manualKYCStatus"]);
                        customerInfo.UserId = Convert.ToString(reader["userId"]);
                        customerInfo.UserStatus = Convert.ToString(reader["userStatus"]);
                        customerInfo.WalletAddress = Convert.ToString(reader["walletAddress"]);
                        customerInfo.CoinPaymentsAddress = Convert.ToString(reader["coinpaymentsAddress"]);
                        customerInfo.BankAccountNumber = Convert.ToString(reader["bankAccountNumber"]);
                        customerInfo.Bank = Convert.ToString(reader["bank"]);
                        customerInfo.IFSC = Convert.ToString(reader["ifsc"]);
                        customerInfo.Swift = Convert.ToString(reader["swift"]);

                    }
                }
                return customerInfo;
            }
        }
    }
}