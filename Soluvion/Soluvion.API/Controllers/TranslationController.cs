using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using Soluvion.API.Models.DTOs;
using Soluvion.API.Models.Enums;
using Soluvion.API.Services;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory; // Ez kell a háttérszálhoz!

        public TranslationController(AppDbContext context, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _scopeFactory = scopeFactory;
        }

        // 1. Státuszok lekérdezése (Admin felülethez)
        [HttpGet("languages/{companyId}")]
        public async Task<ActionResult<IEnumerable<LanguageStatusDto>>> GetCompanyLanguages(int companyId)
        {
            var company = await _context.Companies
                .Include(c => c.Languages)
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null) return NotFound("Cég nem található.");

            var result = new List<LanguageStatusDto>();

            // Default nyelv hozzáadása (ez mindig "Published"-nek számít logikailag)
            result.Add(new LanguageStatusDto
            {
                LanguageCode = company.DefaultLanguage,
                Status = TranslationStatus.Published.ToString(),
                IsDefault = true
            });

            // Többi nyelv hozzáadása
            foreach (var lang in company.Languages)
            {
                result.Add(new LanguageStatusDto
                {
                    LanguageCode = lang.LanguageCode,
                    Status = lang.Status.ToString(),
                    IsDefault = false
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

            if (company.Languages.Any(l => l.LanguageCode == dto.TargetLanguage) || company.DefaultLanguage == dto.TargetLanguage)
            {
                // Ha már létezik, akkor is engedjük újra lefutni a fordítást (javítás/újragenerálás céljából),
                // de a státuszt visszaállítjuk Translating-re.
                var existing = company.Languages.FirstOrDefault(l => l.LanguageCode == dto.TargetLanguage);
                if (existing != null)
                {
                    existing.Status = TranslationStatus.Translating;
                }
            }
            else
            {
                var newLang = new CompanyLanguage
                {
                    CompanyId = dto.CompanyId,
                    LanguageCode = dto.TargetLanguage,
                    Status = TranslationStatus.Translating,
                    LastUpdated = DateTime.UtcNow
                };
                _context.CompanyLanguages.Add(newLang);
            }

            await _context.SaveChangesAsync();

            // Háttérfolyamat indítása - átadjuk a UI szótárat is!
            _ = Task.Run(() => PerformBackgroundTranslation(dto.CompanyId, dto.TargetLanguage, dto.BaseUiTranslations));

            return Accepted(new { message = $"A(z) {dto.TargetLanguage} nyelv hozzáadva. A fordítás a háttérben elindult, nyugodtan elhagyhatod az oldalt." });
        }

        // 3. Publikálás (Review után)
        [HttpPost("publish")]
        public async Task<IActionResult> PublishLanguage([FromBody] PublishLanguageDto dto)
        {
            var langEntity = await _context.CompanyLanguages
                .FirstOrDefaultAsync(cl => cl.CompanyId == dto.CompanyId && cl.LanguageCode == dto.LanguageCode);

            if (langEntity == null) return NotFound("Nyelv nem található.");

            langEntity.Status = TranslationStatus.Published;
            langEntity.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Nyelv sikeresen publikálva." });
        }

        // 4. Felülírások lekérése egy adott nyelvhez
        [HttpGet("overrides/{companyId}/{langCode}")]
        public async Task<ActionResult<Dictionary<string, string>>> GetOverrides(int companyId, string langCode)
        {
            var overrides = await _context.UiTranslationOverrides
                .Where(o => o.CompanyId == companyId && o.LanguageCode == langCode)
                .ToDictionaryAsync(o => o.TranslationKey, o => o.TranslatedText);

            return Ok(overrides);
        }

        // 5. Felülírás mentése (Upsert)
        [HttpPost("save-override")]
        public async Task<IActionResult> SaveOverride([FromBody] OverrideDto dto)
        {
            var existing = await _context.UiTranslationOverrides
                .FirstOrDefaultAsync(o => o.CompanyId == dto.CompanyId &&
                                          o.LanguageCode == dto.LanguageCode &&
                                          o.TranslationKey == dto.Key);

            if (existing != null)
            {
                // Frissítés
                existing.TranslatedText = dto.Value;
            }
            else
            {
                // Új létrehozása
                var newOverride = new UiTranslationOverride
                {
                    CompanyId = dto.CompanyId,
                    LanguageCode = dto.LanguageCode,
                    TranslationKey = dto.Key,
                    TranslatedText = dto.Value
                };
                _context.UiTranslationOverrides.Add(newOverride);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Szöveg sikeresen mentve." });
        }


        // --- PRIVATE BACKGROUND WORKER ---
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

                    string sourceLang = company.DefaultLanguage;
                    string companyTypeContext = company.CompanyType?.Name ?? "Beauty Salon";

                    // --- A) UI STATIKUS SZÖVEGEK FORDÍTÁSA (ÚJ RÉSZ) ---
                    if (uiTranslations != null && uiTranslations.Count > 0)
                    {
                        foreach (var kvp in uiTranslations)
                        {
                            string key = kvp.Key;
                            string originalText = kvp.Value;

                            // Csak akkor fordítunk, ha van értelmes szöveg
                            if (!string.IsNullOrWhiteSpace(originalText))
                            {
                                // AI fordítás (Rövid UI szöveg kontextussal)
                                var translatedText = await translationService.TranslateTextAsync(originalText, targetLanguage, "user interface button or label", companyTypeContext);

                                // Mentés az Overrides táblába
                                var overrideEntity = await dbContext.UiTranslationOverrides
                                    .FirstOrDefaultAsync(o => o.CompanyId == companyId && o.LanguageCode == targetLanguage && o.TranslationKey == key);

                                if (overrideEntity == null)
                                {
                                    dbContext.UiTranslationOverrides.Add(new UiTranslationOverride
                                    {
                                        CompanyId = companyId,
                                        LanguageCode = targetLanguage,
                                        TranslationKey = key,
                                        TranslatedText = translatedText
                                    });
                                }
                                else
                                {
                                    overrideEntity.TranslatedText = translatedText;
                                }
                            }
                        }
                        // Szakaszos mentés, hogy ne vesszen el minden hiba esetén
                        await dbContext.SaveChangesAsync();
                    }

                    // --- B) SZOLGÁLTATÁSOK FORDÍTÁSA ---
                    var services = await dbContext.Services.Where(s => s.CompanyId == companyId).ToListAsync();
                    foreach (var service in services)
                    {
                        if (service.Name.TryGetValue(sourceLang, out var nameSource))
                        {
                            var translatedName = await translationService.TranslateTextAsync(nameSource, targetLanguage, "service name", companyTypeContext);
                            service.Name[targetLanguage] = translatedName;
                        }
                        if (service.Description.TryGetValue(sourceLang, out var descSource))
                        {
                            var translatedDesc = await translationService.TranslateTextAsync(descSource, targetLanguage, "service description", companyTypeContext);
                            service.Description[targetLanguage] = translatedDesc;
                        }
                        if (service.Category.TryGetValue(sourceLang, out var catSource))
                        {
                            var translatedCat = await translationService.TranslateTextAsync(catSource, targetLanguage, "service category", companyTypeContext);
                            service.Category[targetLanguage] = translatedCat;
                        }
                    }
                    await dbContext.SaveChangesAsync();

                    // --- C) GALÉRIA KATEGÓRIÁK FORDÍTÁSA ---
                    var galleryCats = await dbContext.GalleryCategories.Where(g => g.CompanyId == companyId).ToListAsync();
                    foreach (var cat in galleryCats)
                    {
                        if (cat.Name.TryGetValue(sourceLang, out var catNameSource))
                        {
                            var translatedCatName = await translationService.TranslateTextAsync(catNameSource, targetLanguage, "gallery category", companyTypeContext);
                            cat.Name[targetLanguage] = translatedCatName;
                        }
                    }
                    await dbContext.SaveChangesAsync();

                    // --- VÉGE: STÁTUSZ FRISSÍTÉS ---
                    var langEntity = await dbContext.CompanyLanguages
                        .FirstOrDefaultAsync(cl => cl.CompanyId == companyId && cl.LanguageCode == targetLanguage);

                    if (langEntity != null)
                    {
                        langEntity.Status = TranslationStatus.ReviewPending;
                    }

                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Translation Error: {ex.Message}");
                    // Hiba esetén Error státusz
                    var langEntity = await dbContext.CompanyLanguages
                        .FirstOrDefaultAsync(cl => cl.CompanyId == companyId && cl.LanguageCode == targetLanguage);

                    if (langEntity != null)
                    {
                        langEntity.Status = TranslationStatus.Error;
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }
}