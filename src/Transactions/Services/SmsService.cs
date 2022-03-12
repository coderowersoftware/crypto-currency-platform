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
        Task<bool> SendAsync(Guid tenantId, Guid userId);
        Task<bool> VerifyAsync(Guid tenantId, Guid userId, string otp);
    }

    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;

        public SmsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendAsync(Guid tenantId, Guid userId)
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
                    cmd.Parameters.AddWithValue("service_", NpgsqlDbType.Text, "twilio");

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

                var status = SendTwilioMessage(phoneNumber, otp);

                return true;
            }
        }

        public async Task<bool> VerifyAsync(Guid tenantId, Guid userId, string otp)
        {
            var query = "getotp";
            var sentOtp = string.Empty;

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, userId);

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

        private string SendTwilioMessage(string phoneNumber, string otp)
        {
            var accountSid = "ACdcfcdbc2ac4877a905337958845334e8";
            var authToken = "a66756f55d1aec61ee1a69fdf05b830d";
            TwilioClient.Init(accountSid, authToken);

            var messageOptions = new CreateMessageOptions(
                new PhoneNumber(phoneNumber));
            messageOptions.MessagingServiceSid = "MG85c3ff083d19790afd098825e75181f8";
            messageOptions.Body = $"Your Cloud Chain verification code is {otp}";

            var message = MessageResource.Create(messageOptions);

            return message?.Status.ToString();
        }

    }
}