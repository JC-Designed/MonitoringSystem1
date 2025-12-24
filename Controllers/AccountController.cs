using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // ======================= LOGIN =======================
        [HttpGet]
        public IActionResult Login() => View();

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
                    // Admin goes to Landing page first
                    if (roleString == "Admin")
                        return RedirectToAction("Landing", "Admin");
                    else
                        return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "Invalid login attempt.";
            return View();
        }

        // ======================= REGISTER =======================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string roleString)
        {
            if (string.IsNullOrEmpty(roleString))
            {
                ViewBag.Error = "Please select a role.";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                IsApproved = false
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, roleString);
                TempData["Success"] = "Registration successful. Wait for admin approval.";
                return View();
            }

            ViewBag.Error = result.Errors.FirstOrDefault()?.Description;
            return View();
        }

        // ======================= LOGOUT =======================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
