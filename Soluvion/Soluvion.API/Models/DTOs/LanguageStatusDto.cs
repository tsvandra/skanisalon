namespace Soluvion.API.Models.DTOs
{
    public class LanguageStatusDto
    {
        public string LanguageCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Enum stringként
        public bool IsDefault { get; set; }
        public int Progress { get; set; }
    }
}
