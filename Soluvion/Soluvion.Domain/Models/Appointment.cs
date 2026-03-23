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
        
        // Státusz és Forrás
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        public BookingSource Source { get; set; } = BookingSource.Web;

        // Szöveges mezők (Megjegyzések és Egyezkedés)
        public string? CustomerNotes { get; set; } // Ezt írta a vendég a weben
        public string? AdminNotes { get; set; }    // Ezt csak a dolgozók látják (belső info)
        public string? StatusReason { get; set; }  // Indoklás elutasításhoz vagy átszervezéshez

        public ICollection<AppointmentItem> Items { get; set; } = new List<AppointmentItem>();
    }
}