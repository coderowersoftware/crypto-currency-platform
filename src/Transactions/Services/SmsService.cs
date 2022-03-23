using System.Data;
using CodeRower.CCP.Controllers.Models;
using Npgsql;
using NpgsqlTypes;
using SendGrid;
using SendGrid.Helpers.Mail;
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
                string email = null;

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
                        email = Convert.ToString(reader["email"]);
                        otp = Convert.ToString(reader["otp"]);
                    }
                }

                if (string.IsNullOrEmpty(phoneNumber))
                    return false;

                var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);

                try
                {
                    var smsStatus = await SendTwilioMessage(phoneNumber, otp, tenantInfo).ConfigureAwait(false);
                }
                catch (Exception)
                {

                }
                try
                {
                    var emailStatus = await SendEmail(email, otp, tenantInfo).ConfigureAwait(false);
                }
                catch (Exception)
                {

                }

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

        private async Task<string> SendTwilioMessage(string phoneNumber, string otp, TenantInfo tenantInfo)
        {

            TwilioClient.Init(tenantInfo.TwilioAccountSID, tenantInfo.TwilioAuthToken);

            var messageOptions = new CreateMessageOptions(
                new PhoneNumber(phoneNumber));
            messageOptions.MessagingServiceSid = tenantInfo.TwilioMessageServiceId;
            messageOptions.Body = $"Your Cloud Chain Mining Platform one time verification code is {otp}";

            var message = await MessageResource.CreateAsync(messageOptions).ConfigureAwait(false);

            return message?.Status.ToString();
        }

        private async Task<string> SendEmail(string email, string otp, TenantInfo tenantInfo)
        {
            var client = new SendGridClient(tenantInfo.SendGridApiKey);
            var from = new EmailAddress("no-reply@cloudchainwallet.org");
            var to = new EmailAddress(email);

            var data = new { otp = otp };

            var msg = MailHelper.CreateSingleTemplateEmail(from, to, tenantInfo.SendGridOtpTemplateId, data);
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);

            return response.IsSuccessStatusCode.ToString();
        }

    }
}