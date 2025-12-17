using Microsoft.AspNetCore.Identity;

namespace MonitoringSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsApproved { get; set; } = true;
    }
}
