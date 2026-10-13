using ARM_APIs.Interface;
using ARMCommon.ActionFilter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ARM_APIs.Controllers
{
    [ServiceFilter(typeof(ApiResponseFilter))]
    [ApiController]
    public class ARMAuthenticateUserController : Controller
    {

        private readonly DataContext _context;
        private readonly IAPI _api;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration _configuration;

        public ARMAuthenticateUserController(DataContext context, IAPI api, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _context = context;
            _api = api;
            this.httpContextAccessor = httpContextAccessor;
            _configuration = configuration; 
        }


        [AllowAnonymous]
        [HttpPost("api/ARMAuthenticateUser")]
        public async Task<IActionResult> ARMAuthenticateUser(ARMAuthenticateUser users)
        {
            ARMResult result;
            if (string.IsNullOrEmpty(users.username) || string.IsNullOrEmpty(users.usergroup) || string.IsNullOrEmpty(users.appname))
                throw new KeyNotFoundException("Invalid Fields");
            Random generator = new Random();
            string OTP = generator.Next(0, 1000000).ToString("D6");

            string url = _configuration["EmailConfiguration:URL"];//"http://52.172.91.153/ARM/ARMNotify";


            //string url = "https://localhost:7192/ARMNotify";

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            parameters.Add("OTP", OTP);
            //IEnumerable<string> m_oEnum = new List<string>() { "nsmahara8@gmail.com", "deeptisingh@agile-labs.com" };

            IEnumerable<string> senderList = new List<string>() { };
            senderList = senderList.Concat(new List<string>() { users.email });

            var model = new EmailDetails()
            {
                To = senderList.ToList(),
                Subject = "ARMAuthenticUserTesting"
            };


            var input = new ARMNotify()
            {
                NotificationType = "Email",
                TemplateId = "ARMAuthenticateUser",
                TemplateString = "Your SignIn OTP is {{otp}}",
                NotificationData = parameters,
                EmailDetails = model,
            };
            string inputJson = JsonConvert.SerializeObject(input);

            string Mediatype = "application/json";

            await _api.POSTData(url, inputJson, Mediatype);
             
            result = new ARMResult();
            result.result.Add("message", "SUCCESS");
            result.result.Add("OTP", OTP);
            return Ok(result);


        }



    }
}
