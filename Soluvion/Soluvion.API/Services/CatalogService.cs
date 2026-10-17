using System.ClientModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using Soluvion.API.Data;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;

namespace Soluvion.API.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        private static readonly string[] CreativeAngles = new[]
        {
            "Use a sleek 45-degree diagonal tool composition with subtle sparkle accents.",
            "Focus on the primary professional salon tool in a symmetrical front-facing silhouette.",
            "Combine the main tool with flowing S-curve hair waves or water droplets.",
            "Design an iconic, bold luxury emblem combining two complementary salon instruments.",
            "Use graceful organic curves paired with a crisp geometric tool outline."
        };

        public CatalogService(
            AppDbContext context,
            ITenantContext tenantContext,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _tenantContext = tenantContext;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        private int GetCurrentCompanyId() => _tenantContext.CurrentCompany?.Id ?? 0;

        public async Task<IEnumerable<CatalogDto>> GetCatalogAsync()
        {
            var services = await _context.Services
                .Include(s => s.Variants).ThenInclude(v => v.DefaultProducts).ThenInclude(dp => dp.Product)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();

            return services.Select(MapToDto).ToList();
        }

        public async Task<CatalogDto?> CreateCatalogItemAsync(CatalogDto dto)
        {
            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return null;

            var service = new Service
            {
                CompanyId = companyId,
                Name = dto.Name ?? new Dictionary<string, string> { { "hu", "Új szolgáltatás" } },
                Category = dto.Category != null && dto.Category.Any() ? dto.Category : new Dictionary<string, string> { { "hu", "Egyéb" } },
                Description = dto.Description ?? new Dictionary<string, string>(),
                DefaultPrice = dto.DefaultPrice ?? 0,
                DefaultDuration = dto.DefaultDuration,
                OrderIndex = dto.OrderIndex,
                PictogramLink = dto.PictogramLink ?? string.Empty,
                CategoryPictogram = dto.CategoryPictogram ?? string.Empty,
                IsBookable = dto.IsBookable,
                Variants = dto.Variants?.Select(v => new ServiceVariant
                {
                    VariantName = v.VariantName ?? new Dictionary<string, string> { { "hu", "Normál" } },
                    Price = v.Price ?? 0,
                    Duration = v.Duration,
                    ProfileModifiers = v.ProfileModifiers ?? new Dictionary<string, string>(),
                    DefaultProducts = v.DefaultProducts?.Select(dp => new ServiceVariantProduct
                    {
                        ProductId = dp.ProductId,
                        DefaultQuantity = dp.DefaultQuantity
                    }).ToList() ?? new List<ServiceVariantProduct>()
                }).ToList() ?? new List<ServiceVariant>()
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return MapToDto(service);
        }

        public async Task<CatalogDto?> UpdateCatalogItemAsync(int id, CatalogDto dto)
        {
            var existing = await _context.Services
                .Include(s => s.Variants).ThenInclude(v => v.DefaultProducts).ThenInclude(dp => dp.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existing == null) return null;

            // 1. Alap adatok frissítése
            existing.Name = dto.Name;
            existing.Category = dto.Category;
            existing.Description = dto.Description;
            existing.DefaultPrice = dto.DefaultPrice ?? 0;
            existing.DefaultDuration = dto.DefaultDuration;
            existing.OrderIndex = dto.OrderIndex;
            existing.PictogramLink = dto.PictogramLink ?? string.Empty;
            existing.CategoryPictogram = dto.CategoryPictogram ?? string.Empty;
            existing.IsBookable = dto.IsBookable;

            // 2. Variánsok törlése
            var incomingIds = dto.Variants?.Select(v => v.Id).ToList() ?? new List<int>();
            var toDelete = existing.Variants.Where(v => v.Id != 0 && !incomingIds.Contains(v.Id)).ToList();
            _context.ServiceVariants.RemoveRange(toDelete);

            // 3. Variánsok hozzáadása / frissítése
            if (dto.Variants != null)
            {
                foreach (var vDto in dto.Variants)
                {
                    if (vDto.Id == 0)
                    {
                        existing.Variants.Add(new ServiceVariant
                        {
                            VariantName = vDto.VariantName,
                            Price = vDto.Price ?? 0,
                            Duration = vDto.Duration,
                            ProfileModifiers = vDto.ProfileModifiers ?? new Dictionary<string, string>()
                        });
                    }
                    else
                    {
                        var existVar = existing.Variants.FirstOrDefault(x => x.Id == vDto.Id);
                        if (existVar != null)
                        {
                            existVar.VariantName = vDto.VariantName;
                            existVar.Price = vDto.Price ?? 0;
                            existVar.Duration = vDto.Duration;
                            existVar.ProfileModifiers = vDto.ProfileModifiers ?? new Dictionary<string, string>();
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return MapToDto(existing);
        }

        public async Task<bool> DeleteCatalogItemAsync(int id)
        {
            var s = await _context.Services.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return false;

            _context.Services.Remove(s);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateVariantRecipeAsync(int variantId, List<ServiceVariantProductDto> products)
        {
            var variant = await _context.ServiceVariants
                .Include(v => v.DefaultProducts)
                .FirstOrDefaultAsync(v => v.Id == variantId);

            if (variant == null) return false;

            _context.ServiceVariantProducts.RemoveRange(variant.DefaultProducts);
            if (products != null && products.Any())
            {
                var newLinks = products.Select(p => new ServiceVariantProduct
                {
                    ServiceVariantId = variantId,
                    ProductId = p.ProductId,
                    DefaultQuantity = p.DefaultQuantity
                }).ToList();
                _context.ServiceVariantProducts.AddRange(newLinks);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<GeneratePictogramResponseDto> GeneratePictogramSvgAsync(GeneratePictogramRequestDto dto)
        {
            int companyId = GetCurrentCompanyId();
            var company = await _context.Companies
                .Include(c => c.CompanyType)
                .FirstOrDefaultAsync(c => c.Id == companyId);

            string companyTypeName = company?.CompanyType?.Name ?? "Premium Hair & Beauty Salon";
            string normalizedMode = (dto.Mode ?? "svg").Trim().ToLowerInvariant();

            // 2-LÉPCSŐS MÓD: Képgenerálás (gpt-image-2.5-flare / sunburst) -> Kliensoldali Vektorizálás
            if (normalizedMode == "image" || normalizedMode == "image-vector")
            {
                var (base64Img, imgModel) = await GenerateIconImageFromAiAsync(
                    companyTypeName,
                    dto.CategoryName,
                    dto.ServiceName,
                    dto.IsCategoryLevel,
                    dto.SiblingServices);

                if (!string.IsNullOrWhiteSpace(base64Img))
                {
                    return new GeneratePictogramResponseDto
                    {
                        Base64Image = base64Img,
                        ModelUsed = imgModel,
                        Mode = "image"
                    };
                }
            }

            // 1-LÉPCSŐS MÓD (vagy fallback): GPT-6 Astra / GPT-6.1 Sol High-Reasoning közvetlen SVG kódgenerálás
            var (svg, svgModel) = await GenerateSvgFromAiAsync(
                companyTypeName,
                dto.CategoryName,
                dto.ServiceName,
                dto.IsCategoryLevel,
                dto.PreviousSvg,
                dto.SiblingServices,
                dto.PreferredModel);

            if (dto.IsCategoryLevel)
            {
                var allServices = await _context.Services.ToListAsync();
                foreach (var s in allServices)
                {
                    if (s.Category != null && s.Category.Values.Any(v => string.Equals(v?.Trim(), dto.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        s.CategoryPictogram = svg;
                    }
                }
                await _context.SaveChangesAsync();
            }
            else if (dto.ServiceId.HasValue && dto.ServiceId.Value > 0)
            {
                var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == dto.ServiceId.Value);
                if (service != null)
                {
                    service.PictogramLink = svg;
                    await _context.SaveChangesAsync();
                }
            }

            return new GeneratePictogramResponseDto
            {
                Svg = svg,
                ModelUsed = svgModel,
                Mode = "svg"
            };
        }

        public async Task<int> GenerateMissingPictogramsAsync(string? languageCode)
        {
            int companyId = GetCurrentCompanyId();
            var company = await _context.Companies
                .Include(c => c.CompanyType)
                .FirstOrDefaultAsync(c => c.Id == companyId);

            string companyTypeName = company?.CompanyType?.Name ?? "Premium Hair & Beauty Salon";
            string defaultLang = !string.IsNullOrWhiteSpace(languageCode)
                ? languageCode
                : (company?.DefaultLanguage ?? "hu");

            var services = await _context.Services.OrderBy(s => s.OrderIndex).ToListAsync();
            int generatedCount = 0;

            string GetLocalized(Dictionary<string, string>? dict)
            {
                if (dict == null || dict.Count == 0) return string.Empty;
                if (dict.TryGetValue(defaultLang, out var val) && !string.IsNullOrWhiteSpace(val)) return val;
                if (dict.TryGetValue("hu", out var huVal) && !string.IsNullOrWhiteSpace(huVal)) return huVal;
                return dict.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
            }

            var categoryGroups = services
                .GroupBy(s => GetLocalized(s.Category))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            // Csak a fő kategóriák piktogramjainak pótlása (az alkategóriáknak / szolgáltatásoknak nem kell külön piktogram)
            foreach (var group in categoryGroups)
            {
                var siblingNames = group.Select(s => GetLocalized(s.Name)).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                var existingCatSvg = group.Select(s => s.CategoryPictogram).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
                if (string.IsNullOrWhiteSpace(existingCatSvg))
                {
                    var (catSvg, _) = await GenerateSvgFromAiAsync(companyTypeName, group.Key, null, true, null, siblingNames, "gpt-6-astra");
                    existingCatSvg = catSvg;
                    generatedCount++;
                }

                foreach (var s in group)
                {
                    if (string.IsNullOrWhiteSpace(s.CategoryPictogram))
                    {
                        s.CategoryPictogram = existingCatSvg;
                    }
                }
            }

            if (generatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return generatedCount;
        }

        private async Task<(string Svg, string ModelUsed)> GenerateSvgFromAiAsync(
            string companyType,
            string categoryName,
            string? serviceName,
            bool isCategory,
            string? previousSvg,
            List<string>? siblingServices,
            string? preferredModel = "gpt-6-astra")
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("IDE_JON"))
            {
                return (GetFallbackSvg(categoryName, serviceName), "fallback");
            }

            // Modell prioritási sorrend: választott modell (alapból GPT-6 Astra) -> GPT-6.1 Sol -> konfigurált modell -> GPT-4o
            var candidateModels = new List<string>();
            if (!string.IsNullOrWhiteSpace(preferredModel))
            {
                candidateModels.Add(preferredModel.Trim());
            }
            var configuredPictogramModel = _configuration["OpenAI:PictogramModel"];
            if (!string.IsNullOrWhiteSpace(configuredPictogramModel)) candidateModels.Add(configuredPictogramModel);
            candidateModels.Add("gpt-6-astra");
            candidateModels.Add("gpt-6.1-sol");
            var configuredDefault = _configuration["OpenAI:Model"];
            if (!string.IsNullOrWhiteSpace(configuredDefault) && !candidateModels.Contains(configuredDefault))
                candidateModels.Add(configuredDefault);
            candidateModels.Add("gpt-4o");

            string subject = isCategory || string.IsNullOrWhiteSpace(serviceName)
                ? $"Salon Category: '{categoryName}'"
                : $"Specific Salon Treatment: '{serviceName}' (inside Category: '{categoryName}')";

            string creativeHint = CreativeAngles[Random.Shared.Next(CreativeAngles.Length)];
            int randomSeed = Random.Shared.Next(1000, 9999);

            var siblingContext = (siblingServices != null && siblingServices.Count > 0)
                ? $"\nTreatments included in this category: [{string.Join(", ", siblingServices.Take(10))}]. Design an iconic, clean symbol that represents this category at a glance (even at 16px-24px size) and is visually distinct from other salon categories."
                : string.Empty;

            var regenerationContext = !string.IsNullOrWhiteSpace(previousSvg)
                ? $"\nREGENERATION REQUEST (Variation #{randomSeed}): The user wants a COMPLETELY NEW visual concept, different tool/symbol, and different geometry from the previous attempt(s)! Previous SVG(s) to avoid repeating:\n{previousSvg}\nCreative direction: {creativeHint}"
                : $"\nCreative direction (Seed #{randomSeed}): {creativeHint}";

            string systemPrompt =
                "You are a master vector iconographer for luxury hair & beauty salons (like Dyson, Kerastase, Oribe). " +
                "Your task is to write a multi-element, geometrically precise, recognizable 24x24 monoline <svg> icon.\n" +
                "STRICT ARCHITECTURAL & VISUAL RULES:\n" +
                "1. Output ONLY raw <svg>...</svg> markup. No markdown fences, no explanations.\n" +
                "2. Exact root element: <svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.65\" stroke-linecap=\"round\" stroke-linejoin=\"round\">\n" +
                "3. Compose the icon from 3 to 7 well-proportioned vector primitives (<path>, <circle>, <rect>, <line>, <ellipse>) using coordinates between 2 and 22 so it fills the 24x24 canvas with balanced padding.\n" +
                "4. ANATOMICAL PRECISION FOR SALON SERVICES:\n" +
                "   - Blowdry / Hajfújás / Szárítás: Draw a distinct salon hairdryer profile (cylindrical barrel + angled handle + concentrator nozzle + 2 wavy airflow lines).\n" +
                "   - Straightening / Vasalás: Draw an open V-shaped flat iron (two sleek heated plates hinged at one end + a vertical straight hair strand + tiny sparkle).\n" +
                "   - Wash & Waves / Mosás, hullám: Draw flowing S-curve hair locks paired with 2 delicate water droplets or foam bubbles.\n" +
                "   - Haircut / Hajvágás: Distinguish between Wet Cut (scissors + water drop/comb), Dry Cut (shears + clean geometric hair silhouette), Men/Barber (electric clipper or comb + mustache/razor), Children (smaller playful shears + star).\n" +
                "   - Coloring / Balayage / Melír: Draw a tinting bowl + angled coloring brush or foil sectioning with a droplet.\n" +
                "   - Keratin / Botox / Treatment: Draw a luxury serum dropper/pipette or a glowing lotus/shield droplet.\n" +
                "   - Updo / Bridal / Braids: Draw an intertwined 3-strand braid pattern or an elegant tiara/crown with hair curves.\n" +
                "5. Never use <text>, <image>, <script>, or hardcoded hex colors.";

            string userPrompt =
                $"Business Type: {companyType}.\n" +
                $"Target to illustrate: {subject}.{siblingContext}{regenerationContext}\n" +
                "Design and return the raw 24x24 <svg> code now.";

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            foreach (var model in candidateModels.Distinct())
            {
                try
                {
                    bool supportsReasoning = model.Contains("sol", StringComparison.OrdinalIgnoreCase)
                                          || model.Contains("astra", StringComparison.OrdinalIgnoreCase)
                                          || model.StartsWith("o", StringComparison.OrdinalIgnoreCase);

                    object requestPayload = supportsReasoning
                        ? new
                        {
                            model,
                            reasoning_effort = "high",
                            messages = new object[]
                            {
                                new { role = "system", content = systemPrompt },
                                new { role = "user", content = userPrompt }
                            }
                        }
                        : new
                        {
                            model,
                            temperature = 0.85,
                            messages = new object[]
                            {
                                new { role = "system", content = systemPrompt },
                                new { role = "user", content = userPrompt }
                            }
                        };

                    var content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                    // Ha a modell nem támogatja a reasoning_effort mezőt, próbáljuk meg anélkül ugyanazzal a modellel
                    if (!response.IsSuccessStatusCode && supportsReasoning)
                    {
                        var fallbackPayload = new
                        {
                            model,
                            temperature = 0.85,
                            messages = new object[]
                            {
                                new { role = "system", content = systemPrompt },
                                new { role = "user", content = userPrompt }
                            }
                        };
                        content = new StringContent(JsonSerializer.Serialize(fallbackPayload), Encoding.UTF8, "application/json");
                        response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var text = choices[0].GetProperty("message").GetProperty("content").GetString();
                        var sanitized = SanitizeSvg(text);
                        if (!string.IsNullOrWhiteSpace(sanitized))
                        {
                            return (sanitized, model);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Pictogram SVG AI ({model})] {ex.Message}");
                }
            }

            return (GetFallbackSvg(categoryName, serviceName), "fallback");
        }

        private async Task<(string? Base64Image, string ModelUsed)> GenerateIconImageFromAiAsync(
            string companyType,
            string categoryName,
            string? serviceName,
            bool isCategory,
            List<string>? siblingServices)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("IDE_JON"))
            {
                return (null, "none");
            }

            var candidateImageModels = new List<string>();
            var configuredImgModel = _configuration["OpenAI:ImageModel"];
            if (!string.IsNullOrWhiteSpace(configuredImgModel)) candidateImageModels.Add(configuredImgModel);
            candidateImageModels.Add("gpt-image-2.5-flare");
            candidateImageModels.Add("gpt-image-2.5-sunburst");
            candidateImageModels.Add("gpt-image-1");
            candidateImageModels.Add("dall-e-3");

            string subject = isCategory || string.IsNullOrWhiteSpace(serviceName)
                ? $"salon category '{categoryName}'"
                : $"salon service '{serviceName}' (category: '{categoryName}')";

            string creativeHint = CreativeAngles[Random.Shared.Next(CreativeAngles.Length)];
            int randomSeed = Random.Shared.Next(1000, 9999);

            string prompt =
                $"A single minimalist luxury icon representing {subject} for a {companyType} (Variation #{randomSeed}). " +
                $"{creativeHint} " +
                "Flat bold black monoline vector stencil style on a pure solid white background (#FFFFFF). " +
                "Single centered object, thick uniform clean lines, high contrast, no text, no letters, no numbers, no shading, no gradients, no border box.";

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(45);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            foreach (var model in candidateImageModels.Distinct())
            {
                try
                {
                    object payload = model.StartsWith("gpt-image", StringComparison.OrdinalIgnoreCase)
                        ? new
                        {
                            model,
                            prompt,
                            size = "1024x1024",
                            background = "transparent",
                            output_format = "png"
                        }
                        : new
                        {
                            model,
                            prompt,
                            n = 1,
                            size = "1024x1024",
                            response_format = "b64_json"
                        };

                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("https://api.openai.com/v1/images/generations", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.GetArrayLength() > 0)
                    {
                        var first = dataArr[0];
                        if (first.TryGetProperty("b64_json", out var b64Prop) && !string.IsNullOrWhiteSpace(b64Prop.GetString()))
                        {
                            return ($"data:image/png;base64,{b64Prop.GetString()}", model);
                        }
                        if (first.TryGetProperty("url", out var urlProp) && !string.IsNullOrWhiteSpace(urlProp.GetString()))
                        {
                            var imgBytes = await httpClient.GetByteArrayAsync(urlProp.GetString());
                            return ($"data:image/png;base64,{Convert.ToBase64String(imgBytes)}", model);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Pictogram Image AI ({model})] {ex.Message}");
                }
            }

            return (null, "none");
        }

        private static string SanitizeSvg(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            var match = Regex.Match(raw, @"<svg[\s\S]*?<\/svg>", RegexOptions.IgnoreCase);
            if (!match.Success) return string.Empty;

            var svg = match.Value;
            // XSS védelem: tilos script vagy inline event attribútum
            if (Regex.IsMatch(svg, @"<script|onload|onerror|onclick|javascript:", RegexOptions.IgnoreCase))
            {
                return string.Empty;
            }

            // Biztosítjuk a currentColor stroke-ot
            if (!svg.Contains("viewBox", StringComparison.OrdinalIgnoreCase))
            {
                svg = Regex.Replace(svg, @"^<svg", "<svg viewBox=\"0 0 24 24\"", RegexOptions.IgnoreCase);
            }

            return svg.Trim();
        }

        private static string GetFallbackSvg(string categoryName, string? serviceName)
        {
            string combined = $"{categoryName} {serviceName}".ToLowerInvariant();

            // Festés / Balayage / Melír / Szőkítés (Ecset + tálka / csepp)
            if (combined.Contains("fest") || combined.Contains("color") || combined.Contains("farb") || combined.Contains("balayage") || combined.Contains("melír") || combined.Contains("melir") || combined.Contains("szők") || combined.Contains("odfarb"))
            {
                return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m19 11-8-8-8.6 8.6a2 2 0 0 0 0 2.8l5.2 5.2c.8.8 2 .8 2.8 0L19 11Z\"/><path d=\"m5 2 5 5\"/><path d=\"M2 13h15\"/><path d=\"M22 20a2 2 0 1 1-4 0c0-1.6 1.7-2.4 2-4 .3 1.6 2 2.4 2 4Z\"/></svg>";
            }

            // Vágás / Férfi / Női (Olló)
            if (combined.Contains("vág") || combined.Contains("vag") || combined.Contains("strih") || combined.Contains("cut") || combined.Contains("férfi") || combined.Contains("pánsk"))
            {
                return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"6\" cy=\"6\" r=\"3\"/><path d=\"M8.12 8.12 12 12\"/><path d=\"M20 4 8.12 15.88\"/><circle cx=\"6\" cy=\"18\" r=\"3\"/><path d=\"M14.8 14.8 20 20\"/></svg>";
            }

            // Kezelés / Keratin / Botox / Regeneráló / Ampulla (Csillogó csepp / Spa)
            if (combined.Contains("kezel") || combined.Contains("keratin") || combined.Contains("botox") || combined.Contains("kúrá") || combined.Contains("kura") || combined.Contains("ošetren") || combined.Contains("regener"))
            {
                return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z\"/><path d=\"M12 12v4\"/><path d=\"M10 14h4\"/></svg>";
            }

            // Szárítás / Beszárítás / Hullám / Alkalmi / Konty
            if (combined.Contains("szárít") || combined.Contains("szarit") || combined.Contains("fúkan") || combined.Contains("konty") || combined.Contains("alkalmi") || combined.Contains("účes") || combined.Contains("style"))
            {
                return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M9.937 15.5A2 2 0 0 0 8.5 14.063l-6.135-1.582a.5.5 0 0 1 0-.962L8.5 9.936A2 2 0 0 0 9.937 8.5l1.582-6.135a.5.5 0 0 1 .963 0L14.063 8.5A2 2 0 0 0 15.5 9.937l6.135 1.581a.5.5 0 0 1 0 .964L15.5 14.063a2 2 0 0 0-1.437 1.437l-1.582 6.135a.5.5 0 0 1-.963 0z\"/><path d=\"M20 3v4\"/><path d=\"M22 5h-4\"/></svg>";
            }

            // Információ / Fontos
            if (combined.Contains("info") || combined.Contains("fontos") || combined.Contains("dôlež"))
            {
                return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 16v-4\"/><path d=\"M12 8h.01\"/></svg>";
            }

            // Alapértelmezett prémium fésű & csillag piktogram
            return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 3l1.9 5.8a2 2 0 0 0 1.3 1.3L21 12l-5.8 1.9a2 2 0 0 0-1.3 1.3L12 21l-1.9-5.8a2 2 0 0 0-1.3-1.3L3 12l5.8-1.9a2 2 0 0 0 1.3-1.3L12 3z\"/></svg>";
        }

        private CatalogDto MapToDto(Service s)
        {
            return new CatalogDto
            {
                Id = s.Id,
                CompanyId = s.CompanyId,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                DefaultPrice = s.DefaultPrice,
                DefaultDuration = s.DefaultDuration,
                OrderIndex = s.OrderIndex,
                PictogramLink = s.PictogramLink ?? string.Empty,
                CategoryPictogram = s.CategoryPictogram ?? string.Empty,
                IsBookable = s.IsBookable,
                Variants = s.Variants?.Select(v => new CatalogVariantDto
                {
                    Id = v.Id,
                    VariantName = v.VariantName,
                    Price = v.Price,
                    Duration = v.Duration,
                    ProfileModifiers = v.ProfileModifiers ?? new Dictionary<string, string>(),
                    DefaultProducts = v.DefaultProducts?.Select(dp => new ServiceVariantProductDto
                    {
                        ProductId = dp.ProductId,
                        ProductName = dp.Product?.Name,
                        DefaultQuantity = dp.DefaultQuantity
                    }).ToList() ?? new List<ServiceVariantProductDto>()
                }).ToList() ?? new List<CatalogVariantDto>()
            };
        }
    }
}

