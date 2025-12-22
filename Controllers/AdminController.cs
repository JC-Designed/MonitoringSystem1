using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public IActionResult Landing()
        {
            // This action renders the landing page
            return View();
        }
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            return View();
        }

        public IActionResult Users()
        {
            ViewData["Title"] = "Users";
            return View();
        }

        public IActionResult Company()
        {
            ViewData["Title"] = "Company";
            return View();
        }

        public IActionResult Messages()
        {
            ViewData["Title"] = "Messages";
            return View();
        }

        public IActionResult Reports()
        {
            ViewData["Title"] = "Reports";
            return View();
        }
    }
}
