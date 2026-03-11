namespace Soluvion.API.Interfaces
{
    public interface ITranslationService
    {
        Task<string> TranslateTextAsync(string text, string targetLanguage, string context, string companyType);
    }
}
