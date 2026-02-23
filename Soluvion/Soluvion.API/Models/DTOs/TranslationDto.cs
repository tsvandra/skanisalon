namespace Soluvion.API.Models.DTOs
{
    /// <summary>
    /// A "Varázspálca" (egyszeri szövegfordítás) kéréséhez használt DTO
    /// </summary>
    public class TranslationRequestDto
    {
        public string Text { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
    }
}