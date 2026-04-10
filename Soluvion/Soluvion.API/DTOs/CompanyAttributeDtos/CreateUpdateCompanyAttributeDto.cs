using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs.CompanyAttributeDtos
{
    public class CreateUpdateCompanyAttributeDto
    {
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Label { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DataType { get; set; } = "text";

        public List<string> Options { get; set; } = new();
        public bool IsRequired { get; set; }
        public bool ShowOnPublicBooking { get; set; }
        public bool IsActive { get; set; } = true;
    }
}