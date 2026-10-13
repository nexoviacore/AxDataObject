
using ARMCommon.ActionFilter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using ARMCommon.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NPOI.OpenXmlFormats.Spreadsheet;
using System.Dynamic;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class ARMNotifyController : ControllerBase
    {


        private readonly IEmailSender _emailSender;
        private DataContext _context;
        private readonly IRedisHelper _redis;
        private readonly IConfiguration _config;
        private readonly Utils _utils;

        public ARMNotifyController(IEmailSender emailSender, DataContext context, IRedisHelper redis,  IConfiguration config, Utils utils)
        {
            _emailSender = emailSender;
            _context = context;
            _redis = redis;
            _config = config;
            _utils = utils;
        }

        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMNotify")]
        public async Task<IActionResult> Post(ARMNotify model)
        { 
            string strJSON = String.Empty;
             
            var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new ExpandoObjectConverter());
            jsonSettings.Converters.Add(new StringEnumConverter());
            dynamic config = JsonConvert.DeserializeObject<ExpandoObject>(json, jsonSettings);
            config.ParallelTasksCount = 25;
            var newJson = JsonConvert.SerializeObject(config, Formatting.Indented, jsonSettings);
            System.IO.File.WriteAllText(appSettingsPath, newJson);
               


            //Replacing content of Template String and sending email.

            var TemplateId = model.TemplateId;
            var Notifydata = model.NotificationData?.ElementAt(0).Value;
            var resultdata = _context.NotificationTemplate.Where(p => p.TemplateId == model.TemplateId).ToList();
            var Template = resultdata.Select(p => p.TemplateString).FirstOrDefault();
            string TemplateString = "";

            if (string.IsNullOrEmpty(model.TemplateId))
            {
                TemplateString = model.TemplateString;
            }
            else
            {
                TemplateString = $"Hi, {Template} ";
            }
            var replaceString = TemplateString.Replace("{{otp}}", Notifydata);
            //var message = new Message(new string[] { "deeptisingh@agile-labs.com", "diptiglobal@gmail.com" }, abc, model.EmailDetails.Subject, TemplateString);
            var emailConfig = _config.GetSection("EmailConfiguration").Get<EmailConfiguration>();
            EmailSender _emailSender = new EmailSender(emailConfig,_utils);
            var message = new Message(model.EmailDetails.To, model.EmailDetails.Bcc, model.EmailDetails.cc, model.EmailDetails.Subject, replaceString);
            await _emailSender.SendEmailAsync(message);
            return Ok("SUCCESS");

        }

    }
}
