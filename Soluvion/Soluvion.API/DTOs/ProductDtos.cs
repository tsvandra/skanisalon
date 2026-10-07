using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? EAN { get; set; }
        public string? Shade { get; set; }
        public bool IsProfessional { get; set; }
        public bool IsRetail { get; set; }
        public UnitOfMeasure Unit { get; set; }
        public decimal PackageSize { get; set; }
        public decimal CostPrice { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal LowStockThreshold { get; set; }
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? EAN { get; set; }
        public string? Shade { get; set; }
        public bool IsProfessional { get; set; } = true;
        public bool IsRetail { get; set; } = false;
        public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Milliliter;
        public decimal PackageSize { get; set; }
        public decimal CostPrice { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal LowStockThreshold { get; set; }
    }

    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? EAN { get; set; }
        public string? Shade { get; set; }
        public bool IsProfessional { get; set; }
        public bool IsRetail { get; set; }
        public UnitOfMeasure Unit { get; set; }
        public decimal PackageSize { get; set; }
        public decimal CostPrice { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal LowStockThreshold { get; set; }
    }
}

