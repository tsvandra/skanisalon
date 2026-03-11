namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }

        // Később ide jöhetnek a Service nevek, Vendég nevek is, 
        // egyelőre az ID-k és alapadatok is elegek a naptárnak.
    }
}
