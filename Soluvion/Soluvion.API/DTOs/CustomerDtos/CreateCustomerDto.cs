using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs.CustomerDtos
{
    public class CreateCustomerDto
    {
        [MaxLength(100, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "VALIDATION_PHONE")]
        [MaxLength(30, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "VALIDATION_EMAIL")]
        [MaxLength(150, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Email { get; set; }

        [MaxLength(1000, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Notes { get; set; }

        public Dictionary<string, string> Attributes { get; set; } = new();
    }
}