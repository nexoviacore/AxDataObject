using Microsoft.AspNetCore.Mvc;

namespace AgileConnect.Controllers
{
    public class SignalRController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
