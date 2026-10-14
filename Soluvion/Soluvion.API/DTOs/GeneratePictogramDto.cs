using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.DTOs
{
    public class GeneratePictogramRequestDto
    {
        [Required(ErrorMessage = "VALIDATION_REQUIRED")]
        [MaxLength(200, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? ServiceName { get; set; }

        public int? ServiceId { get; set; }
        public bool IsCategoryLevel { get; set; }
    }

    public class GenerateMissingPictogramsRequestDto
    {
        [MaxLength(10, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? LanguageCode { get; set; }
    }
}

