using System.ComponentModel.DataAnnotations;
using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class CreateAppointmentDto
    {
        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [Range(1, int.MaxValue, ErrorMessage = "VALIDATION_RANGE")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [Range(1, int.MaxValue, ErrorMessage = "VALIDATION_RANGE")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MinLength(1, ErrorMessage = "VALIDATION_MIN_ITEMS")]
        public List<AppointmentItemRequestDto> Items { get; set; } = new();

        public AppointmentStatus Status { get; set; }

        [MaxLength(1000, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Notes { get; set; }

        public bool Force { get; set; } = false;
        public string? ExtraMaterials { get; set; }
    }
}
