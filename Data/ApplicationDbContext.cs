using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Models;

namespace MonitoringSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Existing table
        public DbSet<Company> Companies { get; set; }

        // Existing Post table
        public DbSet<Post> Posts { get; set; }

        // NEW: Table for multiple images per post
        public DbSet<PostImage> PostImages { get; set; }

        // NEW: Table for likes on posts
        public DbSet<PostLike> PostLikes { get; set; }
    }
}
