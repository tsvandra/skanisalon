using System.ComponentModel.DataAnnotations.Schema;

namespace Soluvion.Domain.Models
{
    public class ServiceVariantProduct
    {
        public int Id { get; set; }
        public int ServiceVariantId { get; set; }
        public int ProductId { get; set; }

        // Várható, alapértelmezett fogyás ehhez a variánshoz
        [Column(TypeName = "decimal(18,4)")]
        public decimal DefaultQuantity { get; set; }

        // Navigation
        public ServiceVariant? ServiceVariant { get; set; }
        public Product? Product { get; set; }
    }
}

