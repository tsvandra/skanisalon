using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.Models.DTOs
{
    public class AddLanguageDto
    {
        [Required]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(5)]
        public string TargetLanguage { get; set; } = string.Empty; // pl. "sk"
        public Dictionary<string, string> BaseUiTranslations { get; set; } = new();
        public bool UseAi { get; set; } = true;
    }
}
