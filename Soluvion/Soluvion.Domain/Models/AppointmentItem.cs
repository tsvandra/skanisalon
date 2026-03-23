namespace Soluvion.Domain.Models
{
    public class AppointmentItem
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }

        public int ServiceVariantId { get; set; }
        public ServiceVariant? ServiceVariant { get; set; }

        public decimal Price { get; set; }
        public int CalculatedDurationMinutes { get; set; }
        public int? ProcessingTimeMinutes { get; set; } // Opcionális hatóidő
    }
}