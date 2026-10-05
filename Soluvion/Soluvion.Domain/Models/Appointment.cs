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
        
        // St�tusz �s Forr�s
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        public BookingSource Source { get; set; } = BookingSource.Web;

        // Sz�veges mezok (Megjegyz�sek �s Egyezked�s)
        public string? CustomerNotes { get; set; } // Ezt �rta a vend�g a weben
        public string? AdminNotes { get; set; }    // Ezt csak a dolgoz�k l�tj�k (belso info)
        public string? StatusReason { get; set; }  // Indokl�s elutas�t�shoz vagy �tszervez�shez

        public ICollection<AppointmentItem> Items { get; set; } = new List<AppointmentItem>();        public bool MaterialUsageRecorded { get; set; } = false;
        public string? ExtraMaterials { get; set; } // JSON array of extra materials
    }
}
