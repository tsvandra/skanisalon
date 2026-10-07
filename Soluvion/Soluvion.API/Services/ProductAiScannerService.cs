using System.ClientModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.Services
{
    public class ProductAiScannerService : IProductAiScannerService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductAiScannerService> _logger;

        public ProductAiScannerService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<ProductAiScannerService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<ProductAiScanResultDto> ScanProductImagesAsync(IEnumerable<IFormFile> images)
        {
            var fileList = images?.ToList() ?? new List<IFormFile>();
            if (fileList.Count == 0)
            {
                throw new ArgumentException("Legalább egy képet fel kell tölteni a beolvasáshoz.");
            }

            // Képek konvertálása memóriába
            var imageItems = new List<(byte[] Bytes, string ContentType)>();
            foreach (var file in fileList)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();
                var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType;
                imageItems.Add((bytes, contentType));
            }

            var geminiKey = _configuration["Gemini:ApiKey"] ?? _configuration["AI:GeminiApiKey"];
            var openAiKey = _configuration["OpenAI:ApiKey"];

            string jsonResponse;

            // 1. Prioritás: Google Gemini (2.0 Flash / 1.5 Flash - ingyenes/filléres, szupergyors)
            if (!string.IsNullOrWhiteSpace(geminiKey) && !geminiKey.Contains("IDE_JON"))
            {
                _logger.LogInformation("Termékfelismerés Google Gemini API segítségével ({ImageCount} kép)...", imageItems.Count);
                jsonResponse = await ScanWithGeminiAsync(imageItems, geminiKey);
            }
            // 2. Prioritás: OpenAI Vision (gpt-4o-mini / gpt-4o)
            else if (!string.IsNullOrWhiteSpace(openAiKey) && !openAiKey.Contains("IDE_JON"))
            {
                _logger.LogInformation("Termékfelismerés OpenAI Vision API segítségével ({ImageCount} kép)...", imageItems.Count);
                jsonResponse = await ScanWithOpenAiAsync(imageItems, openAiKey);
            }
            else
            {
                throw new InvalidOperationException(
                    "Az AI termékfelismeréshez API kulcs szükséges! Kérlek állíts be egy Google Gemini API kulcsot (Gemini:ApiKey) vagy OpenAI API kulcsot (OpenAI:ApiKey) a konfigurációban (appsettings.json vagy környezeti változók).");
            }

            return ParseAiResponse(jsonResponse);
        }

        private async Task<string> ScanWithGeminiAsync(List<(byte[] Bytes, string ContentType)> images, string apiKey)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            var parts = new List<object>
            {
                new { text = GetSystemPrompt() }
            };

            foreach (var img in images)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = img.ContentType,
                        data = Convert.ToBase64String(img.Bytes)
                    }
                });
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = parts.ToArray() }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.1
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API hiba ({StatusCode}): {Error}", response.StatusCode, errorText);
                throw new Exception($"Gemini Vision API hiba ({response.StatusCode}): {errorText}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var respParts) &&
                    respParts.GetArrayLength() > 0)
                {
                    return respParts[0].GetProperty("text").GetString() ?? "{}";
                }
            }

            throw new Exception("Nem érkezett érvényes válasz a Gemini API-tól.");
        }

        private async Task<string> ScanWithOpenAiAsync(List<(byte[] Bytes, string ContentType)> images, string apiKey)
        {
            var model = _configuration["OpenAI:VisionModel"] ?? "gpt-4o-mini";
            var client = new ChatClient(model, new ApiKeyCredential(apiKey));

            var contentParts = new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart(GetSystemPrompt())
            };

            foreach (var img in images)
            {
                contentParts.Add(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(img.Bytes), img.ContentType));
            }

            var messages = new List<ChatMessage>
            {
                new UserChatMessage(contentParts)
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
                Temperature = 0.1f
            };

            var completion = await client.CompleteChatAsync(messages, options);
            return completion.Value.Content[0].Text;
        }

        private static string GetSystemPrompt()
        {
            return @"You are an expert hair salon and cosmetic inventory scanner.
Analyze the provided photos of the product (bottle, box, labels, barcode, shade number).
Identify and extract the product information into a clean JSON object with the following exact keys:

{
  ""name"": ""string - Brand and product line (e.g. 'L'Oréal Professionnel Metal Detox Sampon', 'Olaplex No.2 Bond Perfector', 'Dia Light'). Do NOT put the shade/color code here if it has one."",
  ""shade"": ""string or null - Color shade code or tint variant if present (e.g. '15.2', '7.1 Blond', '6/00', 'No.1'). Null if not a colored/shaded product."",
  ""ean"": ""string or null - The numeric barcode (EAN-13, UPC) if visible on the bottle/box. Clean numbers only, or null if not visible."",
  ""packageSize"": number - Numeric volume or weight without unit (e.g. 500, 1500, 50, 100, 1). Default to 1 if unknown.,
  ""unit"": number - Enum integer: 0 for ml (Milliliter), 1 for g (Gram), 2 for pcs (Piece), 3 for m (Meter), 4 for cm (Centimeter). Default to 0 for liquids/creams/shampoos, 1 for powders/solid masks, 2 for tools/gloves.,
  ""isProfessional"": boolean - true if marked as salon exclusive, professional use only, or salon large pack,
  ""isRetail"": boolean - true if standard consumer retail packaging,
  ""description"": ""string or null - Brief 1-2 sentence Hungarian description or purpose of the product.""
}

IMPORTANT: Only return the raw JSON object. Do not wrap in markdown quotes if possible.";
        }

        private ProductAiScanResultDto ParseAiResponse(string rawJson)
        {
            try
            {
                var cleaned = rawJson.Trim();
                if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(7);
                }
                if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(3);
                }
                if (cleaned.EndsWith("```"))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - 3);
                }
                cleaned = cleaned.Trim();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var dto = JsonSerializer.Deserialize<ProductAiScanResultDto>(cleaned, options) 
                          ?? new ProductAiScanResultDto();

                // Alapértelmezések érvényesítése
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    dto.Name = "Felismeretlen termék";
                }
                if (dto.PackageSize <= 0)
                {
                    dto.PackageSize = 1;
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba az AI válasz deszerializálásakor: {Raw}", rawJson);
                throw new Exception($"Nem sikerült feldolgozni az AI választ: {ex.Message}");
            }
        }
    }
}
