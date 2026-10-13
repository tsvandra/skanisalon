using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MaxLength(100, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MinLength(6, ErrorMessage = "VALIDATION_MIN_LENGTH")]
        public string Password { get; set; } = string.Empty;
    }
}
