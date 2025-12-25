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
using System.IO;
using Microsoft.AspNetCore.Http;

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

        // ================= LANDING PAGE =================
        public IActionResult Landing()
        {
            var posts = _db.Posts
                .Include(p => p.Images)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View(posts);
        }

        // ================= CREATE NEW POST =================
        [HttpPost]
        public async Task<IActionResult> CreatePost(string content, List<IFormFile>? imageFiles)
        {
            var post = new Post
            {
                UserName = "Admin User",
                Content = content
            };

            if (imageFiles != null && imageFiles.Count > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                foreach (var file in imageFiles)
                {
                    if (file.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploads, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        post.Images.Add(new PostImage { FileName = fileName });
                    }
                }
            }

            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            return RedirectToAction("Landing");
        }

        // ================= EDIT POST =================
        [HttpPost]
        public async Task<IActionResult> EditPost(int postId, string content, List<IFormFile>? newImages, List<int>? removeImageIds)
        {
            var post = await _db.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return NotFound();

            post.Content = content;

            if (removeImageIds != null)
            {
                var imagesToRemove = post.Images.Where(i => removeImageIds.Contains(i.Id)).ToList();
                foreach (var img in imagesToRemove)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", img.FileName);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    post.Images.Remove(img);
                }
            }

            if (newImages != null && newImages.Count > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                foreach (var file in newImages)
                {
                    if (file.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploads, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        post.Images.Add(new PostImage { FileName = fileName });
                    }
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Landing");
        }

        // ================= TOGGLE LIKE =================
        [HttpPost]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var userId = _userManager.GetUserId(User);

            var post = await _db.Posts
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return NotFound();

            var existingLike = post.Likes.FirstOrDefault(l => l.UserId == userId);
            if (existingLike != null)
                _db.PostLikes.Remove(existingLike);
            else
                _db.PostLikes.Add(new PostLike { PostId = postId, UserId = userId });

            await _db.SaveChangesAsync();
            return RedirectToAction("Landing");
        }

        // ================= DELETE POST =================
        [HttpPost]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var post = await _db.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return NotFound();

            if (post.Images != null)
            {
                foreach (var img in post.Images)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", img.FileName);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
            }

            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();

            return RedirectToAction("Landing");
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
