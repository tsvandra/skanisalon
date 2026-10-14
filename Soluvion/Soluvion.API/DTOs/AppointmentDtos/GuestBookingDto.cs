using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class GuestBookingDto
    {
        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [EmailAddress(ErrorMessage = "VALIDATION_EMAIL")]
        [MaxLength(150, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MinLength(6, ErrorMessage = "VALIDATION_PHONE")]
        [MaxLength(50, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MinLength(2, ErrorMessage = "VALIDATION_MIN_LENGTH")]
        [MaxLength(150, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string FullName { get; set; } = string.Empty;

        // Dinamikus vendég attribútumok (pl. Hajhossz)
        public Dictionary<string, string> Attributes { get; set; } = new();

        [MaxLength(1000, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MinLength(1, ErrorMessage = "VALIDATION_MIN_ITEMS")]
        public List<int> ServiceVariantIds { get; set; } = new();

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        public DateTime StartDateTime { get; set; }

        // A frontend küldi, hogy melyik dolgozóhoz foglalunk
        public int EmployeeId { get; set; }
    }
}