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

        public CompanyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Company/1
        // Ezt bárki hívhatja (hogy betöltsön az oldal)
        [HttpGet("{id}")]
        public async Task<ActionResult<Company>> GetCompanyById(int id)
        {
            var company = await _context.Companies
                .Include(c => c.CompanyType)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null) return NotFound();

            return company;
        }

        // PUT: api/Company
        // MÓDOSÍTÁS - CSAK BEJELENTKEZVE!
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateCompany(Company updatedCompany)
        {
            // 1. Biztonság: Kinek a nevében vagyunk itt?
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim == null) return Unauthorized("Nincs cég hozzárendelve a felhasználóhoz.");

            int userCompanyId = int.Parse(companyClaim.Value);

            // 2. Megkeressük a céget az adatbázisban
            var existingCompany = await _context.Companies.FindAsync(userCompanyId);
            if (existingCompany == null) return NotFound("A cég nem található.");

            // 3. Frissítjük a mezőket
            // (Nem írjuk felül az ID-t, és egyéb technikai mezőket)

            // Alapadatok
            existingCompany.Name = updatedCompany.Name;
            existingCompany.Email = updatedCompany.Email;
            existingCompany.Phone = updatedCompany.Phone;

            // Cím
            existingCompany.City = updatedCompany.City;
            existingCompany.StreetName = updatedCompany.StreetName;
            existingCompany.HouseNumber = updatedCompany.HouseNumber;
            existingCompany.PostalCode = updatedCompany.PostalCode;

            // Social & Web
            existingCompany.FacebookUrl = updatedCompany.FacebookUrl;
            existingCompany.InstagramUrl = updatedCompany.InstagramUrl;
            existingCompany.TikTokUrl = updatedCompany.TikTokUrl;
            existingCompany.MapEmbedUrl = updatedCompany.MapEmbedUrl;

            // Nyitvatartás Konfiguráció
            existingCompany.OpeningHoursTitle = updatedCompany.OpeningHoursTitle;
            existingCompany.OpeningHoursDescription = updatedCompany.OpeningHoursDescription;
            existingCompany.OpeningTimeSlots = updatedCompany.OpeningTimeSlots;
            existingCompany.OpeningExtraInfo = updatedCompany.OpeningExtraInfo;

            // Design (Ha engedjük nekik)
            existingCompany.PrimaryColor = updatedCompany.PrimaryColor;
            existingCompany.SecondaryColor = updatedCompany.SecondaryColor;
            // A LogoUrl-t külön fájlfeltöltővel kéne kezelni, azt most hagyjuk békén

            // 4. Mentés
            await _context.SaveChangesAsync();

            return Ok(existingCompany);
        }

        // POST: api/Company (Csak belső használatra vagy SuperAdminnak)
        [HttpPost]
        public async Task<ActionResult<Company>> CreateCompany(Company company)
        {
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCompanyById), new { id = company.Id }, company);
        }
    }
}