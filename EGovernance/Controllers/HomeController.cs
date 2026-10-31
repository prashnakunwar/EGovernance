using Microsoft.AspNetCore.Mvc;

namespace EGOVERNANCE.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}