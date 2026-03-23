namespace Soluvion.API.DTOs
{
    public class CatalogDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public Dictionary<string, string> Name { get; set; } = new();
        public Dictionary<string, string> Category { get; set; } = new();
        public Dictionary<string, string> Description { get; set; } = new();
        public decimal? DefaultPrice { get; set; }
        public int DefaultDuration { get; set; }
        public int OrderIndex { get; set; }
        public List<CatalogVariantDto> Variants { get; set; } = new();
    }

    public class CatalogVariantDto
    {
        public int Id { get; set; }
        public Dictionary<string, string> VariantName { get; set; } = new();
        public decimal? Price { get; set; }
        public int Duration { get; set; }
    }
}