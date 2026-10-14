using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soluvion.Domain.Models
{
    public class InventoryDocumentItem
    {
        public int Id { get; set; }
        public int InventoryDocumentId { get; set; }
        public int ProductId { get; set; }

        // Itt mindig abszolút (pozitív) értéket tárolunk (pl. 5 db).
        // Az üzleti logika (Service réteg) a fej (InventoryDocument.Type) alapján dönti el, 
        // hogy ez növeli (Receipt) vagy csökkenti (Issue) a Product.CurrentStock értékét.
        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; }

        // Milyen áron lett bevételezve/kiadva
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        /// <summary>Tételszintű (soronkénti) egyedi megjegyzés.</summary>
        [MaxLength(500)]
        public string? ItemNote { get; set; }

        /// <summary>A termék készlete közvetlenül a mozgás előtt.</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? StockBefore { get; set; }

        /// <summary>A termék készlete közvetlenül a mozgás után.</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal? StockAfter { get; set; }

        // Navigation
        public InventoryDocument? InventoryDocument { get; set; }
        public Product? Product { get; set; }
    }
}

