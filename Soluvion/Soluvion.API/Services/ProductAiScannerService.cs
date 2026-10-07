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
            var imageItems = await ConvertImagesAsync(images);
            var jsonResponse = await ExecuteVisionPromptAsync(imageItems, GetProductSystemPrompt());
            return ParseProductAiResponse(jsonResponse);
        }

        public async Task<DeliveryNoteScanResultDto> ScanDeliveryNoteImagesAsync(IEnumerable<IFormFile> images)
        {
            var imageItems = await ConvertImagesAsync(images);
            var jsonResponse = await ExecuteVisionPromptAsync(imageItems, GetDeliveryNoteSystemPrompt());
            return ParseDeliveryNoteAiResponse(jsonResponse);
        }

        private async Task<List<(byte[] Bytes, string ContentType)>> ConvertImagesAsync(IEnumerable<IFormFile> images)
        {
            var fileList = images?.ToList() ?? new List<IFormFile>();
            if (fileList.Count == 0)
            {
                throw new ArgumentException("Legalább egy képet fel kell tölteni a beolvasáshoz.");
            }

            var imageItems = new List<(byte[] Bytes, string ContentType)>();
            foreach (var file in fileList)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();
                var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType;
                imageItems.Add((bytes, contentType));
            }

            return imageItems;
        }

        private async Task<string> ExecuteVisionPromptAsync(List<(byte[] Bytes, string ContentType)> imageItems, string systemPrompt)
        {
            var geminiKey = _configuration["Gemini:ApiKey"] ?? _configuration["AI:GeminiApiKey"];
            var openAiKey = _configuration["OpenAI:ApiKey"];

            // 1. Prioritás: Google Gemini (2.0 Flash / 1.5 Flash - ingyenes/filléres, szupergyors)
            if (!string.IsNullOrWhiteSpace(geminiKey) && !geminiKey.Contains("IDE_JON"))
            {
                _logger.LogInformation("Képfelismerés Google Gemini API segítségével ({ImageCount} kép)...", imageItems.Count);
                return await ScanWithGeminiAsync(imageItems, geminiKey, systemPrompt);
            }
            // 2. Prioritás: OpenAI Vision (gpt-4o-mini / gpt-4o)
            if (!string.IsNullOrWhiteSpace(openAiKey) && !openAiKey.Contains("IDE_JON"))
            {
                _logger.LogInformation("Képfelismerés OpenAI Vision API segítségével ({ImageCount} kép)...", imageItems.Count);
                return await ScanWithOpenAiAsync(imageItems, openAiKey, systemPrompt);
            }

            throw new InvalidOperationException(
                "Az AI képfelismeréshez API kulcs szükséges! Kérlek állíts be egy Google Gemini API kulcsot (Gemini:ApiKey) vagy OpenAI API kulcsot (OpenAI:ApiKey) a konfigurációban (appsettings.json vagy környezeti változók).");
        }

        private async Task<string> ScanWithGeminiAsync(List<(byte[] Bytes, string ContentType)> images, string apiKey, string systemPrompt)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            var parts = new List<object>
            {
                new { text = systemPrompt }
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

        private async Task<string> ScanWithOpenAiAsync(List<(byte[] Bytes, string ContentType)> images, string apiKey, string systemPrompt)
        {
            var model = _configuration["OpenAI:VisionModel"] ?? "gpt-4o-mini";
            var client = new ChatClient(model, new ApiKeyCredential(apiKey));

            var contentParts = new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart(systemPrompt)
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

        private static string GetProductSystemPrompt()
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

        private static string GetDeliveryNoteSystemPrompt()
        {
            return @"You are an expert delivery note ('dodací list', 'szállítólevél', 'faktúra', invoice) scanner for hair salons and beauty businesses.
Analyze the provided images of the delivery note or invoice. These documents are usually in Slovak, Hungarian, German, or English.
Extract the header details and every product line item into a clean JSON object with the following exact keys:

{
  ""documentNumber"": ""string or null - The delivery note / invoice number (e.g. 'DL2026/0451', '24010052', '12345')"",
  ""supplier"": ""string or null - The supplier / distributor company name (e.g. 'L'Oréal Slovensko s.r.o.', 'New Flag', 'Hair Professional')"",
  ""issueDate"": ""string or null - The document date in YYYY-MM-DD format if readable"",
  ""items"": [
    {
      ""rawName"": ""string - Full exact description as printed on the delivery note row"",
      ""name"": ""string - Cleaned brand and product name (e.g. 'L'Oréal Dia Light', 'Schwarzkopf Igora Royal'). Exclude the shade code or quantity if distinct."",
      ""code"": ""string or null - Catalog number / item code / article number if present (e.g. 'E12345', '10.12')"",
      ""shade"": ""string or null - Color shade / tint / nuance number if this is a hair dye / toner / color (e.g. '10.12', '7.1', '6/00', 'Clear')"",
      ""quantity"": number - Delivered quantity / amount (e.g. 1, 3, 6, 12). Must be positive number,
      ""unitPrice"": number - Unit cost/purchase price without tax or with tax as listed (e.g. 7.50). 0 if not listed,
      ""unit"": number - Enum integer: 0 for ml, 1 for g, 2 for pcs (pieces/darab/ks), 3 for m, 4 for cm. Default to 2 (pieces/ks) for salon products/tubes, or 0 if indicated in ml,
      ""packageSize"": number - Package volume or weight if indicated in the item text (e.g. 50, 100, 250, 500, 1000). Default to 1.,
      ""ean"": ""string or null - EAN barcode if printed on the delivery note""
    }
  ]
}

IMPORTANT:
- Carefully inspect all line items on the sheet. Do not skip items.
- If a line is a hair color (e.g. 'DIA LIGHT 10.12 50ML' or 'IGORA 7-1 60ML'), cleanly separate the shade ('10.12' or '7-1') and the package size (50 or 60).
- Only return the raw JSON object.";
        }

        private static string CleanJson(string rawJson)
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
            return cleaned.Trim();
        }

        private static string? SanitizeValue(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            var trimmed = val.Trim();
            if (string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "undefined", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "n/a", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "ismeretlen", StringComparison.OrdinalIgnoreCase) ||
                trimmed == "-")
            {
                return null;
            }
            return trimmed;
        }

        private ProductAiScanResultDto ParseProductAiResponse(string rawJson)
        {
            try
            {
                var cleaned = CleanJson(rawJson);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var dto = JsonSerializer.Deserialize<ProductAiScanResultDto>(cleaned, options) 
                          ?? new ProductAiScanResultDto();

                dto.Shade = SanitizeValue(dto.Shade);
                dto.EAN = SanitizeValue(dto.EAN);
                dto.Description = SanitizeValue(dto.Description);

                if (string.IsNullOrWhiteSpace(dto.Name) || string.Equals(dto.Name, "null", StringComparison.OrdinalIgnoreCase))
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

        private DeliveryNoteScanResultDto ParseDeliveryNoteAiResponse(string rawJson)
        {
            try
            {
                var cleaned = CleanJson(rawJson);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var dto = JsonSerializer.Deserialize<DeliveryNoteScanResultDto>(cleaned, options) 
                          ?? new DeliveryNoteScanResultDto();

                dto.DocumentNumber = SanitizeValue(dto.DocumentNumber);
                dto.Supplier = SanitizeValue(dto.Supplier);
                dto.IssueDate = SanitizeValue(dto.IssueDate);
                dto.Items ??= new List<DeliveryNoteItemDto>();

                foreach (var item in dto.Items)
                {
                    item.Shade = SanitizeValue(item.Shade);
                    item.Code = SanitizeValue(item.Code);
                    item.EAN = SanitizeValue(item.EAN);

                    if (string.IsNullOrWhiteSpace(item.Name) || string.Equals(item.Name, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        item.Name = !string.IsNullOrWhiteSpace(item.RawName) ? item.RawName : "Ismeretlen tétel";
                    }
                    if (item.Quantity <= 0)
                    {
                        item.Quantity = 1;
                    }
                    if (item.PackageSize <= 0)
                    {
                        item.PackageSize = 1;
                    }
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba a szállítólevél AI válasz deszerializálásakor: {Raw}", rawJson);
                throw new Exception($"Nem sikerült feldolgozni a szállítólevél AI választ: {ex.Message}");
            }
        }
    }
}

