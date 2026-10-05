using System.ComponentModel.DataAnnotations.Schema;

namespace Soluvion.Domain.Models
{
    public class AppointmentUsedProduct
    {
        public int Id { get; set; }
        
        // Csatolhatjuk az AppointmentItem-hez, hiszen azon belül értelmezett a szolgáltatás
        public int AppointmentItemId { get; set; }
        
        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal QuantityUsed { get; set; }

        public bool IsBilledToCustomer { get; set; } = false;

        // Navigation
        public AppointmentItem? AppointmentItem { get; set; }
        public Product? Product { get; set; }
    }
}

