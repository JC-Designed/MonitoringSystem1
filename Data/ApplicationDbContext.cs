using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Models;

namespace MonitoringSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ===================== COMPANY =====================
        public DbSet<Company> Companies { get; set; } = null!; // added null-forgiving to avoid warnings

        // ===================== POSTS =====================
        public DbSet<Post> Posts { get; set; } = null!;

        // Multiple images per post
        public DbSet<PostImage> PostImages { get; set; } = null!;

        // Likes per post
        public DbSet<PostLike> PostLikes { get; set; } = null!;
    }
}
