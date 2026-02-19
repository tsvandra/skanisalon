using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using Soluvion.API.Models.DTOs;
using Soluvion.API.Models.Enums;
using Soluvion.API.Services;
using System.Security.Claims;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITranslationService _translationService;

        public TranslationController(AppDbContext context, IServiceScopeFactory scopeFactory, ITranslationService translationService)
        {
            _context = context;
            _scopeFactory = scopeFactory;
            _translationService = translationService;
        }

        // --- VARÁZSPÁLCA VÉGPONT ---
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> TranslateSingleText([FromBody] TranslationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Text)) return BadRequest("Nincs szöveg");

            try
            {
                var companyIdClaim = User.FindFirst("CompanyId");
                if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
                {
                    return Unauthorized("Érvénytelen token: hiányzó CompanyId.");
                }

                // BIZTONSÁGI JAVÍTÁS: Nem hivatkozunk a company.Type-ra, hogy elkerüljük a Null Reference hibát.
                string companyType = "General Business";

                string translatedText = await _translationService.TranslateTextAsync(
                    request.Text,
                    request.TargetLanguage,
                    "general",
                    companyType
                );

                return Ok(new { translatedText = translatedText });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fordítási hiba: {ex.Message}");
                return StatusCode(500, "Hiba történt a fordítás során.");
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

            if (company == null) return NotFound("Cég nem található.");

            var result = new List<LanguageStatusDto>();

            result.Add(new LanguageStatusDto
            {
                LanguageCode = company.DefaultLanguage ?? "hu",
                Status = TranslationStatus.Published.ToString(),
                IsDefault = true,
                Progress = 100
            });

            foreach (var lang in company.Languages)
            {
                if (lang.LanguageCode == company.DefaultLanguage) continue;

                result.Add(new LanguageStatusDto
                {
                    LanguageCode = lang.LanguageCode,
                    Status = lang.Status.ToString(),
                    IsDefault = false,
                    Progress = lang.Progress
                });
            }

            return Ok(result);
        }

        [HttpPost("add-language")]
        public async Task<IActionResult> AddLanguage([FromBody] AddLanguageDto dto)
        {
            var company = await _context.Companies
                .Include(c => c.Languages)
                .FirstOrDefaultAsync(c => c.Id == dto.CompanyId);

            if (company == null) return NotFound("Cég nem található.");

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
                return Accepted(new { message = $"A(z) {dto.TargetLanguage} nyelv hozzáadva. A fordítás elindult." });
            }
            else
            {
                return Ok(new { message = $"A(z) {dto.TargetLanguage} nyelv hozzáadva (üresen)." });
            }
        }

        [HttpDelete("language/{companyId}/{langCode}")]
        public async Task<IActionResult> DeleteLanguage(int companyId, string langCode)
        {
            var company = await _context.Companies.Include(c => c.Languages).FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null) return NotFound();
            if (company.DefaultLanguage == langCode) return BadRequest("Alapértelmezett nyelv nem törölhető.");

            var langEntity = company.Languages.FirstOrDefault(l => l.LanguageCode == langCode);
            if (langEntity != null) _context.CompanyLanguages.Remove(langEntity);

            var overrides = await _context.UiTranslationOverrides
                .Where(o => o.CompanyId == companyId && o.LanguageCode == langCode)
                .ToListAsync();
            _context.UiTranslationOverrides.RemoveRange(overrides);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Nyelv törölve." });
        }

        [HttpPost("publish")]
        public async Task<IActionResult> PublishLanguage([FromBody] PublishLanguageDto dto)
        {
            var langEntity = await _context.CompanyLanguages
                .FirstOrDefaultAsync(cl => cl.CompanyId == dto.CompanyId && cl.LanguageCode == dto.LanguageCode);

            if (langEntity == null) return NotFound("Nyelv nem található.");

            langEntity.Status = TranslationStatus.Published;
            langEntity.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Nyelv publikálva." });
        }

        [HttpGet("overrides/{companyId}/{langCode}")]
        public async Task<ActionResult<Dictionary<string, string>>> GetOverrides(int companyId, string langCode)
        {
            var overrides = await _context.UiTranslationOverrides
                .Where(o => o.CompanyId == companyId && o.LanguageCode == langCode)
                .ToDictionaryAsync(o => o.TranslationKey, o => o.TranslatedText);

            return Ok(overrides);
        }

        [HttpPost("save-override")]
        public async Task<IActionResult> SaveOverride([FromBody] UiTranslationOverrideDto dto)
        {
            var existing = await _context.UiTranslationOverrides
                .FirstOrDefaultAsync(o => o.CompanyId == dto.CompanyId &&
                                          o.LanguageCode == dto.LanguageCode &&
                                          o.TranslationKey == dto.Key);

            if (existing != null) existing.TranslatedText = dto.Value;
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
            return Ok(new { message = "Mentve." });
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

                    // BIZTONSÁGI JAVÍTÁS:
                    string companyType = "General Business";

                    int totalItems = (uiTranslations?.Count ?? 0)
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

                    // A) UI
                    if (uiTranslations != null)
                    {
                        foreach (var kvp in uiTranslations)
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
                    var services = await dbContext.Services.Include(s => s.Variants).Where(s => s.CompanyId == companyId).ToListAsync();
                    foreach (var s in services)
                    {
                        if (s.Name != null && s.Name.ContainsKey("hu") && !string.IsNullOrWhiteSpace(s.Name["hu"]))
                            s.Name[targetLanguage] = await translationService.TranslateTextAsync(s.Name["hu"], targetLanguage, "service", companyType);
                        await UpdateProgress();

                        if (s.Category != null && s.Category.ContainsKey("hu") && !string.IsNullOrWhiteSpace(s.Category["hu"]))
                            s.Category[targetLanguage] = await translationService.TranslateTextAsync(s.Category["hu"], targetLanguage, "service", companyType);
                        await UpdateProgress();

                        if (s.Description != null && s.Description.ContainsKey("hu") && !string.IsNullOrWhiteSpace(s.Description["hu"]))
                            s.Description[targetLanguage] = await translationService.TranslateTextAsync(s.Description["hu"], targetLanguage, "service", companyType);
                        await UpdateProgress();

                        if (s.Variants != null)
                        {
                            foreach (var v in s.Variants)
                            {
                                if (v.VariantName != null && v.VariantName.ContainsKey("hu") && !string.IsNullOrWhiteSpace(v.VariantName["hu"]))
                                {
                                    v.VariantName[targetLanguage] = await translationService.TranslateTextAsync(v.VariantName["hu"], targetLanguage, "service", companyType);
                                }
                            }
                        }
                    }
                    await dbContext.SaveChangesAsync();

                    // C) Gallery
                    var cats = await dbContext.GalleryCategories.Where(c => c.CompanyId == companyId).ToListAsync();
                    foreach (var c in cats)
                    {
                        if (c.Name != null && c.Name.ContainsKey("hu") && !string.IsNullOrWhiteSpace(c.Name["hu"]))
                            c.Name[targetLanguage] = await translationService.TranslateTextAsync(c.Name["hu"], targetLanguage, "gallery", companyType);
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
                    Console.WriteLine("Háttérfordítási hiba: " + ex.Message + " | StackTrace: " + ex.StackTrace);
                    var l = await dbContext.CompanyLanguages.FindAsync(companyId, targetLanguage);
                    if (l != null) { l.Status = TranslationStatus.Error; await dbContext.SaveChangesAsync(); }
                }
            }
        }
    }
}