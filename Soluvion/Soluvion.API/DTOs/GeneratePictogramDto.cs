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

        /// <summary>
        /// "svg" = GPT-6.1-Sol High-Reasoning direct SVG generation
        /// "image" = Two-step GPT-Image-2.5 image generation -> client-side SVG vectorization
        /// </summary>
        [MaxLength(30, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string Mode { get; set; } = "svg";

        /// <summary>
        /// Optional preferred AI model ("gpt-6-astra" or "gpt-6.1-sol"). Defaults to "gpt-6-astra".
        /// </summary>
        [MaxLength(50, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? PreferredModel { get; set; } = "gpt-6-astra";

        /// <summary>
        /// Optional previous SVG(s) when regenerating, so the AI chooses a fresh visual metaphor in a new context.
        /// </summary>
        [MaxLength(12000, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? PreviousSvg { get; set; }

        /// <summary>
        /// Optional sibling service/category names so every category gets a distinct icon.
        /// </summary>
        public List<string>? SiblingServices { get; set; }
    }

    public class GeneratePictogramResponseDto
    {
        public string? Svg { get; set; }
        public string? Base64Image { get; set; }
        public string ModelUsed { get; set; } = string.Empty;
        public string Mode { get; set; } = "svg";
    }

    public class GenerateMissingPictogramsRequestDto
    {
        [MaxLength(10, ErrorMessage = "VALIDATION_MAX_LENGTH")]
        public string? LanguageCode { get; set; }
    }
}

