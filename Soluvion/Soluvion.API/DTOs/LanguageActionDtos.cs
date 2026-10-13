using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs
{
    public class ToggleLanguageStatusDto
    {
        [Required]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(5)]
        public string LanguageCode { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public class SetDefaultLanguageDto
    {
        [Required]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(5)]
        public string LanguageCode { get; set; } = string.Empty;
    }
}

