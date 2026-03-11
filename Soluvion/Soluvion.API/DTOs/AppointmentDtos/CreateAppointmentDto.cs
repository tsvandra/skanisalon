namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class CreateAppointmentDto
    {
        public int CustomerId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartDateTime { get; set; }
        public List<int> ServiceVariantIds { get; set; } = new();
        public string? Notes { get; set; }
        public bool Force { get; set; } = false; // Ütközés kényszerítése
    }
}
