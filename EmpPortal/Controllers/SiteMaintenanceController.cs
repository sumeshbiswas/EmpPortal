using Microsoft.AspNetCore.Mvc;

namespace EmpPortal.Controllers
{
    public class SiteMaintenanceController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/SiteMaintenance/Index.cshtml");
        }
    }
}
