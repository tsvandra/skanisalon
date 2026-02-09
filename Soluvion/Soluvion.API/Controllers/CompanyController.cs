using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using System.Security.Claims;

namespace Soluvion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;

        public CompanyController(AppDbContext context, Cloudinary cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        // GET: api/Company/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Company>> GetCompanyById(int id)
        {
            var company = await _context.Companies
                .Include(c => c.CompanyType)
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

            // Megjegyzés: A LogoUrl és FooterImageUrl itt NEM frissül, azokat a külön endpoint kezeli

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

        // --- ÚJ: KÉPFELTÖLTÉS (LOGO & FOOTER) ---

        [HttpPost("upload/logo")]
        [Authorize]
        public async Task<IActionResult> UploadLogo(IFormFile file)
        {
            return await UploadBrandingImage(file, "logo");
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
            string? oldPublicId = type == "logo" ? company.LogoPublicId : company.FooterImagePublicId;
            if (!string.IsNullOrEmpty(oldPublicId))
            {
                await _cloudinary.DestroyAsync(new DeletionParams(oldPublicId));
            }

            // 3. Mentés DB-be
            if (type == "logo")
            {
                company.LogoUrl = uploadResult.SecureUrl.ToString();
                company.LogoPublicId = uploadResult.PublicId;
            }
            else
            {
                company.FooterImageUrl = uploadResult.SecureUrl.ToString();
                company.FooterImagePublicId = uploadResult.PublicId;
            }

            await _context.SaveChangesAsync();
            return Ok(new { url = uploadResult.SecureUrl.ToString() });
        }
    }
}