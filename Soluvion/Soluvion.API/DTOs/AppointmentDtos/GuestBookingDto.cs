namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class GuestBookingDto
    {
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        // Dinamikus vendég attribútumok (pl. Hajhossz)
        public Dictionary<string, string> Attributes { get; set; } = new();
        public string? Notes { get; set; }

        public List<int> ServiceVariantIds { get; set; } = new();
        public DateTime StartDateTime { get; set; }

        // A frontend küldi, hogy melyik dolgozóhoz foglalunk
        public int EmployeeId { get; set; }
    }
}