using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmsmasterController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/HRMS/hrmsmaster/Index.cshtml");
        }
    }
}
