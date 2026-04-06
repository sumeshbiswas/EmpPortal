using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class hrmsjoiningController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/HRMS/hrmsjoining/Index.cshtml");
        }
    }
}
