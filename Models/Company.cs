using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        [Phone]
        public string Contact { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Industry { get; set; }
    }
}
