using Soluvion.Domain.Models.Enums;

namespace Soluvion.Domain.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        public int CustomerId { get; set; }
        public CompanyCustomer? Customer { get; set; }

        public int EmployeeId { get; set; }
        public CompanyEmployee? Employee { get; set; }

        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public decimal TotalPrice { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public string? Notes { get; set; }

        public ICollection<AppointmentItem> Items { get; set; } = new List<AppointmentItem>();
    }
}