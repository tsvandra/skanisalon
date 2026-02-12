using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Services;
using System.Security.Claims;

namespace Soluvion.API.Controllers
{
    public class TranslationRequest
    {
        public string Text { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = "en";
        public string Context { get; set; } = "general";
    }

    [Route("api/[controller]")]
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly ITranslationService _translationService;
        private readonly AppDbContext _context; // Adatbázis elérés injektálása

        public TranslationController(ITranslationService translationService, AppDbContext context)
        {
            _translationService = translationService;
            _context = context;
        }

        // Segédfüggvény a CompanyId kinyerésére a tokenből
        private int GetCurrentCompanyId()
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null && int.TryParse(companyClaim.Value, out int companyId))
            {
                return companyId;
            }
            return 0;
        }

        [HttpPost]
        public async Task<IActionResult> Translate([FromBody] TranslationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Nincs szöveg, amit lefordíthatnék.");
            }

            // 1. Lekérjük a cég típusát az adatbázisból
            string companyTypeName = "Beauty Salon"; // Alapértelmezett fallback
            int companyId = GetCurrentCompanyId();

            if (companyId > 0)
            {
                var company = await _context.Companies
                    .Include(c => c.CompanyType) // Betöltjük a kapcsolódó típust
                    .FirstOrDefaultAsync(c => c.Id == companyId);

                if (company != null && company.CompanyType != null)
                {
                    companyTypeName = company.CompanyType.Name;
                }
            }

            // 2. Átadjuk a típust is a service-nek
            var translatedText = await _translationService.TranslateTextAsync(
                request.Text,
                request.TargetLanguage,
                request.Context,
                companyTypeName // <--- Itt adjuk át a dinamikus típust
            );

            return Ok(new { translatedText });
        }
    }
}