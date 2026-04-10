using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs.CompanyAttributeDtos
{
    public class CompanyAttributeDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public bool IsRequired { get; set; }
        public bool ShowOnPublicBooking { get; set; }
        public bool IsActive { get; set; }
    }
}