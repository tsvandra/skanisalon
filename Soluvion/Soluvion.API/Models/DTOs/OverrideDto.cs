using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.Models.DTOs
{
    public class OverrideDto
    {
        [Required]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(5)]
        public string LanguageCode { get; set; } = string.Empty;

        [Required]
        public string Key { get; set; } = string.Empty; // pl. "nav.services"

        [Required]
        public string Value { get; set; } = string.Empty; // pl. "Kezelések"
    }
}