using Soluvion.Domain.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soluvion.Domain.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? EAN { get; set; }

        // Belső szakmai használatra való-e
        public bool IsProfessional { get; set; } = true;

        // Pultnál eladható-e a vendégeknek
        public bool IsRetail { get; set; } = false;

        public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Milliliter;

        // Kiszerelés (pl. 100, ha egy 100ml-es tubusról van szó)
        [Column(TypeName = "decimal(18,4)")]
        public decimal PackageSize { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RetailPrice { get; set; }

        // Gyors készlet lekérdezéshez (bár a bizonylatokból is kiszámolható)
        [Column(TypeName = "decimal(18,4)")]
        public decimal CurrentStock { get; set; } = 0;

        // Minimum készlet (figyelmeztetés rendeléshez)
        [Column(TypeName = "decimal(18,4)")]
        public decimal LowStockThreshold { get; set; } = 0;

        public bool IsDeleted { get; set; } = false;
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public Company? Company { get; set; }
        
        public ICollection<InventoryDocumentItem> InventoryMovements { get; set; } = new List<InventoryDocumentItem>();
        public ICollection<ServiceVariantProduct> ServiceVariantLinks { get; set; } = new List<ServiceVariantProduct>();
    }
}

