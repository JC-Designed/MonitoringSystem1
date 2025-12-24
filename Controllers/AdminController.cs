using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MonitoringSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // ================= LANDING PAGE =================
        public IActionResult Landing()
        {
            return View();
        }

        // ================= DASHBOARD =================
        public async Task<IActionResult> Dashboard()
        {
            // 1. Get all users
            var allUsers = await _userManager.Users.ToListAsync();

            // 2. Load roles for each user
            foreach (var user in allUsers)
            {
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            }

            // 3. Dashboard stats
            ViewBag.PendingUsers = allUsers.Count(u => !u.IsApproved);
            ViewBag.ApprovedUsers = allUsers.Count(u => u.IsApproved);
            ViewBag.TotalUsers = allUsers.Count;
            ViewBag.TotalCompanies = allUsers.Count(u => u.Roles.Contains("Company"));

            // 4. Pending users for modal
            ViewBag.PendingUsersList = allUsers
                .Where(u => !u.IsApproved)
                .Select(u => new PendingUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Roles.FirstOrDefault() ?? "No Role"
                })
                .ToList();

            // 5. Chart data: monthly registrations & cumulative total users
            var currentYear = DateTime.Now.Year;
            var monthlyRegistrations = new int[12];
            var totalUsersByMonth = new int[12];

            for (int month = 1; month <= 12; month++)
            {
                monthlyRegistrations[month - 1] = allUsers.Count(u =>
                    u.CreatedAt.Year == currentYear && u.CreatedAt.Month == month);

                totalUsersByMonth[month - 1] = allUsers.Count(u =>
                    u.CreatedAt.Year < currentYear ||
                    (u.CreatedAt.Year == currentYear && u.CreatedAt.Month <= month));
            }

            ViewBag.MonthlyRegistrations = monthlyRegistrations;
            ViewBag.TotalUsersByMonth = totalUsersByMonth;
            ViewBag.CurrentYear = currentYear;

            // 6. Pass all users as model
            return View(allUsers);
        }

        // ================= APPROVE USER =================
        public async Task<IActionResult> Approve(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsApproved = true;
                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // ================= REJECT USER =================
        public async Task<IActionResult> Reject(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            return RedirectToAction(nameof(Dashboard));
        }
    }
}
