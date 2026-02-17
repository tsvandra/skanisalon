namespace Soluvion.API.Models.DTOs
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> Category { get; set; }
        public Dictionary<string, string> Description { get; set; }
        public decimal? DefaultPrice { get; set; }
        public int DefaultDuration { get; set; }
        public int OrderIndex { get; set; }
        public List<ServiceVariantDto> Variants { get; set; } = new();
    }

    public class ServiceVariantDto
    {
        public int Id { get; set; }
        public string VariantName { get; set; }
        public decimal? Price { get; set; }
        public int Duration { get; set; }
    }
}