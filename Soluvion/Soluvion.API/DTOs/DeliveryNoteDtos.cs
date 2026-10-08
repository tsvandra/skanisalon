using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.DTOs
{
    public class DeliveryNoteScanResultDto
    {
        public string? DocumentNumber { get; set; }
        public string? Supplier { get; set; }
        public string? IssueDate { get; set; }
        public List<DeliveryNoteItemDto> Items { get; set; } = new();
    }

    public class DeliveryNoteItemDto
    {
        public string RawName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Shade { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; } = 0;
        public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Piece;
        public decimal PackageSize { get; set; } = 1;
        public string? EAN { get; set; }
    }

    public class ImportDeliveryNoteDto
    {
        public string? DocumentNumber { get; set; }
        public string? Supplier { get; set; }
        public string? Note { get; set; }
        public List<ImportDeliveryNoteItemDto> Items { get; set; } = new();
    }

    public class ImportDeliveryNoteItemDto
    {
        /// <summary>
        /// "match" (hozzárendelés meglévő termékhez) | "create" (új termék létrehozása) | "skip" (kihagyás)
        /// </summary>
        public string Action { get; set; } = "match";

        public int? MatchedProductId { get; set; }

        public CreateProductDto? NewProduct { get; set; }

        public decimal Quantity { get; set; }

        public decimal CostPrice { get; set; }
    }

    public class ImportDeliveryNoteResultDto
    {
        public int DocumentId { get; set; }
        public int CreatedProductsCount { get; set; }
        public int MatchedItemsCount { get; set; }
        public int TotalItemsCount { get; set; }
    }
}
