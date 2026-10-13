using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AgileConnect.Controllers
{
    [Authorize]

    public class ARMInstanceController : Controller
    {
        private string ARMConfigFilePath;
        private readonly IRedisHelper _redis;
        private readonly IConfiguration _config;
        private readonly DataContext _context;
        public ARMInstanceController(IConfiguration config, IRedisHelper redis, DataContext context)
        {
            _config = config;
            _context = context;
            ARMConfigFilePath = _config["ARMConfigFilePath"];
            _redis = redis;

        }


        public ActionResult Dashboard()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int instanceId)
        {
            try
            {
                var serviceLog = await _context.ARMServiceLogs.FirstOrDefaultAsync(x => x.InstanceID == instanceId);

                if (serviceLog == null)
                {
                    return Json(new { success = false, message = "Record not found." });
                }

                _context.ARMServiceLogs.Remove(serviceLog);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during delete action: {ex.Message}");
                return Json(new { success = false, message = $"An error occurred while deleting the record: {ex.Message}" });
            }
        }




        public ActionResult service()
        {
            var ServiceList = _context.ARMServiceLogs.ToList();
            return View(ServiceList);
        }

        public ActionResult Configure()
        {
            if (System.IO.File.Exists(ARMConfigFilePath))
            {
                string strJSON = String.Empty;

                var fileStream = new FileStream(ARMConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))

                {
                    strJSON = streamReader.ReadToEnd();
                    var jObject = JObject.Parse(strJSON);

                    var keyData = new
                    {
                        keyInterval = jObject["KeyData"]?["KeyInterval"]?.ToString(),
                        privateKey = jObject["KeyData"]?["PrivateKey"]?.ToString()
                    };



                    var emailGateway = new
                    {
                        From = jObject["emailGateway"]?["From"]?.ToString(),
                        SmtpServer = jObject["emailGateway"]?["SmtpServer"]?.ToString(),
                        Port = jObject["emailGateway"]?["Port"]?.ToString(),
                        Username = jObject["emailGateway"]?["Username"]?.ToString(),
                        Password = jObject["emailGateway"]?["Password"]?.ToString(),
                        Server = jObject["emailGateway"]?["Server"]?.ToString()

                    };


                    var smsGateway = new
                    {
                        userto = jObject["SMSGateway"]?["userto"]?.ToString(),
                        userfrom = jObject["SMSGateway"]?["userfrom"]?.ToString(),
                        message = jObject["SMSGateway"]?["message"]?.ToString()

                    };


                    var whatsappLogin = new
                    {
                        Mobile = jObject["WhatsappLogin"]?["Mobile"]?.ToString(),
                        AuthCode = jObject["WhatsappLogin"]?["AuthCode"]?.ToString()

                    };

                    var adminInstance = new
                    {
                        admin = jObject["AdminInstance"]?["admin"]?.ToString(),
                        password = jObject["AdminInstance"]?["Password"]?.ToString(),
                        adminEmailId = jObject["AdminInstance"]?["AdminEmailId"]?.ToString()

                    };


                    var ARM = new
                    {
                        EmailGateway = emailGateway,
                        SMSGateway = smsGateway,
                        WhatsappLogin = whatsappLogin,
                        AdminInstance = adminInstance,
                        KeyData = keyData

                    };


                    var decrStrJSON = JsonConvert.SerializeObject(ARM, Formatting.Indented);
                    ViewBag.ARMText = decrStrJSON ?? String.Empty;

                }
            }
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Configure(ARMModel model)
        {
            var privateKey = RefreshKey(16);
            var emailGateway = new
            {
                From = model.From,
                SmtpServer = model.SmtpServer,
                Port = model.Portno,
                Username = model.Username,
                Password = model.Password,
                Server = model.Server
            };


            var smsGateway = new
            {
                userto = model.UserTo,
                userfrom = model.UserFrom,
                message = model.Message

            };

            var whatsappLogin = new
            {
                Mobile = model.MobileNo,
                AuthCode = model.AuthCode

            };

            var adminInstance = new
            {
                admin = model.Admin,
                Password = model.loginPassword,
                AdminEmailId = model.AdminEmailId

            };
            var keyData = new
            {
                KeyInterval = model.KeyInterval,
                PrivateKey = privateKey
            };


            var messageObjToLog = new
            {
                emailGateway = emailGateway,
                SMSGateway = smsGateway,
                WhatsappLogin = whatsappLogin,
                AdminInstance = adminInstance,
                KeyData = keyData
            };

            StreamWriter sw = new StreamWriter(ARMConfigFilePath);
            var messages = JsonConvert.SerializeObject(messageObjToLog, Formatting.Indented);
            //IDatabase db = _redis.GetDatabase();

            //set value in redis 
            var key = "ARM-INSTANCE-CONFIG";
            await _redis.StringSetAsync(key, messages);
            sw.WriteLine(messages);

            //Close the file
            sw.Close();

            return RedirectToAction("Configure");
        }

        public static string RefreshKey(int size)
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

            byte[] data = new byte[4 * size];
            using (var crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }

    }




}