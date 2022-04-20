using System.Data;
using System.Net;
using System.Net.Mail;
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

                if (string.IsNullOrEmpty(otp) || (string.IsNullOrEmpty(phoneNumber) && string.IsNullOrEmpty(email)))
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
                    await SendEmail(email, otp, tenantInfo).ConfigureAwait(false);
                }
                catch (Exception)
                {

                }

                return true;
            }
        }

        public async Task<bool> VerifyAsync(Guid tenantId, Guid userId, string otp, string service)
        {
            var query = "verifyotp";
            var verified = false;

            using (NpgsqlConnection conn = new NpgsqlConnection(_configuration.GetSection("AppSettings:ConnectionStrings:Postgres_CCP").Value))
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);
                    cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Uuid, userId);
                    cmd.Parameters.AddWithValue("service_", NpgsqlDbType.Text, service);
                    cmd.Parameters.AddWithValue("otp", NpgsqlDbType.Text, otp);

                    if (conn.State != ConnectionState.Open) conn.Open();
                    verified = (bool)await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                }
            }

            return verified;
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

        //private async Task<string> SendEmail(string email, string otp, TenantInfo tenantInfo)
        //{
        //    var client = new SendGridClient(tenantInfo.SendGridApiKey);
        //    var from = new EmailAddress(tenantInfo.SendGridEmailFrom);
        //    var to = new EmailAddress(email);

        //    var data = new { otp = otp };

        //    var msg = MailHelper.CreateSingleTemplateEmail(from, to, tenantInfo.SendGridOtpTemplateId, data);
        //    var response = await client.SendEmailAsync(msg).ConfigureAwait(false);

        //    return response.IsSuccessStatusCode.ToString();
        //}

        private async Task SendEmail(string email, string otp, TenantInfo tenantInfo)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress(tenantInfo.SendGridEmailFrom);
                message.To.Add(new MailAddress(email));
                message.Subject = "One time password";
                message.IsBodyHtml = true; //to make message body as html  
                message.Body = $@"<p>Hello,</p>
    <p>One time password for your current request is:</p>
    <h1>{otp}</h1>
    <p>If you did not ask to process any request, you can ignore this email.</p>
    <p>Thanks,</p>
    <p>Your CCMT team</p>";
                //smtp.Port = 465;
                smtp.Host = "smtpout.secureserver.net"; //for godaddy  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(tenantInfo.SendGridEmailFrom, tenantInfo.SmtpEmailPassword);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Timeout = 10000;
                smtp.Send(message);
            }
            catch (Exception)
            {

            }

        }

    }
}