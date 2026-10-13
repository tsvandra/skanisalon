using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.Domain.Models;
using Soluvion.API.DTOs;
using Soluvion.Domain.Models.Enums;
using System.Security.Claims;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITranslationService _translationService;
        private readonly IUserContext _userContext;
        private readonly ILogger<TranslationController> _logger;

        public TranslationController(
            AppDbContext context, 
            IServiceScopeFactory scopeFactory, 
            ITranslationService translationService,
            IUserContext userContext,
            ILogger<TranslationController> logger)
        {
            _context = context;
            _scopeFactory = scopeFactory;
            _translationService = translationService;
            _userContext = userContext;
            _logger = logger;
        }

        // --- VARÁZSPÁLCA VÉGPONT ---
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> TranslateSingleText([FromBody] TranslationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Text)) return BadRequest(new { errorCode = "TEXT_REQUIRED" });

            try
            {
                int? companyId = _userContext.CompanyId;
                if (!companyId.HasValue || companyId.Value <= 0)
                {
                    return Unauthorized(new { errorCode = "INVALID_SESSION" });
                }

                var company = await _context.Companies
                    .Include(c => c.CompanyType)
                    .FirstOrDefaultAsync(c => c.Id == companyId.Value);

                string companyType = company?.CompanyType?.Name ?? "General Business";

                string translatedText = await _translationService.TranslateTextAsync(
                    request.Text,
                    request.TargetLanguage,
                    request.Context ?? "general",
                    companyType
                );

                return Ok(new { translatedText = translatedText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Translation] Translation failed: {Message}", ex.Message);
                return StatusCode(500, new { errorCode = "TRANSLATION_FAILED", detail = ex.Message });
            }
        }
        // ---------------------------------------------------------------

        [HttpGet("languages/{companyId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<LanguageStatusDto>>> GetCompanyLanguages(int companyId)
        {
            var company = await _context.Companies
                .Include(c => c.Languages)
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var defaultLangCode = company.DefaultLanguage ?? "sk";
            var coreCodes = new[] { "hu", "sk", "en" };
            var result = new List<LanguageStatusDto>();

            // 1. Alapértelmezett nyelv (mindig Published, mindig IsDefault: true, IsSystem: true ha gyári)
            result.Add(new LanguageStatusDto
            {
                LanguageCode = defaultLangCode,
                Status = TranslationStatus.Published.ToString(),
                Progress = 100,
                IsDefault = true,
                IsSystem = coreCodes.Contains(defaultLangCode)
            });

            // 2. A többi gyári alapnyelv a 3-ból (ha nem a default)
            foreach (var coreCode in coreCodes)
            {
                if (coreCode == defaultLangCode) continue;

                var existingLang = company.Languages.FirstOrDefault(l => l.LanguageCode == coreCode);
                result.Add(new LanguageStatusDto
                {
                    LanguageCode = coreCode,
                    Status = existingLang != null ? existingLang.Status.ToString() : TranslationStatus.Inactive.ToString(),
                    IsDefault = false,
                    IsSystem = true,
                    Progress = 100
                });
            }

            // 3. Minden egyéb egyedi hozzáadott nyelv (pl. "de", "ro", "ua")
            foreach (var lang in company.Languages)
            {
                if (coreCodes.Contains(lang.LanguageCode)) continue;
                if (lang.LanguageCode == defaultLangCode) continue;

                result.Add(new LanguageStatusDto
                {
                    LanguageCode = lang.LanguageCode,
                    Status = lang.Status.ToString(),
                    IsDefault = false,
                    IsSystem = false,
                    Progress = lang.Progress
                });
            }

            return Ok(result);
        }

        [HttpPost("toggle-status")]
        [Authorize]
        public async Task<IActionResult> ToggleLanguageStatus([FromBody] ToggleLanguageStatusDto dto)
        {
            var company = await _context.Companies
                .Include(c => c.Languages)
                .FirstOrDefaultAsync(c => c.Id == dto.CompanyId);

            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            if (company.DefaultLanguage == dto.LanguageCode && !dto.IsActive)
            {
                return BadRequest(new { errorCode = "CANNOT_DEACTIVATE_DEFAULT_LANGUAGE" });
            }

            var langEntity = company.Languages.FirstOrDefault(l => l.LanguageCode == dto.LanguageCode);
            if (langEntity == null)
            {
                langEntity = new CompanyLanguage
                {
                    CompanyId = dto.CompanyId,
                    LanguageCode = dto.LanguageCode,
                    Status = dto.IsActive ? TranslationStatus.Published : TranslationStatus.Inactive,
                    Progress = 100,
                    LastUpdated = DateTime.UtcNow
                };
                _context.CompanyLanguages.Add(langEntity);
            }
            else
            {
                langEntity.Status = dto.IsActive ? TranslationStatus.Published : TranslationStatus.Inactive;
                langEntity.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, languageCode = dto.LanguageCode, isActive = dto.IsActive });
        }

        [HttpPost("set-default")]
        [Authorize]
        public async Task<IActionResult> SetDefaultLanguage([FromBody] SetDefaultLanguageDto dto)
        {
            var company = await _context.Companies
                .Include(c => c.Languages)
                .FirstOrDefaultAsync(c => c.Id == dto.CompanyId);

            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var coreCodes = new[] { "hu", "sk", "en" };
            var langEntity = company.Languages.FirstOrDefault(l => l.LanguageCode == dto.LanguageCode);

            if (langEntity == null && !coreCodes.Contains(dto.LanguageCode))
            {
                return BadRequest(new { errorCode = "LANGUAGE_NOT_FOUND" });
            }

            company.DefaultLanguage = dto.LanguageCode;

            // Az új alapértelmezett nyelvnek kötelezően Published (aktív) státuszúnak kell lennie
            if (langEntity != null)
            {
                langEntity.Status = TranslationStatus.Published;
                langEntity.LastUpdated = DateTime.UtcNow;
            }
            else if (coreCodes.Contains(dto.LanguageCode))
            {
                _context.CompanyLanguages.Add(new CompanyLanguage
                {
                    CompanyId = dto.CompanyId,
                    LanguageCode = dto.LanguageCode,
                    Status = TranslationStatus.Published,
                    Progress = 100,
                    LastUpdated = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, defaultLanguage = dto.LanguageCode });
        }

        [HttpPost("add-language")]
        public async Task<IActionResult> AddLanguage([FromBody] AddLanguageDto dto)
        {
            var company = await _context.Companies
                .Include(c => c.Languages)
                .FirstOrDefaultAsync(c => c.Id == dto.CompanyId);

            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var initialStatus = dto.UseAi ? TranslationStatus.Translating : TranslationStatus.ReviewPending;

            var existing = company.Languages.FirstOrDefault(l => l.LanguageCode == dto.TargetLanguage);
            if (existing != null)
            {
                existing.Status = initialStatus;
            }
            else
            {
                var newLang = new CompanyLanguage
                {
                    CompanyId = dto.CompanyId,
                    LanguageCode = dto.TargetLanguage,
                    Status = initialStatus,
                    LastUpdated = DateTime.UtcNow
                };
                _context.CompanyLanguages.Add(newLang);
            }

            await _context.SaveChangesAsync();

            if (dto.UseAi)
            {
                // Háttérszál indítása
                _ = Task.Run(() => PerformBackgroundTranslation(dto.CompanyId, dto.TargetLanguage, dto.BaseUiTranslations));
                return Accepted(new { success = true, status = "Translating" });
            }
            else
            {
                return Ok(new { success = true, status = "Created" });
            }
        }

        [HttpDelete("language/{companyId}/{langCode}")]
        [Authorize]
        public async Task<IActionResult> DeleteLanguage(int companyId, string langCode)
        {
            var company = await _context.Companies.Include(c => c.Languages).FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });
            if (company.DefaultLanguage == langCode) return BadRequest(new { errorCode = "CANNOT_DELETE_DEFAULT_LANGUAGE" });

            var coreCodes = new[] { "hu", "sk", "en" };
            if (coreCodes.Contains(langCode))
            {
                return BadRequest(new { errorCode = "CANNOT_DELETE_SYSTEM_LANGUAGE" });
            }

            var langEntity = company.Languages.FirstOrDefault(l => l.LanguageCode == langCode);
            if (langEntity != null) _context.CompanyLanguages.Remove(langEntity);

            var overrides = await _context.UiTranslationOverrides
                .Where(o => o.CompanyId == companyId && o.LanguageCode == langCode)
                .ToListAsync();
            _context.UiTranslationOverrides.RemoveRange(overrides);

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("publish")]
        [Authorize]
        public async Task<IActionResult> PublishLanguage([FromBody] PublishLanguageDto dto)
        {
            var langEntity = await _context.CompanyLanguages
                .FirstOrDefaultAsync(cl => cl.CompanyId == dto.CompanyId && cl.LanguageCode == dto.LanguageCode);

            if (langEntity == null) return NotFound(new { errorCode = "LANGUAGE_NOT_FOUND" });

            langEntity.Status = TranslationStatus.Published;
            langEntity.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("overrides/{companyId}/{langCode}")]
        public async Task<ActionResult<Dictionary<string, string>>> GetOverrides(int companyId, string langCode)
        {
            var overrides = await _context.UiTranslationOverrides
                .Where(o => o.CompanyId == companyId && o.LanguageCode == langCode)
                .ToDictionaryAsync(o => o.TranslationKey, o => o.TranslatedText);

            return Ok(overrides);
        }

        [HttpDelete("overrides/{companyId}/{langCode}")]
        [Authorize]
        public async Task<IActionResult> ResetOverrides(int companyId, string langCode, [FromQuery] bool includeBranding = false)
        {
            var query = _context.UiTranslationOverrides
                .Where(o => o.CompanyId == companyId && o.LanguageCode == langCode);

            // BIZTONSÁGI VÉDELEM: A tenant saját arculati/bemutatkozó szövegeit (home.*) tilos rendszerszinten törölni
            if (!includeBranding)
            {
                query = query.Where(o => !o.TranslationKey.StartsWith("home."));
            }

            var overrides = await query.ToListAsync();

            if (overrides.Count > 0)
            {
                _context.UiTranslationOverrides.RemoveRange(overrides);
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, deletedCount = overrides.Count });
        }

        [HttpPost("save-override")]
        public async Task<IActionResult> SaveOverride([FromBody] UiTranslationOverrideDto dto)
        {
            var existing = await _context.UiTranslationOverrides
                .FirstOrDefaultAsync(o => o.CompanyId == dto.CompanyId &&
                                          o.LanguageCode == dto.LanguageCode &&
                                          o.TranslationKey == dto.Key);

            if (string.IsNullOrWhiteSpace(dto.Value))
            {
                if (existing != null)
                {
                    _context.UiTranslationOverrides.Remove(existing);
                    await _context.SaveChangesAsync();
                }
                return Ok(new { success = true, removed = true });
            }

            if (existing != null)
            {
                existing.TranslatedText = dto.Value;
            }
            else
            {
                _context.UiTranslationOverrides.Add(new UiTranslationOverride
                {
                    CompanyId = dto.CompanyId,
                    LanguageCode = dto.LanguageCode,
                    TranslationKey = dto.Key,
                    TranslatedText = dto.Value
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // --- HÁTTÉRFOLYAMAT JAVÍTVA ---
        private async Task PerformBackgroundTranslation(int companyId, string targetLanguage, Dictionary<string, string> uiTranslations)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var translationService = scope.ServiceProvider.GetRequiredService<ITranslationService>();

                try
                {
                    var company = await dbContext.Companies.FindAsync(companyId);
                    if (company == null) return;

                    string companyType = "General Business";
                    var coreCodes = new[] { "hu", "sk", "en" };
                    bool translateUi = !coreCodes.Contains(targetLanguage) && uiTranslations != null && uiTranslations.Count > 0;

                    int totalItems = (translateUi ? uiTranslations!.Count : 0)
                                     + (await dbContext.Services.CountAsync(s => s.CompanyId == companyId) * 3)
                                     + (await dbContext.GalleryCategories.CountAsync(g => g.CompanyId == companyId));

                    if (totalItems == 0) totalItems = 1;
                    int currentItem = 0;

                    async Task UpdateProgress()
                    {
                        currentItem++;
                        if (currentItem % 5 == 0 || currentItem == totalItems)
                        {
                            var langToUpdate = await dbContext.CompanyLanguages.FindAsync(companyId, targetLanguage);
                            if (langToUpdate != null)
                            {
                                langToUpdate.Progress = (int)((double)currentItem / totalItems * 100);
                                if (langToUpdate.Progress > 99) langToUpdate.Progress = 99;
                                await dbContext.SaveChangesAsync();
                            }
                        }
                    }

                    // A) UI Overrides (Kizárólag 4.+ egyedi nyelvekhez, a HU, SK, EN gyári csomagokat nem írjuk felül AI-jal)
                    if (translateUi)
                    {
                        foreach (var kvp in uiTranslations!)
                        {
                            if (!string.IsNullOrWhiteSpace(kvp.Value))
                            {
                                string translated = await translationService.TranslateTextAsync(kvp.Value, targetLanguage, "ui", companyType);

                                var ov = await dbContext.UiTranslationOverrides.FirstOrDefaultAsync(o => o.CompanyId == companyId && o.LanguageCode == targetLanguage && o.TranslationKey == kvp.Key);
                                if (ov == null) dbContext.UiTranslationOverrides.Add(new UiTranslationOverride { CompanyId = companyId, LanguageCode = targetLanguage, TranslationKey = kvp.Key, TranslatedText = translated });
                                else ov.TranslatedText = translated;
                            }
                            await UpdateProgress();
                        }
                        await dbContext.SaveChangesAsync();
                    }

                    // B) Services
                    var sourceLang = company.DefaultLanguage ?? "sk";
                    var services = await dbContext.Services.Include(s => s.Variants).Where(s => s.CompanyId == companyId).ToListAsync();

                    string ResolveSourceText(Dictionary<string, string>? dict)
                    {
                        if (dict == null) return string.Empty;
                        if (dict.TryGetValue(sourceLang, out var val) && !string.IsNullOrWhiteSpace(val)) return val;
                        if (dict.TryGetValue("sk", out var skVal) && !string.IsNullOrWhiteSpace(skVal)) return skVal;
                        if (dict.TryGetValue("hu", out var huVal) && !string.IsNullOrWhiteSpace(huVal)) return huVal;
                        return dict.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
                    }

                    foreach (var s in services)
                    {
                        var nameSrc = ResolveSourceText(s.Name);
                        if (!string.IsNullOrWhiteSpace(nameSrc))
                            s.Name![targetLanguage] = await translationService.TranslateTextAsync(nameSrc, targetLanguage, "service", companyType);
                        await UpdateProgress();

                        var catSrc = ResolveSourceText(s.Category);
                        if (!string.IsNullOrWhiteSpace(catSrc))
                            s.Category![targetLanguage] = await translationService.TranslateTextAsync(catSrc, targetLanguage, "service", companyType);
                        await UpdateProgress();

                        var descSrc = ResolveSourceText(s.Description);
                        if (!string.IsNullOrWhiteSpace(descSrc))
                            s.Description![targetLanguage] = await translationService.TranslateTextAsync(descSrc, targetLanguage, "service", companyType);
                        await UpdateProgress();

                        if (s.Variants != null)
                        {
                            foreach (var v in s.Variants)
                            {
                                var vSrc = ResolveSourceText(v.VariantName);
                                if (!string.IsNullOrWhiteSpace(vSrc))
                                {
                                    v.VariantName![targetLanguage] = await translationService.TranslateTextAsync(vSrc, targetLanguage, "service", companyType);
                                }
                            }
                        }
                    }
                    await dbContext.SaveChangesAsync();

                    // C) Gallery
                    var cats = await dbContext.GalleryCategories.Where(c => c.CompanyId == companyId).ToListAsync();
                    foreach (var c in cats)
                    {
                        var cSrc = ResolveSourceText(c.Name);
                        if (!string.IsNullOrWhiteSpace(cSrc))
                            c.Name![targetLanguage] = await translationService.TranslateTextAsync(cSrc, targetLanguage, "gallery", companyType);
                        await UpdateProgress();
                    }
                    await dbContext.SaveChangesAsync();

                    // KÉSZ
                    var finalLang = await dbContext.CompanyLanguages.FindAsync(companyId, targetLanguage);
                    if (finalLang != null)
                    {
                        finalLang.Status = TranslationStatus.ReviewPending;
                        finalLang.Progress = 100;
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Translation] Background translation error for CompanyId {CompanyId} target {TargetLanguage}: {Message}", companyId, targetLanguage, ex.Message);
                    var l = await dbContext.CompanyLanguages.FindAsync(companyId, targetLanguage);
                    if (l != null) { l.Status = TranslationStatus.Error; await dbContext.SaveChangesAsync(); }
                }
            }
        }
    }
}