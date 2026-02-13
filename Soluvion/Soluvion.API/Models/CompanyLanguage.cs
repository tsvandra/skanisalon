using Soluvion.API.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soluvion.API.Models
{
    public class CompanyLanguage
    {
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(5)] // pl. "en", "hu", "sk"
        public string LanguageCode { get; set; } = string.Empty;

        public TranslationStatus Status { get; set; } = TranslationStatus.Created;

        // Opcionális: Ki kezdeményezte / mikor
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}