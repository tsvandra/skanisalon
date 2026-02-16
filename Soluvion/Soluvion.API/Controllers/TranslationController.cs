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

            // Ha nem kér AI-t, akkor a státusz rögtön "ReviewPending" (hogy szerkeszthesse),
            // vagy "Created". Legyen ReviewPending, hogy a banner figyelmeztesse: "Hé, ez még üres!"
            var initialStatus = dto.UseAi ? TranslationStatus.Translating : TranslationStatus.ReviewPending;

            if (company.Languages.Any(l => l.LanguageCode == dto.TargetLanguage) || company.DefaultLanguage == dto.TargetLanguage)
            {
                var existing = company.Languages.FirstOrDefault(l => l.LanguageCode == dto.TargetLanguage);
                if (existing != null)
                {
                    // Ha újraindítjuk, frissítjük a státuszt
                    existing.Status = initialStatus;
                }
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

            // CSAK AKKOR indítjuk a jobot, ha kérte az AI-t!
            if (dto.UseAi)
            {
                _ = Task.Run(() => PerformBackgroundTranslation(dto.CompanyId, dto.TargetLanguage, dto.BaseUiTranslations));
                return Accepted(new { message = $"A(z) {dto.TargetLanguage} nyelv hozzáadva. A fordítás elindult." });
            }
            else
            {
                return Ok(new { message = $"A(z) {dto.TargetLanguage} nyelv hozzáadva (üresen). Kezdheted a kézi fordítást." });
            }
        }

        // Nyelv törlése
        [HttpDelete("language/{companyId}/{langCode}")]
        public async Task<IActionResult> DeleteLanguage(int companyId, string langCode)
        {
            var company = await _context.Companies
                .Include(c => c.Languages)
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null) return NotFound("Cég nem található.");
            if (company.DefaultLanguage == langCode) return BadRequest("Az alapértelmezett nyelv nem törölhető.");

            // 1. Töröljük a kapcsolatot
            var langEntity = company.Languages.FirstOrDefault(l => l.LanguageCode == langCode);
            if (langEntity != null)
            {
                _context.CompanyLanguages.Remove(langEntity);
            }

            // 2. Töröljük a UI Overrides bejegyzéseket is ehhez a nyelvhez
            var overrides = await _context.UiTranslationOverrides
                .Where(o => o.CompanyId == companyId && o.LanguageCode == langCode)
                .ToListAsync();

            _context.UiTranslationOverrides.RemoveRange(overrides);

            // Megjegyzés: A JSONB mezőkből (Service.Name['sk']) NEM töröljük ki a kulcsokat,
            // mert az nagyon erőforrásigényes lenne, és nem zavar senkit, ha ott marad "szemétként".
            // Ha újra hozzáadják a nyelvet, felülíródik.

            await _context.SaveChangesAsync();
            return Ok(new { message = "Nyelv sikeresen törölve." });
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

                    // 1. Összeszámoljuk, mennyi munka van (Total Items)
                    // UI elemek + Szolgáltatás mezők (3 per szolgáltatás) + Galéria kategóriák
                    int totalItems = (uiTranslations?.Count ?? 0)
                                     + (await dbContext.Services.CountAsync(s => s.CompanyId == companyId) * 3)
                                     + (await dbContext.GalleryCategories.CountAsync(g => g.CompanyId == companyId));

                    if (totalItems == 0) totalItems = 1; // 0 osztás elkerülése
                    int currentItem = 0;

                    // Helper a frissítéshez
                    async Task UpdateProgress()
                    {
                        currentItem++;
                        // Csak minden 5. elemnél vagy a végén írjunk DB-t a teljesítmény miatt
                        if (currentItem % 5 == 0 || currentItem == totalItems)
                        {
                            var langToUpdate = await dbContext.CompanyLanguages.FindAsync(companyId, targetLanguage);
                            if (langToUpdate != null)
                            {
                                langToUpdate.Progress = (int)((double)currentItem / totalItems * 100);
                                // Biztosítjuk, hogy 100% alatt maradjon amíg nem végzünk teljesen
                                if (langToUpdate.Progress > 99) langToUpdate.Progress = 99;
                                await dbContext.SaveChangesAsync();
                            }
                        }
                    }

                    string sourceLang = company.DefaultLanguage;
                    string companyTypeContext = company.CompanyType?.Name ?? "Beauty Salon";

                    // --- A) UI STATIKUS SZÖVEGEK ---
                    if (uiTranslations != null)
                    {
                        foreach (var kvp in uiTranslations)
                        {
                            if (!string.IsNullOrWhiteSpace(kvp.Value))
                            {
                                var translatedText = await translationService.TranslateTextAsync(kvp.Value, targetLanguage, "UI button/label", companyTypeContext);

                                var overrideEntity = await dbContext.UiTranslationOverrides
                                    .FirstOrDefaultAsync(o => o.CompanyId == companyId && o.LanguageCode == targetLanguage && o.TranslationKey == kvp.Key);

                                if (overrideEntity == null)
                                {
                                    dbContext.UiTranslationOverrides.Add(new UiTranslationOverride
                                    {
                                        CompanyId = companyId,
                                        LanguageCode = targetLanguage,
                                        TranslationKey = kvp.Key,
                                        TranslatedText = translatedText
                                    });
                                }
                                else { overrideEntity.TranslatedText = translatedText; }
                            }
                            await UpdateProgress();
                        }
                        await dbContext.SaveChangesAsync(); // Szakaszos mentés
                    }

                    // --- B) SZOLGÁLTATÁSOK ---
                    var services = await dbContext.Services.Where(s => s.CompanyId == companyId).ToListAsync();
                    foreach (var service in services)
                    {
                        if (service.Name.TryGetValue(sourceLang, out var n))
                            service.Name[targetLanguage] = await translationService.TranslateTextAsync(n, targetLanguage, "service name", companyTypeContext);
                        await UpdateProgress();

                        if (service.Description.TryGetValue(sourceLang, out var d))
                            service.Description[targetLanguage] = await translationService.TranslateTextAsync(d, targetLanguage, "service description", companyTypeContext);
                        await UpdateProgress();

                        if (service.Category.TryGetValue(sourceLang, out var c))
                            service.Category[targetLanguage] = await translationService.TranslateTextAsync(c, targetLanguage, "service category", companyTypeContext);
                        await UpdateProgress();
                    }
                    await dbContext.SaveChangesAsync();

                    // --- C) GALÉRIA ---
                    var galleryCats = await dbContext.GalleryCategories.Where(g => g.CompanyId == companyId).ToListAsync();
                    foreach (var cat in galleryCats)
                    {
                        if (cat.Name.TryGetValue(sourceLang, out var cn))
                            cat.Name[targetLanguage] = await translationService.TranslateTextAsync(cn, targetLanguage, "gallery category", companyTypeContext);
                        await UpdateProgress();
                    }
                    await dbContext.SaveChangesAsync();

                    // --- KÉSZ ---
                    var finalLang = await dbContext.CompanyLanguages.FindAsync(companyId, targetLanguage);
                    if (finalLang != null)
                    {
                        finalLang.Status = TranslationStatus.ReviewPending;
                        finalLang.Progress = 100; // Kész
                    }
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Translation Error: {ex.Message}");
                    var langEntity = await dbContext.CompanyLanguages.FindAsync(companyId, targetLanguage);
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