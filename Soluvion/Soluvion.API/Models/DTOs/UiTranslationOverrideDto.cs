namespace Soluvion.API.Models.DTOs
{
    public class UiTranslationOverrideDto
    {
        public int CompanyId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}