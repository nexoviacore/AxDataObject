using AgileConnect.EncrDecr.cs;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;

namespace AgileConnect.Controllers
{


    public class ARMSignInController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly DataContext _context;
        private readonly IRedisHelper _redis;
        public string key = "ARM-" + Guid.NewGuid().ToString();
        private string generatedToken = null;


        public ARMSignInController(IConfiguration config, ITokenService tokenService, DataContext context, IRedisHelper redis)
        {
            _config = config;
            _tokenService = tokenService;
            _context = context;
            _redis = redis;
        }
        public IActionResult Index()
        {
            return View("SignIn");
        }
        public IActionResult SignIn()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SignIn(UserModel userModel)
        {
            if (string.IsNullOrEmpty(userModel.UserName) || string.IsNullOrEmpty(userModel.Password))
            {
                return (RedirectToAction("Error", "Home"));
            }

            IActionResult response = Unauthorized();


            if (System.IO.File.Exists(_config["ARMConfigFilePath"]))
            {
                string strJSON = String.Empty;

                var fileStream = new FileStream(_config["ARMConfigFilePath"], FileMode.Open, FileAccess.Read, FileShare.None);
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))

                {
                    strJSON = streamReader.ReadToEnd();
                    var jObject = JObject.Parse(strJSON);
                    var privateKey = jObject["KeyData"]?["PrivateKey"]?.ToString();
                    var userId = jObject["AdminInstance"]?["admin"]?.ToString();
                    var Password = jObject["AdminInstance"]?["Password"]?.ToString().ToUpper();



                    // validate in File
                    if (userModel.UserName == userId && userModel.Password.ToUpper() == Password)
                    {
                        generatedToken = _tokenService.CreateToken(_config["Jwt:Key"].ToString(), _config["Jwt:Issuer"].ToString(),
                    userId);

                        if (generatedToken != null)
                        {
                            HttpContext.Session.SetString("TokenId", generatedToken);
                            HttpContext.Session.SetString("SessionId", key);
                            HttpContext.Session.SetString("Username", userModel.UserName);
                            await _redis.StringSetAsync(key, generatedToken);

                            var abc = _redis.StringGetAsync(key);
                            return RedirectToAction("Dashboard", "ARMInstance");
                        }
                        else
                        {
                            ViewBag.result = "UserName or Password is Incorrect";
                            ViewBag.color = "red";
                            return View();
                        }
                    }
                    else
                    {
                        ViewBag.result = "UserName or Password is Incorrect";
                        ViewBag.color = "red";
                        return View();
                    }


                }

            }
            else
            {
                ViewBag.result = "Config File is not Present at Location";
                ViewBag.color = "red";
                return View();
            }

        }


        public async Task<IActionResult> SignOut()
        {
            HttpContext.Session.Clear();
            return View("SignIn");
        }

        [Route("/error-development")]
        public IActionResult HandleErrorDevelopment(
        [FromServices] IHostEnvironment hostEnvironment)
        {
            if (!hostEnvironment.IsDevelopment())
            {
                return NotFound();
            }

            var exceptionHandlerFeature =
                HttpContext.Features.Get<IExceptionHandlerFeature>()!;

            return Problem(
                detail: exceptionHandlerFeature.Error.StackTrace,
                title: exceptionHandlerFeature.Error.Message);
        }

        [Route("/error")]
        public IActionResult HandleError() =>
            Problem();

        public IActionResult ResetCredential()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult ResetCredential(UserModel userModel)
        {
            if (string.IsNullOrEmpty(userModel.Email) || string.IsNullOrEmpty(userModel.Password))
            {
                return (RedirectToAction("Error", "Home"));
            }

            IActionResult response = Unauthorized();


            if (System.IO.File.Exists(_config["ARMConfigFilePath"]))
            {
                string strJSON = String.Empty;

                var fileStream = new FileStream(_config["ARMConfigFilePath"], FileMode.Open, FileAccess.Read, FileShare.None);
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))

                {
                    strJSON = streamReader.ReadToEnd();
                    var jObject = JObject.Parse(strJSON);
                    var privateKey = jObject["KeyData"]?["PrivateKey"]?.ToString();
                    var EmailId = EncDec.Decrypt(jObject["AdminInstance"]?["AdminEmailId"]?.ToString(), privateKey);
                    var Password = EncDec.Decrypt(jObject["AdminInstance"]?["Password"]?.ToString(), privateKey);



                    // validate in File
                    if (userModel.Email == EmailId && userModel.Password == Password)
                    {
                        ViewBag.result = "Password will be sent to your EmailId Shortly";
                        ViewBag.color = "green";
                        return View();
                    }
                    else
                    {
                        ViewBag.result = "Email Id or Password is Incorrect";
                        ViewBag.color = "red";
                        return View();
                    }


                }

            }

            ViewBag.result = "Password will be sent to your EmailId Shortly";
            ViewBag.color = "green";
            return View();
        }

    }
}
