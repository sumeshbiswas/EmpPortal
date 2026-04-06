using Microsoft.AspNetCore.Mvc;

namespace EmpPortal.Controllers.AccessedError
{
    public class AccessedErrorController : Controller
    {
        public IActionResult Index(string message, int? code)
        {
            ViewBag.ErrorMessage = message ?? "Something went wrong.";
            ViewBag.StatusCode = code ?? 500;
            return View();
        }


    }
}
