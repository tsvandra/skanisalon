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
    }
}
