using System.Data;
using Npgsql;
using NpgsqlTypes;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CodeRower.CCP.Services
{
    public interface ISmsService
    {
        Task<bool> SendAsync(Guid tenantId, Guid userId, string service);
        Task<bool> VerifyAsync(Guid tenantId, Guid userId, string otp, string service);
    }

    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;
       
        public SmsService(IConfiguration configuration, ITenantService tenantService)
        {
            _configuration = configuration;
            _tenantService = tenantService;
        }

        public async Task<bool> SendAsync(Guid tenantId, Guid userId, string service)
        {
            var query = "sendotp";

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                string otp = null;
                string phoneNumber = null;

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, userId);
                    cmd.Parameters.AddWithValue("service_", NpgsqlDbType.Text, service);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        phoneNumber = Convert.ToString(reader["mobile"]);
                        otp = Convert.ToString(reader["otp"]);
                    }
                }

                if (string.IsNullOrEmpty(phoneNumber))
                    return false;

                var status = await SendTwilioMessage(tenantId, phoneNumber, otp).ConfigureAwait(false);

                return true;
            }
        }

        public async Task<bool> VerifyAsync(Guid tenantId, Guid userId, string otp, string service)
        {
            var query = "getotp";
            var sentOtp = string.Empty;

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, userId);
                    cmd.Parameters.AddWithValue("service_", NpgsqlDbType.Text, service);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

                    while (reader.Read())
                    {
                        sentOtp = Convert.ToString(reader["otp"]);
                    }
                }
            }

            return otp == sentOtp;
        }

        private async Task<string> SendTwilioMessage(Guid tenantId, string phoneNumber, string otp)
        {
            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

            TwilioClient.Init(tenantInfo.TwilioAccountSID, tenantInfo.TwilioAuthToken);

            var messageOptions = new CreateMessageOptions(
                new PhoneNumber(phoneNumber));
            messageOptions.MessagingServiceSid = tenantInfo.TwilioMessageServiceId;
            messageOptions.Body = $"Your Cloud Chain verification code is {otp}";

            var message = MessageResource.Create(messageOptions);

            return message?.Status.ToString();
        }

    }
}