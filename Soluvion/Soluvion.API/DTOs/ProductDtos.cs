using System.ComponentModel.DataAnnotations;
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
        public string? ImageUrl { get; set; }
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
        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MaxLength(150, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Description { get; set; }

        [MaxLength(50, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? EAN { get; set; }

        [MaxLength(50, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Shade { get; set; }

        [MaxLength(500, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? ImageUrl { get; set; }

        public bool IsProfessional { get; set; } = true;
        public bool IsRetail { get; set; } = false;
        public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Milliliter;

        [Range(0, 1000000, ErrorMessage = "VALIDATION_RANGE")]
        public decimal PackageSize { get; set; }

        [Range(0, 1000000, ErrorMessage = "VALIDATION_RANGE")]
        public decimal CostPrice { get; set; }

        [Range(0, 1000000, ErrorMessage = "VALIDATION_RANGE")]
        public decimal RetailPrice { get; set; }

        [Range(0, 1000000, ErrorMessage = "VALIDATION_RANGE")]
        public decimal LowStockThreshold { get; set; }

        [Range(0, 1000000, ErrorMessage = "VALIDATION_RANGE")]
        public decimal InitialStock { get; set; } = 0;
    }

    public class UpdateProductDto
    {
        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MaxLength(150, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Description { get; set; }

        [MaxLength(50, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? EAN { get; set; }

        [MaxLength(50, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? Shade { get; set; }

        [MaxLength(500, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? ImageUrl { get; set; }

        public bool IsProfessional { get; set; }
        public bool IsRetail { get; set; }
        public UnitOfMeasure Unit { get; set; }

        [Range(0, 1000000, ErrorMessage = "VALIDATION_RANGE")]
        public decimal PackageSize { get; set; }

        [Range(0, 1000000, ErrorMessage = "VALIDATION_RANGE")]
        public decimal CostPrice { get; set; }

        [Range(0, 1000000, ErrorMessage = "VALIDATION_RANGE")]
        public decimal RetailPrice { get; set; }

        [Range(0, 1000000, ErrorMessage = "VALIDATION_RANGE")]
        public decimal LowStockThreshold { get; set; }
    }
}

