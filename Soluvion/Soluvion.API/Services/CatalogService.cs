using System.ClientModel;
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

        public CatalogService(AppDbContext context, ITenantContext tenantContext, IConfiguration configuration)
        {
            _context = context;
            _tenantContext = tenantContext;
            _configuration = configuration;
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

        public async Task<string> GeneratePictogramSvgAsync(GeneratePictogramRequestDto dto)
        {
            int companyId = GetCurrentCompanyId();
            var company = await _context.Companies
                .Include(c => c.CompanyType)
                .FirstOrDefaultAsync(c => c.Id == companyId);

            string companyTypeName = company?.CompanyType?.Name ?? "Premium Hair & Beauty Salon";
            string svg = await GenerateSvgFromAiAsync(companyTypeName, dto.CategoryName, dto.ServiceName, dto.IsCategoryLevel);

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

            return svg;
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

            // 1. Kategória piktogramok pótlása
            var categoryGroups = services
                .GroupBy(s => GetLocalized(s.Category))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key));

            foreach (var group in categoryGroups)
            {
                var existingCatSvg = group.Select(s => s.CategoryPictogram).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
                if (string.IsNullOrWhiteSpace(existingCatSvg))
                {
                    existingCatSvg = await GenerateSvgFromAiAsync(companyTypeName, group.Key, null, true);
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

            // 2. Szolgáltatás piktogramok pótlása
            foreach (var s in services)
            {
                if (string.IsNullOrWhiteSpace(s.PictogramLink))
                {
                    var catName = GetLocalized(s.Category);
                    var srvName = GetLocalized(s.Name);
                    s.PictogramLink = await GenerateSvgFromAiAsync(companyTypeName, catName, srvName, false);
                    generatedCount++;
                }
            }

            if (generatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return generatedCount;
        }

        private async Task<string> GenerateSvgFromAiAsync(string companyType, string categoryName, string? serviceName, bool isCategory)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:TranslationModel"]
                     ?? _configuration["OpenAI:Model"]
                     ?? "gpt-4o-mini";

            string subject = isCategory || string.IsNullOrWhiteSpace(serviceName)
                ? $"Category: '{categoryName}'"
                : $"Service: '{serviceName}' (in category '{categoryName}')";

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var chatClient = new ChatClient(model, new ApiKeyCredential(apiKey));
                    string systemPrompt =
                        "You are an expert luxury icon designer for high-end beauty & hair salons. " +
                        "Design a single, clean, minimalist, fine-line vector SVG icon (24x24 viewBox) that visually represents the requested salon category or service. " +
                        "STRICT RULES:\n" +
                        "1. Output ONLY raw <svg>...</svg> code. No markdown, no backticks, no explanations.\n" +
                        "2. Root tag MUST be: <svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\">\n" +
                        "3. Use ONLY <path>, <circle>, <line>, <polyline>, <rect>, or <ellipse>. Never use <text>, <image>, <script>, or hardcoded colors.\n" +
                        "4. Make the icon recognizable, balanced, and elegant (e.g., scissors, hairdryer, coloring brush & bowl, comb, hair waves, sparkles, lotus/drop for keratin/botox, crown/updo, etc.).";

                    string userPrompt = $"Business Type: {companyType}. {subject}. Generate the minimalist 24x24 SVG icon now.";

                    var messages = new List<ChatMessage>
                    {
                        new SystemChatMessage(systemPrompt),
                        new UserChatMessage(userPrompt)
                    };

                    ChatCompletion completion = await chatClient.CompleteChatAsync(messages);
                    if (completion.Content != null && completion.Content.Count > 0)
                    {
                        var sanitized = SanitizeSvg(completion.Content[0].Text);
                        if (!string.IsNullOrWhiteSpace(sanitized))
                        {
                            return sanitized;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Pictogram AI fallback] {ex.Message}");
                }
            }

            return GetFallbackSvg(categoryName, serviceName);
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

