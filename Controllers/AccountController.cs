using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System.Threading.Tasks;

namespace MonitoringSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET: Login Page
        [HttpGet]
        public IActionResult Login() => View();

        // POST: Login Action
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string roleString)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                if (!user.IsApproved)
                {
                    ViewBag.Error = "Account not approved.";
                    return View();
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains(roleString))
                {
                    ViewBag.Error = "Invalid role selected.";
                    return View();
                }

                var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
                if (result.Succeeded)
                {
                    // Redirect Admin users to Admin Landing page
                    if (roleString == "Admin")
                        return RedirectToAction("Landing", "Admin");
                    else
                        return RedirectToAction("Index", "Home"); // Other roles
                }
            }

            ViewBag.Error = "Invalid login attempt.";
            return View();
        }

        // POST: Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
