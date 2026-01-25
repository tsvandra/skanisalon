using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public  string Username { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
        public int CompanyId { get; set; }
    }
}
