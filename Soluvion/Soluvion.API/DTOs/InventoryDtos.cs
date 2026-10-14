using Soluvion.Domain.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs
{
    public class InventoryDocumentDto
    {
        public int Id { get; set; }
        public InventoryDocumentType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }
        public int? AppointmentId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? CreatedByName { get; set; }
        public bool IsReversed { get; set; }
        public int? ReversalOfDocumentId { get; set; }
        public int ItemsCount { get; set; }
        public decimal TotalValue { get; set; }
        public List<InventoryDocumentItemDto> Items { get; set; } = new();
    }

    public class InventoryDocumentItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductShade { get; set; }
        public string? ProductEan { get; set; }
        public string? ProductImageUrl { get; set; }
        public UnitOfMeasure ProductUnit { get; set; }
        public decimal ProductPackageSize { get; set; }
        public decimal Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public string? ItemNote { get; set; }
        public decimal? StockBefore { get; set; }
        public decimal? StockAfter { get; set; }
    }

    public class CreateInventoryDocumentDto
    {
        public InventoryDocumentType Type { get; set; }
        public string? Note { get; set; }
        public int? AppointmentId { get; set; }

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

        [MaxLength(500)]
        public string? ItemNote { get; set; }
    }

    public class ProductHistoryResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Shade { get; set; }
        public string? ImageUrl { get; set; }
        public UnitOfMeasure Unit { get; set; }
        public decimal PackageSize { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal CostPrice { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal TotalReceivedQty { get; set; }
        public decimal TotalIssuedQty { get; set; }
        public int MovementsCount { get; set; }
        public decimal? LastCostPrice { get; set; }
        public decimal? PreviousCostPrice { get; set; }
        public List<ProductMovementHistoryItemDto> Movements { get; set; } = new();
    }

    public class ProductMovementHistoryItemDto
    {
        public int ItemId { get; set; }
        public int DocumentId { get; set; }
        public InventoryDocumentType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Quantity { get; set; }
        public decimal SignedQuantity { get; set; }
        public decimal StockBefore { get; set; }
        public decimal StockAfter { get; set; }
        public decimal CostPrice { get; set; }
        public string? DocumentNote { get; set; }
        public string? ItemNote { get; set; }
        public string? CreatedByName { get; set; }
        public int? AppointmentId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public bool IsReversed { get; set; }
        public int? ReversalOfDocumentId { get; set; }
        public int DocumentItemsCount { get; set; }
    }

    public class UpdateInventoryNoteDto
    {
        [MaxLength(500)]
        public string? Note { get; set; }
    }
}


