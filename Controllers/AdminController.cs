using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Models;
using MonitoringSystem.Data;
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
        private readonly ApplicationDbContext _db;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // ================= HELPER TO GET SCHOOL YEAR =================
        private int GetSchoolYear()
        {
            if (int.TryParse(Request.Query["schoolYear"], out int year))
                return year;
            return DateTime.Now.Year;
        }

        // ================= DASHBOARD =================
        public async Task<IActionResult> Dashboard()
        {
            int schoolYear = GetSchoolYear();

            // Get all users for the selected school year including Admins
            var allUsers = await _userManager.Users
                .Where(u => u.CreatedAt.Year <= schoolYear) // Include all users created until the school year
                .ToListAsync();

            foreach (var user in allUsers)
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            // Counts
            ViewBag.PendingUsers = allUsers.Count(u => !u.IsApproved);
            ViewBag.ApprovedUsers = allUsers.Count(u => u.IsApproved);
            ViewBag.TotalUsers = allUsers.Count; // ✅ Includes Admins
            ViewBag.TotalCompanies = _db.Companies
                .Include(c => c.User)
                .Count(c => c.User != null && c.User.CreatedAt.Year <= schoolYear);

            // Pending Users List
            ViewBag.PendingUsersList = allUsers
                .Where(u => !u.IsApproved)
                .Select(u => new PendingUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Roles.FirstOrDefault() ?? "No Role"
                })
                .ToList();

            // Monthly Registrations
            var monthlyRegistrations = new int[12];
            var totalUsersByMonth = new int[12];

            for (int month = 1; month <= 12; month++)
            {
                // New users in this month
                monthlyRegistrations[month - 1] = allUsers.Count(u =>
                    u.CreatedAt.Year == schoolYear && u.CreatedAt.Month == month);

                // Total users up to this month
                totalUsersByMonth[month - 1] = allUsers.Count(u =>
                    u.CreatedAt.Year < schoolYear ||
                    (u.CreatedAt.Year == schoolYear && u.CreatedAt.Month <= month));
            }

            ViewBag.MonthlyRegistrations = monthlyRegistrations;
            ViewBag.TotalUsersByMonth = totalUsersByMonth;
            ViewBag.CurrentYear = schoolYear;
            ViewBag.SchoolYear = schoolYear;

            return View(allUsers);
        }

        // ================= USERS PAGE =================
        public async Task<IActionResult> Users()
        {
            int schoolYear = GetSchoolYear();
            var allUsers = await _userManager.Users
                .Where(u => u.CreatedAt.Year <= schoolYear) // Include Admins
                .ToListAsync();

            foreach (var user in allUsers)
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            ViewBag.SchoolYear = schoolYear;
            return View(allUsers);
        }

        // ================= COMPANY PAGE =================
        public IActionResult Company()
        {
            int schoolYear = GetSchoolYear();
            var companies = _db.Companies
                .Include(c => c.User)
                .Where(c => c.User != null && c.User.CreatedAt.Year <= schoolYear)
                .ToList();

            ViewBag.SchoolYear = schoolYear;
            return View(companies);
        }

        // ================= MESSAGES PAGE =================
        public IActionResult Messages()
        {
            int schoolYear = GetSchoolYear();
            ViewBag.SchoolYear = schoolYear;
            return View();
        }

        // ================= REPORTS PAGE =================
        public IActionResult Reports()
        {
            int schoolYear = GetSchoolYear();
            ViewBag.SchoolYear = schoolYear;
            return View();
        }

        // ================= APPROVE USER (AJAX) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsApproved = true;
                await _userManager.UpdateAsync(user);
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        // ================= REJECT USER (AJAX) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        // ================= EDIT USER (AJAX POST) =================
        [HttpPost]
        public async Task<IActionResult> EditUser([FromBody] EditUserDto model)
        {
            if (string.IsNullOrEmpty(model.Id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.UserName = model.Name;
            user.Email = model.Email;

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            }

            await _userManager.UpdateAsync(user);
            return Ok();
        }

        // ================= DELETE USER (AJAX POST) =================
        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserDto model)
        {
            if (string.IsNullOrEmpty(model.Id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            return Ok();
        }

        // ================= DTO CLASSES =================
        public class PendingUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        public class EditUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        public class DeleteUserDto
        {
            public string Id { get; set; } = string.Empty;
        }
    }
}
