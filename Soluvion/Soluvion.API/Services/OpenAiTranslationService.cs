using OpenAI.Chat;
using System.ClientModel;

namespace Soluvion.API.Services
{
    public class OpenAiTranslationService : ITranslationService
    {
        private readonly ChatClient _chatClient;
        private const string MODEL_ID = "gpt-5.2";

        public OpenAiTranslationService(IConfiguration configuration)
        {
            var apiKey = configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API Key nincs beállítva!");
            }
            _chatClient = new ChatClient(MODEL_ID, new ApiKeyCredential(apiKey));
        }

        // MÓDOSÍTÁS: companyType paraméter fogadása
        public async Task<string> TranslateTextAsync(string text, string targetLanguage, string context, string companyType)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Biztonsági ellenőrzés: Ha nincs megadva típus, legyen általános
            string businessType = string.IsNullOrWhiteSpace(companyType) ? "General Business" : companyType;

            // 1. Alap Prompt (Dinamikus Cégtípussal)
            string baseSystemPrompt = $"You are a professional translator for a {businessType} website. ";

            // 2. Kontextus-függő kiegészítés
            string contextInstruction = "";

            switch (context?.ToLower())
            {
                case "service": // Árlista
                    contextInstruction = "Keep service names concise and professional. Do NOT translate well-known brand names. If a term is an industry-standard technical term, keep the professional equivalent.";
                    break;

                case "gallery": // Galéria
                    contextInstruction = "Be creative, descriptive, and inviting. Use an engaging tone suitable for photo captions to attract customers.";
                    break;

                case "inventory": // Raktár
                    contextInstruction = "Strict technical translation. Do not add filler words. Preserve numbers, units (ml, g, oz) and product codes exactly as they appear.";
                    break;

                default: // Általános
                    contextInstruction = "Translate accurately and naturally.";
                    break;
            }

            // 3. Promptok összefűzése
            string finalSystemPrompt = $"{baseSystemPrompt} {contextInstruction} Translate the input text to {targetLanguage}. Only return the translated text, no explanations.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(finalSystemPrompt),
                new UserChatMessage(text)
            };

            try
            {
                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);
                
                if (completion.Content != null && completion.Content.Count > 0)
                {
                    return completion.Content[0].Text.Trim();
                }
                
                return text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAI Hiba: {ex.Message}");
                return text;
            }
        }
    }
}