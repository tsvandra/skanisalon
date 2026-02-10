using OpenAI.Chat;
using System.ClientModel;

namespace Soluvion.API.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateTextAsync(string text, string targetLanguage);
    }

    public class OpenAiTranslationService : ITranslationService
    {
        private readonly ChatClient _chatClient;
        private const string MODEL_ID = "gpt-4o-mini"; // Költséghatékony és gyors modell

        public OpenAiTranslationService(IConfiguration configuration)
        {
            var apiKey = configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API Key nincs beállítva az appsettings.json-ban!");
            }

            // Hivatalos OpenAI kliens inicializálása
            _chatClient = new ChatClient(MODEL_ID, new ApiKeyCredential(apiKey));
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Prompt engineering:
            // 1. Megadjuk a szerepet (System message)
            // 2. Megadjuk a feladatot (User message)
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage($"You are a professional translator for a beauty salon website. Translate the input text to {targetLanguage}. Only return the translated text, no explanations."),
                new UserChatMessage(text)
            };

            try
            {
                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);

                if (completion.Content != null && completion.Content.Count > 0)
                {
                    return completion.Content[0].Text.Trim();
                }

                return text; // Fallback, ha nincs válasz
            }
            catch (Exception ex)
            {
                // Hiba esetén (pl. elfogyott a keret) logolhatnánk, de most visszaadjuk az eredetit
                Console.WriteLine($"OpenAI Hiba: {ex.Message}");
                return text; // Fail-safe: ne omoljon össze az app, csak ne fordítson
            }
        }
    }
}