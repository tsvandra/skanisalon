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

        // --- VARÁZSPÁLCA VÉGPONT (ÉLES OpenAI HÍVÁSSAL) ---
        [HttpPost]
        [Authorize] // Kell a token, hogy tudjuk, melyik cégnek fordítunk
        public async Task<IActionResult> TranslateSingleText([FromBody] TranslationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Text)) return BadRequest("Nincs szöveg");

            try
            {
                // 1. Cég azonosítása a tokenből
                var companyIdClaim = User.FindFirst("CompanyId");
                if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
                {
                    return Unauthorized("Érvénytelen token: hiányzó CompanyId.");
                }

                // 2. Cégtípus lekérése a pontosabb fordításhoz
                var company = await _context.Companies.FindAsync(companyId);
                string companyType = company?.CompanyType.ToString() ?? "General Business";

                // 3. Fordítás hívása (Kontextus egyelőre 'general', amíg a Frontend nem küld pontosabbat)
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
                // Ha az OpenAI hívás elszáll (pl. elfogyott a kredit), ne omoljon össze a kliens
                Console.WriteLine($"Fordítási hiba: {ex.Message}");
                return StatusCode(500, "Hiba történt a fordítás során.");
            }
        }
        // ---------------------------------------------------------------

        // 1. Státuszok lekérdezése
        [HttpGet("languages/{companyId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<LanguageStatusDto>>> GetCompanyLanguages(int companyId)
        {
            var company = await _context.Companies
                .Include(c => c.Languages)
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null) return NotFound("Cég nem található.");

            var result = new List<LanguageStatusDto>();

            // Default nyelv hozzáadása
            result.Add(new LanguageStatusDto
            {
                LanguageCode = company.DefaultLanguage ?? "hu",
                Status = TranslationStatus.Published.ToString(),
                IsDefault = true,
                Progress = 100
            });

            // Többi nyelv hozzáadása
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

        // 2. Új nyelv hozzáadása + AI folyamat indítása
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

        // Nyelv törlése
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

        // 3. Publikálás
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

        // 4. Felülírások lekérése
        [HttpGet("overrides/{companyId}/{langCode}")]
        public async Task<ActionResult<Dictionary<string, string>>> GetOverrides(int companyId, string langCode)
        {
            var overrides = await _context.UiTranslationOverrides
                .Where(o => o.CompanyId == companyId && o.LanguageCode == langCode)
                .ToDictionaryAsync(o => o.TranslationKey, o => o.TranslatedText);

            return Ok(overrides);
        }

        // 5. Felülírás mentése
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

        // --- HÁTTÉRFOLYAMAT (VALÓDI AI SERVICE-SZEL) ---
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

                    // Cégtípus a pontosabb promptoláshoz
                    string companyType = company.CompanyType.ToString();

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

                    // A) UI Fordítás ("ui" kontextus)
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

                    // B) Services Fordítás ("service" kontextus)
                    var services = await dbContext.Services.Include(s => s.Variants).Where(s => s.CompanyId == companyId).ToListAsync();
                    foreach (var s in services)
                    {
                        if (s.Name.ContainsKey("hu")) s.Name[targetLanguage] = await translationService.TranslateTextAsync(s.Name["hu"], targetLanguage, "service", companyType);
                        await UpdateProgress();

                        if (s.Category.ContainsKey("hu")) s.Category[targetLanguage] = await translationService.TranslateTextAsync(s.Category["hu"], targetLanguage, "service", companyType);
                        await UpdateProgress();

                        if (s.Description.ContainsKey("hu")) s.Description[targetLanguage] = await translationService.TranslateTextAsync(s.Description["hu"], targetLanguage, "service", companyType);
                        await UpdateProgress();

                        // Variánsok fordítása
                        if (s.Variants != null)
                        {
                            foreach (var v in s.Variants)
                            {
                                if (v.VariantName.ContainsKey("hu"))
                                {
                                    v.VariantName[targetLanguage] = await translationService.TranslateTextAsync(v.VariantName["hu"], targetLanguage, "service", companyType);
                                }
                            }
                        }
                    }
                    await dbContext.SaveChangesAsync();

                    // C) Gallery Fordítás ("gallery" kontextus)
                    var cats = await dbContext.GalleryCategories.Where(c => c.CompanyId == companyId).ToListAsync();
                    foreach (var c in cats)
                    {
                        if (c.Name.ContainsKey("hu")) c.Name[targetLanguage] = await translationService.TranslateTextAsync(c.Name["hu"], targetLanguage, "gallery", companyType);
                        await UpdateProgress();
                    }
                    // Megj: A képek címeit (GalleryImage) is lehetne itt fordítani, ha szükséges, de egyelőre a kategóriák vannak fókuszban.

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
                    Console.WriteLine("Háttérfordítási hiba: " + ex.Message);
                    var l = await dbContext.CompanyLanguages.FindAsync(companyId, targetLanguage);
                    if (l != null) { l.Status = TranslationStatus.Error; await dbContext.SaveChangesAsync(); }
                }
            }
        }
    }
}