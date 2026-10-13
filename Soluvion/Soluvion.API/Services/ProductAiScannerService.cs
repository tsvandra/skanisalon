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
        private readonly IDocumentTextExtractorService _textExtractor;
        private readonly ILogger<ProductAiScannerService> _logger;

        public ProductAiScannerService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IDocumentTextExtractorService textExtractor,
            ILogger<ProductAiScannerService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _textExtractor = textExtractor;
            _logger = logger;
        }

        public async Task<ProductAiScanResultDto> ScanProductImagesAsync(IEnumerable<IFormFile> images)
        {
            var imageItems = await ConvertImagesAsync(images);
            var jsonResponse = await ExecutePromptAsync(imageItems, null, GetProductSystemPrompt());
            return ParseProductAiResponse(jsonResponse);
        }

        public Task<DeliveryNoteScanResultDto> ScanDeliveryNoteImagesAsync(IEnumerable<IFormFile> images)
        {
            return ScanDeliveryNoteFilesAsync(images);
        }

        public async Task<DeliveryNoteScanResultDto> ScanDeliveryNoteFilesAsync(IEnumerable<IFormFile> files)
        {
            var fileList = files?.ToList() ?? new List<IFormFile>();
            if (fileList.Count == 0)
            {
                throw new ArgumentException("Legalább egy fájlt vagy képet fel kell tölteni a szállítólevél beolvasásához.");
            }

            var visualItems = new List<(byte[] Bytes, string ContentType)>();
            var textBuilder = new StringBuilder();

            foreach (var file in fileList)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

                if (ext == ".pdf" || contentType == "application/pdf")
                {
                    // 1. PDF szöveg kinyerése (ha digitális PDF)
                    var pdfText = await _textExtractor.ExtractTextAsync(file);
                    if (!string.IsNullOrWhiteSpace(pdfText))
                    {
                        textBuilder.AppendLine(pdfText);
                        textBuilder.AppendLine();
                    }

                    // 2. A nyers PDF-et Gemini inline_data-ként is átadjuk (Gemini 2.0 natívan érti a PDF-et OCR-rel!)
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    visualItems.Add((ms.ToArray(), "application/pdf"));
                }
                else if (_textExtractor.IsExtractableDocument(file.FileName, contentType))
                {
                    // Excel (.xlsx, .xls), Word (.docx, .doc), CSV, TXT
                    var docText = await _textExtractor.ExtractTextAsync(file);
                    if (!string.IsNullOrWhiteSpace(docText))
                    {
                        textBuilder.AppendLine(docText);
                        textBuilder.AppendLine();
                    }
                }
                else
                {
                    // Kép (JPEG, PNG, WEBP stb.)
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    var imgContentType = contentType.StartsWith("image/") ? contentType : "image/jpeg";
                    visualItems.Add((ms.ToArray(), imgContentType));
                }
            }

            var jsonResponse = await ExecutePromptAsync(visualItems, textBuilder.ToString(), GetDeliveryNoteSystemPrompt());
            return ParseDeliveryNoteAiResponse(jsonResponse);
        }

        private static async Task<List<(byte[] Bytes, string ContentType)>> ConvertImagesAsync(IEnumerable<IFormFile> images)
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

        private async Task<string> ExecutePromptAsync(List<(byte[] Bytes, string ContentType)> visualItems, string? documentText, string systemPrompt)
        {
            var geminiKey = _configuration["Gemini:ApiKey"] ?? _configuration["AI:GeminiApiKey"];
            var openAiKey = _configuration["OpenAI:ApiKey"];

            // 1. Prioritás: Google Gemini (2.0 Flash / 1.5 Flash - ingyenes/filléres, szupergyors, natív PDF és táblázatkezelés)
            if (!string.IsNullOrWhiteSpace(geminiKey) && !geminiKey.Contains("IDE_JON"))
            {
                _logger.LogInformation("Dokumentumelemzés Google Gemini API segítségével ({VisualCount} vizuális elem, {TextLength} szöveghossz)...",
                    visualItems.Count, documentText?.Length ?? 0);
                return await ScanWithGeminiAsync(visualItems, documentText, geminiKey, systemPrompt);
            }
            // 2. Prioritás: OpenAI Vision / Chat
            if (!string.IsNullOrWhiteSpace(openAiKey) && !openAiKey.Contains("IDE_JON"))
            {
                _logger.LogInformation("Dokumentumelemzés OpenAI API segítségével ({VisualCount} vizuális elem, {TextLength} szöveghossz)...",
                    visualItems.Count, documentText?.Length ?? 0);
                return await ScanWithOpenAiAsync(visualItems, documentText, openAiKey, systemPrompt);
            }

            throw new InvalidOperationException(
                "Az AI dokumentum-felismeréshez API kulcs szükséges! Kérlek állíts be egy Google Gemini API kulcsot (Gemini:ApiKey) vagy OpenAI API kulcsot (OpenAI:ApiKey) a konfigurációban.");
        }

        private async Task<string> ScanWithGeminiAsync(List<(byte[] Bytes, string ContentType)> visualItems, string? documentText, string apiKey, string systemPrompt)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            var parts = new List<object>
            {
                new { text = systemPrompt }
            };

            if (!string.IsNullOrWhiteSpace(documentText))
            {
                parts.Add(new { text = "A feltöltött dokumentumok kinyert szöveges és táblázatos tartalma:\n\n" + documentText });
            }

            foreach (var item in visualItems)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = item.ContentType,
                        data = Convert.ToBase64String(item.Bytes)
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
                throw new Exception($"Gemini API hiba ({response.StatusCode}): {errorText}");
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

        private async Task<string> ScanWithOpenAiAsync(List<(byte[] Bytes, string ContentType)> visualItems, string? documentText, string apiKey, string systemPrompt)
        {
            var model = _configuration["OpenAI:VisionModel"] 
                     ?? _configuration["OpenAI:Model"] 
                     ?? "gpt-5.6-luna";
            var client = new ChatClient(model, new ApiKeyCredential(apiKey));

            var contentParts = new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart(systemPrompt)
            };

            if (!string.IsNullOrWhiteSpace(documentText))
            {
                contentParts.Add(ChatMessageContentPart.CreateTextPart("A feltöltött dokumentumok kinyert szöveges és táblázatos tartalma:\n\n" + documentText));
            }

            foreach (var img in visualItems)
            {
                // OpenAI csak képeket fogad inline_data-ként (PDF-et nem)
                if (img.ContentType.StartsWith("image/"))
                {
                    contentParts.Add(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(img.Bytes), img.ContentType));
                }
            }

            var messages = new List<ChatMessage>
            {
                new UserChatMessage(contentParts)
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
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

IMPORTANT: Only return the raw JSON object.";
        }

        private static string GetDeliveryNoteSystemPrompt()
        {
            return @"You are an expert delivery note ('dodací list', 'szállítólevél', 'faktúra', invoice) scanner for hair salons and beauty businesses.
Analyze the provided delivery note / invoice documents (which can be photos, scans, PDF, Word tables, or Excel sheets). These documents are usually in Slovak, Hungarian, German, or English.
Extract the header details, all available price columns, and every product line item into a clean JSON object with the following exact keys:

{
  ""documentNumber"": ""string or null - The delivery note / invoice number (e.g. 'DL2026/0451', '24010052', '12345')"",
  ""supplier"": ""string or null - The supplier / distributor company name (e.g. 'L'Oréal Slovensko s.r.o.', 'New Flag', 'Hair Professional')"",
  ""issueDate"": ""string or null - The document date in YYYY-MM-DD format if readable"",
  ""availablePriceColumns"": [
    {
      ""id"": ""string - Identifier for this price column (e.g. 'unit_net', 'unit_gross', 'catalog_price', 'discounted_price', 'total_net', 'total_gross')"",
      ""label"": ""string - Readable name in Slovak/Hungarian/English (e.g. 'Nettó egységár (Cena bez DPH)', 'Bruttó egységár (Cena s DPH)', 'Listaár (Cenníková cena)', 'Kedvezményes ár')"",
      ""isDefault"": boolean - true for the primary recommended purchase unit price column
    }
  ],
  ""defaultPriceColumnId"": ""string - Id of the recommended default purchase unit price column (preference: discounted net unit price -> standard net unit price -> gross unit price)"",
  ""items"": [
    {
      ""rawName"": ""string - Full exact description as printed on the delivery note row"",
      ""name"": ""string - Cleaned brand and product name (e.g. 'L'Oréal Dia Light', 'Schwarzkopf Igora Royal'). Exclude shade, code, or volume if separate."",
      ""code"": ""string or null - Catalog number / item code / article number if present (e.g. 'E12345', '10.12')"",
      ""shade"": ""string or null - Color shade / tint / nuance number if this is a hair dye / toner / color (e.g. '10.12', '7.1', '6/00', 'Clear')"",
      ""quantity"": number - Delivered quantity / amount of units or pieces (e.g. 1, 2, 6, 12). Must be positive integer or decimal.,
      ""unit"": number - Enum integer: 0 for ml, 1 for g, 2 for pcs (pieces/darab/ks), 3 for m, 4 for cm. Default to 0 for liquids/creams/shampoos, 1 for powder/bleach, 2 for tools/accessories/tubes.,
      ""packageSize"": number - Package volume or weight of ONE single piece/tube (e.g. 50, 60, 70, 100, 250, 500, 1000). Default to 1 if unknown or not applicable.,
      ""unitPrice"": number - Purchase price for 1 piece based on the default price column (e.g. 7.50). 0 if not listed.,
      ""totalPrice"": number - Total price for this line item (e.g. 15.00 for 2 * 7.50). 0 if not listed.,
      ""availablePrices"": {
        ""unit_net"": 7.50,
        ""unit_gross"": 9.00
      },
      ""ean"": ""string or null - EAN barcode if printed on the delivery note""
    }
  ]
}

CRITICAL RULES:
1. QUANTITY vs PACKAGE SIZE:
   - Carefully distinguish between the number of pieces (quantity) and package volume/size (packageSize).
   - Example 1: '2x70ml' or '2 ks 70ml' -> quantity = 2, packageSize = 70, unit = 0 (ml).
   - Example 2: '140ml' -> if it represents 2 tubes of 70ml (standard for Dia Light / hair color), set quantity = 2, packageSize = 70, unit = 0. If it's a single 140ml bottle, set quantity = 1, packageSize = 140, unit = 0.
   - Example 3: 'Sampon 1500ml' 1 ks -> quantity = 1, packageSize = 1500, unit = 0 (ml).
2. MULTIPLE PRICE COLUMNS:
   - Identify all distinct price columns in the document (e.g. Unit Price excl. VAT / Cena bez DPH, Unit Price incl. VAT / Cena s DPH, Catalog price, Discounted price, Total price).
   - Populate `availablePriceColumns` with all discovered columns.
   - Set `defaultPriceColumnId` to the best purchase unit price without tax (or with tax if no net price exists).
   - In each item, map every discovered column's price in `availablePrices`.
3. TOTAL & UNIT PRICES:
   - Ensure `unitPrice` and `totalPrice` are consistent (`totalPrice` ≈ `quantity` * `unitPrice`).
4. SHADES & NUANCE CODES:
   - For hair colors, tints, nuances, developers, powders, foundations, and cosmetics (e.g. '10.12', '7/1', '9-1', '6RR', '2.N Vanilla', '5.0 Világosbarna', 'Clear'):
   - ALWAYS extract the nuance/shade code or variant into `shade`.
   - If both a numeric/alphanumeric code and a descriptive name are present (e.g. '5.0' and 'Világosbarna' or '2.N' and 'Vanilla'), extract the standard format '[Code] [Name]' (e.g. '5.0 Világosbarna', '2.N Vanilla'), ensuring the code is first.
   - Do NOT omit the shade code when present.
5. Only return the raw JSON object.";
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
                _logger.LogError(ex, "Hiba az AI termékfelismerés deszerializálásakor: {Raw}", rawJson);
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
                dto.AvailablePriceColumns ??= new List<PriceColumnInfoDto>();

                if (dto.AvailablePriceColumns.Count == 0)
                {
                    dto.AvailablePriceColumns.Add(new PriceColumnInfoDto
                    {
                        Id = "unit_price",
                        Label = "Egységár (Jednotková cena)",
                        IsDefault = true
                    });
                    dto.DefaultPriceColumnId = "unit_price";
                }
                else if (string.IsNullOrEmpty(dto.DefaultPriceColumnId))
                {
                    var def = dto.AvailablePriceColumns.FirstOrDefault(c => c.IsDefault) ?? dto.AvailablePriceColumns.First();
                    dto.DefaultPriceColumnId = def.Id;
                }

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

                    // Kétirányú ár konszolidáció: ha az egyik hiányzik, kiszámítjuk
                    if (item.TotalPrice <= 0 && item.UnitPrice > 0)
                    {
                        item.TotalPrice = Math.Round(item.Quantity * item.UnitPrice, 2);
                    }
                    else if (item.UnitPrice <= 0 && item.TotalPrice > 0 && item.Quantity > 0)
                    {
                        item.UnitPrice = Math.Round(item.TotalPrice / item.Quantity, 2);
                    }

                    item.AvailablePrices ??= new Dictionary<string, decimal>();
                    if (!string.IsNullOrEmpty(dto.DefaultPriceColumnId) && !item.AvailablePrices.ContainsKey(dto.DefaultPriceColumnId))
                    {
                        item.AvailablePrices[dto.DefaultPriceColumnId] = item.UnitPrice;
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

