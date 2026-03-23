using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class CreateAppointmentDto
    {
        public int CustomerId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartDateTime { get; set; }
        public List<AppointmentItemRequestDto> Items { get; set; } = new();
        public AppointmentStatus Status { get; set; }
        public string? Notes { get; set; }
        public bool Force { get; set; } = false; // Ütközés kényszerítése
    }
}
