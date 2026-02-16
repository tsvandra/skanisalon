using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.Models
{
    public class UiTranslationOverride
    {
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(5)]
        public string LanguageCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string TranslationKey { get; set; } = string.Empty; // pl. "nav.services"

        [Required]
        public string TranslatedText { get; set; } = string.Empty; // pl. "Kezelések"
    }
}