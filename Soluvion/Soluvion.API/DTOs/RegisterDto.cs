using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public int CompanyTypeId { get; set; } 
    }
}