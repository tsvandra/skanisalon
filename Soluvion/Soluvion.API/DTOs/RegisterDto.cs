using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MaxLength(100, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MinLength(6, ErrorMessage = "VALIDATION_MIN_LENGTH")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MaxLength(150, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [Range(1, int.MaxValue, ErrorMessage = "VALIDATION_RANGE")]
        public int CompanyTypeId { get; set; } 
    }
}