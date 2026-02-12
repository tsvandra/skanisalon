namespace Soluvion.API.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateTextAsync(string text, string targetLanguage, string context, string companyType);
    }
}
