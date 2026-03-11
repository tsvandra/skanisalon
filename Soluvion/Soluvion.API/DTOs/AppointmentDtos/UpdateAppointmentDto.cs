using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class UpdateAppointmentDto
    {
        public DateTime StartDateTime { get; set; }
        public List<int> ServiceVariantIds { get; set; } = new();
        public string? Notes { get; set; }
        public AppointmentStatus Status { get; set; }
        public bool Force { get; set; } = false;
    }
}
