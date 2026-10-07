using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.DTOs
{
    public class ProductAiScanResultDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Shade { get; set; }
        public string? EAN { get; set; }
        public decimal PackageSize { get; set; }
        public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Milliliter;
        public bool IsProfessional { get; set; } = true;
        public bool IsRetail { get; set; } = false;
        public string? Description { get; set; }
        public decimal? EstimatedPrice { get; set; }
        public string? ImageUrl { get; set; }
    }
}

