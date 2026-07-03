using Microsoft.AspNetCore.Mvc;

namespace TUPMundial.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Login", "Auth");
        }
    }
}
