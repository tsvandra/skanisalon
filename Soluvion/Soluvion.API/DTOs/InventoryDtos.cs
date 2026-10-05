using Soluvion.Domain.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs
{
    public class InventoryDocumentDto
    {
        public int Id { get; set; }
        public InventoryDocumentType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; } public int? AppointmentId { get; set; }
        public bool IsReversed { get; set; }
        public int? ReversalOfDocumentId { get; set; }
        public List<InventoryDocumentItemDto> Items { get; set; } = new();
    }

    public class InventoryDocumentItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal CostPrice { get; set; }
    }

    public class CreateInventoryDocumentDto
    {
        public InventoryDocumentType Type { get; set; }
        public string? Note { get; set; } public int? AppointmentId { get; set; }

        [Required, MinLength(1, ErrorMessage = "Legalább egy tételt meg kell adni!")]
        public List<CreateInventoryDocumentItemDto> Items { get; set; } = new();
    }

    public class CreateInventoryDocumentItemDto
    {
        public int ProductId { get; set; }
        
        // Pozitív érték, az API dönti el a fej Type-ja alapján a logikát
        [Range(0.0001, double.MaxValue, ErrorMessage = "A mennyiségnek nagyobbnak kell lennie 0-nál.")]
        public decimal Quantity { get; set; }
        public decimal CostPrice { get; set; }
    }
}

