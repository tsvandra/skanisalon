using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.Models.DTOs
{
    public class PublishLanguageDto
    {
        [Required]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(5)]
        public string LanguageCode { get; set; } = string.Empty;
    }
}
