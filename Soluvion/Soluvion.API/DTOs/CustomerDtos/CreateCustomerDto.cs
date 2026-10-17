using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs.CustomerDtos
{
    public class CreateCustomerDto
    {
        private string? _phone;
        private string? _email;

        [MaxLength(100, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "VALIDATION_PHONE")]
        [MaxLength(30, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Phone
        {
            get => _phone;
            set => _phone = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        [EmailAddress(ErrorMessage = "VALIDATION_EMAIL")]
        [MaxLength(150, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Email
        {
            get => _email;
            set => _email = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        [MaxLength(1000, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Notes { get; set; }

        public Dictionary<string, string> Attributes { get; set; } = new();
    }
}