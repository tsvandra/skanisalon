using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.DTOs;
using Soluvion.API.Models;
using Soluvion.API.Models.DTOs; // Itt van a CompanySettingsDto
using Soluvion.API.Models.Enums;
using Soluvion.API.Services;

namespace Soluvion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly ITenantContext _tenantContext;

        public CompanyController(AppDbContext context, Cloudinary cloudinary, ITenantContext tenantContext)
        {
            _context = context;
            _cloudinary = cloudinary;
            _tenantContext = tenantContext;
        }

        // GET: api/Company/public-config (VENDÉG NÉZET)
        [HttpGet("public-config")]
        [AllowAnonymous]
        public ActionResult<CompanyPublicProfileDto> GetPublicConfig()
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null) return NotFound("A kért szalon nem található.");

            var dto = new CompanyPublicProfileDto
            {
                Id = company.Id,
                Name = company.Name,
                LogoUrl = company.LogoUrl,
                LogoHeight = company.LogoHeight,
                FooterImageUrl = company.FooterImageUrl,
                FooterHeight = company.FooterHeight,
                HeroImageUrl = company.HeroImageUrl,
                PrimaryColor = company.PrimaryColor,
                SecondaryColor = company.SecondaryColor,
                OpeningHoursTitle = company.OpeningHoursTitle,
                OpeningHoursDescription = company.OpeningHoursDescription,
                OpeningTimeSlots = company.OpeningTimeSlots,
                OpeningExtraInfo = company.OpeningExtraInfo,
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
                DefaultLanguage = company.DefaultLanguage,
                SupportedLanguages = company.Languages
                    .Where(l => l.Status == TranslationStatus.Published)
                    .Select(l => l.LanguageCode)
                    .ToList()
            };

            if (!dto.SupportedLanguages.Contains(dto.DefaultLanguage))
            {
                dto.SupportedLanguages.Add(dto.DefaultLanguage);
            }

            return Ok(dto);
        }

        // --- ADMIN BEÁLLÍTÁSOK (JAVÍTOTT DTO-VAL) ---

        // GET: api/Company/{id}
        [HttpGet("{id}")]
        [Authorize] // Csak bejelentkezve!
        public async Task<ActionResult<CompanySettingsDto>> GetCompanySettings(int id)
        {
            // Jogosultság ellenőrzés
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim == null || int.Parse(companyClaim.Value) != id)
                return Forbid();

            var c = await _context.Companies.FindAsync(id);
            if (c == null) return NotFound();

            // Kézi mapping DTO-ba (így elkerüljük a körkörös hivatkozást)
            var dto = new CompanySettingsDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                City = c.City,
                StreetName = c.StreetName,
                HouseNumber = c.HouseNumber,
                PostalCode = c.PostalCode,
                State = c.State,
                FacebookUrl = c.FacebookUrl,
                InstagramUrl = c.InstagramUrl,
                TikTokUrl = c.TikTokUrl,
                MapEmbedUrl = c.MapEmbedUrl,
                OpeningHoursTitle = c.OpeningHoursTitle,
                OpeningHoursDescription = c.OpeningHoursDescription,
                OpeningTimeSlots = c.OpeningTimeSlots,
                OpeningExtraInfo = c.OpeningExtraInfo,
                PrimaryColor = c.PrimaryColor,
                SecondaryColor = c.SecondaryColor,
                FooterHeight = c.FooterHeight,
                LogoHeight = c.LogoHeight,
                LogoUrl = c.LogoUrl,
                HeroImageUrl = c.HeroImageUrl,
                FooterImageUrl = c.FooterImageUrl
            };

            return Ok(dto);
        }

        // PUT: api/Company/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] CompanySettingsDto dto)
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim == null) return Unauthorized();
            if (id != int.Parse(companyClaim.Value)) return Forbid();

            var existing = await _context.Companies.FindAsync(id);
            if (existing == null) return NotFound();

            // Adatok átmásolása DTO-ból Entity-be
            existing.Name = dto.Name;
            existing.Email = dto.Email;
            existing.Phone = dto.Phone;
            existing.City = dto.City;
            existing.StreetName = dto.StreetName;
            existing.HouseNumber = dto.HouseNumber;
            existing.PostalCode = dto.PostalCode;
            existing.State = dto.State; // Ha van ilyen mező a DTO-ban

            existing.FacebookUrl = dto.FacebookUrl;
            existing.InstagramUrl = dto.InstagramUrl;
            existing.TikTokUrl = dto.TikTokUrl;
            existing.MapEmbedUrl = dto.MapEmbedUrl;

            existing.OpeningHoursTitle = dto.OpeningHoursTitle;
            existing.OpeningHoursDescription = dto.OpeningHoursDescription;
            existing.OpeningTimeSlots = dto.OpeningTimeSlots;
            existing.OpeningExtraInfo = dto.OpeningExtraInfo;

            existing.PrimaryColor = dto.PrimaryColor;
            existing.SecondaryColor = dto.SecondaryColor;
            existing.FooterHeight = dto.FooterHeight;
            existing.LogoHeight = dto.LogoHeight;

            await _context.SaveChangesAsync();
            return Ok(dto); // Visszaküldjük a frissített adatokat
        }

        // --- KÉPFELTÖLTÉS (Változatlan) ---
        [HttpPost("upload/logo")]
        [Authorize]
        public async Task<IActionResult> UploadLogo(IFormFile file) => await UploadBrandingImage(file, "logo");

        [HttpPost("upload/hero")]
        [Authorize]
        public async Task<IActionResult> UploadHero(IFormFile file) => await UploadBrandingImage(file, "hero");

        [HttpPost("upload/footer")]
        [Authorize]
        public async Task<IActionResult> UploadFooter(IFormFile file) => await UploadBrandingImage(file, "footer");

        private async Task<IActionResult> UploadBrandingImage(IFormFile file, string type)
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim == null) return Unauthorized();
            int companyId = int.Parse(companyClaim.Value);

            var company = await _context.Companies.FindAsync(companyId);
            if (company == null) return NotFound();

            if (file == null || file.Length == 0) return BadRequest("Nincs fájl.");

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

            // Régi törlése
            string? oldPublicId = type switch
            {
                "logo" => company.LogoPublicId,
                "hero" => company.HeroImagePublicId,
                "footer" => company.FooterImagePublicId,
                _ => null
            };

            if (!string.IsNullOrEmpty(oldPublicId))
            {
                await _cloudinary.DestroyAsync(new DeletionParams(oldPublicId));
            }

            // Mentés
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