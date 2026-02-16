using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.DTOs;
using Soluvion.API.Models;
using Soluvion.API.Models.DTOs; // Új DTO namespace
using Soluvion.API.Services;    // ITenantContext miatt
using System.Security.Claims;
using Soluvion.API.Models.Enums;

namespace Soluvion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly ITenantContext _tenantContext; // Új service injektálása

        public CompanyController(AppDbContext context, Cloudinary cloudinary, ITenantContext tenantContext)
        {
            _context = context;
            _cloudinary = cloudinary;
            _tenantContext = tenantContext;
        }

        // --- ÚJ ENDPOINT: PUBLIKUS CONFIG LEKÉRÉSE (VENDÉG NÉZET) ---
        // GET: api/Company/public-config
        [HttpGet("public-config")]
        [AllowAnonymous] // Bárki hívhatja bejelentkezés nélkül
        public ActionResult<CompanyPublicProfileDto> GetPublicConfig()
        {
            var company = _tenantContext.CurrentCompany;

            if (company == null)
            {
                // Ha a Middleware nem találta meg a céget (rossz domain),
                // akkor visszaadhatunk egy 404-et, vagy egy alapértelmezett "Landing Page"-et.
                return NotFound("A kért szalon nem található ezen a címen.");
            }

            // Mapping (Entity -> DTO)
            // Csak a biztonságos, publikus adatokat adjuk vissza
            var dto = new CompanyPublicProfileDto
            {
                Id = company.Id,
                Name = company.Name,

                // Design
                LogoUrl = company.LogoUrl,
                LogoHeight = company.LogoHeight,
                FooterImageUrl = company.FooterImageUrl,
                FooterHeight = company.FooterHeight,
                HeroImageUrl = company.HeroImageUrl,
                PrimaryColor = company.PrimaryColor,
                SecondaryColor = company.SecondaryColor,

                // Tartalom
                OpeningHoursTitle = company.OpeningHoursTitle,
                OpeningHoursDescription = company.OpeningHoursDescription,
                OpeningTimeSlots = company.OpeningTimeSlots,
                OpeningExtraInfo = company.OpeningExtraInfo,

                // Kapcsolat
                Phone = company.Phone,
                Email = company.Email,
                FacebookUrl = company.FacebookUrl,
                InstagramUrl = company.InstagramUrl,
                TikTokUrl = company.TikTokUrl,
                MapEmbedUrl = company.MapEmbedUrl,

                State = company.State,
                City = company.City,
                StreetName = company.StreetName,
                HouseNumber = company.HouseNumber,
                PostalCode = company.PostalCode,

                // Nyelvek
                DefaultLanguage = company.DefaultLanguage,
                // Itt kinyerjük a nyelvkódokat a CompanyLanguage listából
                SupportedLanguages = company.Languages
                    .Where(l => l.Status == TranslationStatus.Published) // Csak az aktív nyelveket adjuk vissza!
                    .Select(l => l.LanguageCode)
                    .ToList()
            };

            // Ha a listában nincs benne a default nyelv (pl. hu), tegyük bele biztonságból
            if (!dto.SupportedLanguages.Contains(dto.DefaultLanguage))
            {
                dto.SupportedLanguages.Add(dto.DefaultLanguage);
            }

            return Ok(dto);
        }
        // -------------------------------------------------------------

        // GET: api/Company/1 (Ezt csak Admin használja, vagy explicit ID kérésnél)
        [HttpGet("{id}")]
        public async Task<ActionResult<Company>> GetCompanyById(int id)
        {
            var company = await _context.Companies
                .Include(c => c.CompanyType)
                .Include(c => c.Languages) // Nyelveket is betöltjük
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null) return NotFound();

            return company;
        }

        // PUT: api/Company/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] Company updatedCompany)
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim == null) return Unauthorized("Nincs cég hozzárendelve a felhasználóhoz.");

            int userCompanyId = int.Parse(companyClaim.Value);

            if (id != userCompanyId)
            {
                return Forbid("Nincs jogosultsága más cég adatait módosítani!");
            }

            var existingCompany = await _context.Companies.FindAsync(userCompanyId);
            if (existingCompany == null) return NotFound("A cég nem található.");

            // Adatok frissítése
            existingCompany.Name = updatedCompany.Name;
            existingCompany.Email = updatedCompany.Email;
            existingCompany.Phone = updatedCompany.Phone;
            existingCompany.City = updatedCompany.City;
            existingCompany.StreetName = updatedCompany.StreetName;
            existingCompany.HouseNumber = updatedCompany.HouseNumber;
            existingCompany.PostalCode = updatedCompany.PostalCode;
            existingCompany.FacebookUrl = updatedCompany.FacebookUrl;
            existingCompany.InstagramUrl = updatedCompany.InstagramUrl;
            existingCompany.TikTokUrl = updatedCompany.TikTokUrl;
            existingCompany.MapEmbedUrl = updatedCompany.MapEmbedUrl;
            existingCompany.OpeningHoursTitle = updatedCompany.OpeningHoursTitle;
            existingCompany.OpeningHoursDescription = updatedCompany.OpeningHoursDescription;
            existingCompany.OpeningTimeSlots = updatedCompany.OpeningTimeSlots;
            existingCompany.OpeningExtraInfo = updatedCompany.OpeningExtraInfo;
            existingCompany.PrimaryColor = updatedCompany.PrimaryColor;
            existingCompany.SecondaryColor = updatedCompany.SecondaryColor;
            existingCompany.FooterHeight = updatedCompany.FooterHeight;
            existingCompany.LogoHeight = updatedCompany.LogoHeight;

            // Megjegyzés: A LogoUrl, HeroImageUrl és FooterImageUrl itt NEM frissül, azokat a külön endpoint kezeli

            await _context.SaveChangesAsync();
            return Ok(existingCompany);
        }

        // POST: api/Company
        [HttpPost]
        public async Task<ActionResult<Company>> CreateCompany(Company company)
        {
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCompanyById), new { id = company.Id }, company);
        }

        // --- ÚJ: KÉPFELTÖLTÉS (LOGO, HERO & FOOTER) ---

        [HttpPost("upload/logo")]
        [Authorize]
        public async Task<IActionResult> UploadLogo(IFormFile file)
        {
            return await UploadBrandingImage(file, "logo");
        }

        [HttpPost("upload/hero")]
        [Authorize]
        public async Task<IActionResult> UploadHero(IFormFile file)
        {
            return await UploadBrandingImage(file, "hero");
        }

        [HttpPost("upload/footer")]
        [Authorize]
        public async Task<IActionResult> UploadFooter(IFormFile file)
        {
            return await UploadBrandingImage(file, "footer");
        }

        private async Task<IActionResult> UploadBrandingImage(IFormFile file, string type)
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim == null) return Unauthorized();
            int companyId = int.Parse(companyClaim.Value);

            var company = await _context.Companies.FindAsync(companyId);
            if (company == null) return NotFound();

            if (file == null || file.Length == 0) return BadRequest("Nincs fájl kiválasztva.");

            // 1. Feltöltés
            var uploadResult = new ImageUploadResult();
            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"soluvion/company_{companyId}/branding"
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            if (uploadResult.Error != null) return BadRequest(uploadResult.Error.Message);

            // 2. Régi törlése
            string? oldPublicId = null;
            switch (type)
            {
                case "logo": oldPublicId = company.LogoPublicId; break;
                case "hero": oldPublicId = company.HeroImagePublicId; break;
                case "footer": oldPublicId = company.FooterImagePublicId; break;
            }

            if (!string.IsNullOrEmpty(oldPublicId))
            {
                await _cloudinary.DestroyAsync(new DeletionParams(oldPublicId));
            }

            // 3. Mentés DB-be
            switch (type)
            {
                case "logo":
                    company.LogoUrl = uploadResult.SecureUrl.ToString();
                    company.LogoPublicId = uploadResult.PublicId;
                    break;
                case "hero":
                    company.HeroImageUrl = uploadResult.SecureUrl.ToString();
                    company.HeroImagePublicId = uploadResult.PublicId;
                    break;
                case "footer":
                    company.FooterImageUrl = uploadResult.SecureUrl.ToString();
                    company.FooterImagePublicId = uploadResult.PublicId;
                    break;
            }

            await _context.SaveChangesAsync();
            return Ok(new { url = uploadResult.SecureUrl.ToString() });
        }
    }
}