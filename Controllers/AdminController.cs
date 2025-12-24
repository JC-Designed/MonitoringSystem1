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
            var allUsers = await _userManager.Users.ToListAsync();

            foreach (var user in allUsers)
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            ViewBag.PendingUsers = allUsers.Count(u => !u.IsApproved);
            ViewBag.ApprovedUsers = allUsers.Count(u => u.IsApproved);
            ViewBag.TotalUsers = allUsers.Count;
            ViewBag.TotalCompanies = allUsers.Count(u => u.Roles.Contains("Company"));

            ViewBag.PendingUsersList = allUsers
                .Where(u => !u.IsApproved)
                .Select(u => new PendingUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Roles.FirstOrDefault() ?? "No Role"
                })
                .ToList();

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

            return View(allUsers);
        }

        // ================= USERS PAGE =================
        public async Task<IActionResult> Users()
        {
            var allUsers = await _userManager.Users.ToListAsync();

            foreach (var user in allUsers)
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            return View(allUsers);
        }

        // ================= COMPANY PAGE =================
        public IActionResult Company() => View();

        // ================= MESSAGES PAGE =================
        public IActionResult Messages() => View();

        // ================= REPORTS PAGE =================
        public IActionResult Reports() => View();

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

        // ================= EDIT USER (AJAX POST) =================
        [HttpPost]
        public async Task<IActionResult> EditUser([FromBody] EditUserDto model)
        {
            if (string.IsNullOrEmpty(model.Id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.UserName = model.Name;
            user.Email = model.Email;

            // Update role if changed
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            // Update password if provided
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

        // ================= PENDING USER DTO =================
        public class PendingUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        // ================= EDIT USER DTO =================
        public class EditUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        // ================= DELETE USER DTO =================
        public class DeleteUserDto
        {
            public string Id { get; set; } = string.Empty;
        }
    }
}
