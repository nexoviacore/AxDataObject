using ARMCommon.Interface;
using ARMCommon.Model;
using ARMCommon.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Mail;
using System.Net;
using System.Web.Http;



namespace ARMCommon.Helpers
{
    public class NotificationHelper : INotificationHelper
    {
        private readonly IAPI _api;
        private readonly IConfiguration _configuration;
        private readonly Utils _utils;
        private string ARMConfigFilePath;
        private string smtpServer;
        private string Port;
        private string smtpUsername;
        private string smtppassword;
        public NotificationHelper(IAPI api, IConfiguration configuration, Utils utils)
        {
            _api = api;
            _configuration = configuration;
            _utils = utils;
            ARMConfigFilePath = _configuration["ARMConfigFilePath"];
            smtpServer = _configuration["EmailConfiguration:SmtpServer"];
            Port = _configuration["EmailConfiguration:Port"];
            smtpUsername = _configuration["EmailConfiguration:Username"];
            smtppassword = _configuration["EmailConfiguration:Password"];
        }
        public string GenerateOTP()
        {
            Random generator = new Random();
            string otp = generator.Next(0, 1000000).ToString("D6");
            return otp;
        }

    public async Task<object> SendEmailOTP(string senderEmail, string otp, string username)
        {
            try
            {
                string subject = "Forgot Password OTP";
                string body = $"Hi {username},\n\nYour password reset request has been received. To complete the process, please use the following One Time Password (OTP):\n\nOTP: {otp}\n\nPlease login immediately and change the password of your choice for Security purpose.\n\nBest regards,\nSupport Team";

               using (SmtpClient smtpClient = new SmtpClient(smtpServer, int.Parse(Port)))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtppassword);

                    using (MailMessage mailMessage = new MailMessage(smtpUsername, senderEmail))
                    {
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                       await smtpClient.SendMailAsync(mailMessage);
                    }
                }

                return new { status = true, message = "Notification Sent Successfully" };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
                return new { status = false, message = "Failed to send notification: " + ex.Message };
            }
        }







        public static EmailConfiguration GetEmailConfiguration(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                string fileContents = File.ReadAllText(filePath);
                var jObject = JObject.Parse(fileContents);
                var emailGateway = jObject["emailGateway"];



                if (emailGateway != null)
                {
                    EmailConfiguration emailConfig = emailGateway.ToObject<EmailConfiguration>();
                    return emailConfig;
                }
                else
                {
                    throw new Exception("emailGateway section not found in the JSON content.");
                }
            }
            else
            {
                throw new FileNotFoundException("File not found.", filePath);
            }
        }





    }
}
