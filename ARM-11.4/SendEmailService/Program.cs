using ARMCommon.Helpers;
using ARMCommon.Interface;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Net;
using System.Net.Mail;


namespace SendEmailService
{
    public class Program
    {
        private static IConfiguration configuration;
        private static string smtpServer;
        private static string port;
        private static string smtpUsername;
        private static string smtpPassword;
        private static Timer timer;
        private static int intervalInSeconds;
        private static string dbtype;

        static async Task Main(string[] args)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                configuration = builder.Build();

                smtpServer = configuration["EmailConfiguration:SmtpServer"];
                port = configuration["EmailConfiguration:Port"];
                smtpUsername = configuration["EmailConfiguration:Username"];
                smtpPassword = configuration["EmailConfiguration:Password"];
                string connectionString = configuration.GetConnectionString("WebApiDatabase");
                string dbtype = configuration["AppConfig:DbType"];
                intervalInSeconds = int.Parse(configuration["AppConfig:Interval"]);
                TimeSpan interval = TimeSpan.FromSeconds(intervalInSeconds);

                timer = new Timer(async _ => await SendEmails(connectionString, dbtype), null, TimeSpan.Zero, interval);


                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Main: {ex.Message}\n{ex.StackTrace}");
            }
        }
        private static async Task SendEmails(string connectionString, string dbtype)
        {
            try
            {
                string sql = configuration["AppConfig:Query"];
                var dataTable = await GetDBDetails(connectionString, sql, dbtype);

                bool emailsSent = false;

                foreach (DataRow row in dataTable.Rows)
                {
                    try
                    {
                        int jobId = Convert.ToInt32(row["jobid"]);
                        int currentStatus = Convert.ToInt32(row["status"]);

                        if (currentStatus == 0)
                        {
                            string to = row["mailto"].ToString();
                            string subject = row["subject"].ToString();
                            string body = row["body"].ToString();
                            string CC = row["mailcc"].ToString();

                            var emailResult = await SendEmail(to, subject, body, CC);
                            int status = emailResult.status ? 1 : 0;
                            string message = emailResult.message;

                            await UpdateEmailStatus(connectionString, jobId, status, message, dbtype);

                            Console.WriteLine(emailResult.status ? $"Email sent to {to}." : $"Failed to send email to {to}: {message}");
                            emailsSent = emailsSent || emailResult.status;

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing row for jobId: {row["jobid"]}. Error: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                if (!emailsSent)
                {
                    Console.WriteLine("No records to send emails.");
                }

                TimeSpan interval = TimeSpan.FromSeconds(intervalInSeconds);
                var nextOccurrence = DateTime.Now.Add(interval);
                Console.WriteLine($"Next Job scheduled for: {nextOccurrence.ToShortDateString()} - {nextOccurrence:hh:mm:ss tt}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendEmails: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static async Task<EmailResult> SendEmail(string to, string subject, string body, string CC)
        {
            try
            {
                using (SmtpClient smtpClient = new SmtpClient(smtpServer, int.Parse(port)))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    using (MailMessage mailMessage = new MailMessage(smtpUsername, to))
                    {
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = true;

                        if (!string.IsNullOrEmpty(CC))
                        {
                            mailMessage.CC.Add(CC);
                        }

                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }

                return new EmailResult { status = true, message = "Email Sent Successfully." };
            }
            catch (SmtpFailedRecipientException ex)
            {
                Console.WriteLine($"Error sending email to {to}: {ex.Message}\n{ex.StackTrace}");
                return new EmailResult { status = false, message = $"Recipient error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error sending email to {to}: {ex.Message}\n{ex.StackTrace}");
                return new EmailResult { status = false, message = $"General error: {ex.Message}" };
            }
        }


        static async Task<DataTable> GetDBDetails(string connectionString, string sql, string dbType)
        {
            try
            {
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                DataTable dataTable = await dbHelper.ExecuteQueryAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                return dataTable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDBDetails: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }


        private static async Task UpdateEmailStatus(string connectionString, int jobId, int status, string message, string dbtype)
        {
            try
            {
                string sqlUpdate = "UPDATE axp_mailjobs SET status = @status, errormessage = @errormessage WHERE jobid = @jobid";

                IDbHelper dbHelper = DBHelper.CreateDbHelper(sqlUpdate, dbtype, connectionString,
                    new string[] { "@status", "@errormessage", "@jobid" },
                    new DbType[] { DbType.Int32, DbType.String, DbType.Int32 },
                    new object[] { status, message, jobId });

                await dbHelper.ExecuteNonQueryAsync(sqlUpdate, connectionString,
                    new string[] { "@status", "@errormessage", "@jobid" },
                    new DbType[] { DbType.Int32, DbType.String, DbType.Int32 },
                    new object[] { status, message, jobId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating email status for jobId {jobId}: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    public class EmailResult
    {
        public bool status { get; set; }
        public string message { get; set; }
    }
}

