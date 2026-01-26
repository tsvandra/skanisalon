using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using System.Security.Claims; // Ez kell a User adatok olvasásához

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GalleryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public GalleryController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/Gallery
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetImages()
        {
            // Itt "lefordítjuk" az adatbázis szerkezetét a Frontend nyelvére
            // Hogy a Frontend továbbra is 'imageUrl'-t és 'category' szöveget kapjon
            var images = await _context.GalleryImages
                .Include(i => i.Category) // Betöltjük a kapcsolódó kategóriát
                .Select(i => new
                {
                    id = i.Id,
                    imageUrl = i.ImagePath, // Adatbázisban ImagePath, Frontendnek imageUrl
                    category = i.Category != null ? i.Category.Name : "Egyéb", // Kategória név kinyerése
                    title = i.Title
                })
                .ToListAsync();

            return Ok(images);
        }

        // POST: api/Gallery
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<object>> UploadImage(IFormFile file, [FromForm] string category)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Nem választottál ki képet!");

            // 1. Kép mentése fájlrendszerbe
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // URL generálás
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var fullImageUrl = $"{baseUrl}/images/{uniqueFileName}";

            // 2. Kategória kezelése (A Hiba Megoldása)
            // Kinyerjük a Tokenből, hogy melyik Céghez (CompanyId) tartozik a felhasználó
            // Ha valamiért nincs benne, alapértelmezetten 1-esnek vesszük (biztonsági háló)
            int companyId = 1;
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null) int.TryParse(companyClaim.Value, out companyId);

            string categoryName = category ?? "Egyéb";

            // Megkeressük, létezik-e már ez a kategória ennél a cégnél
            var galleryCategory = await _context.GalleryCategories
                .FirstOrDefaultAsync(c => c.Name == categoryName && c.CompanyId == companyId);

            // Ha nem létezik, létrehozzuk
            if (galleryCategory == null)
            {
                galleryCategory = new GalleryCategory
                {
                    Name = categoryName,
                    CompanyId = companyId
                };
                _context.GalleryCategories.Add(galleryCategory);
                await _context.SaveChangesAsync(); // Mentés, hogy kapjon ID-t
            }

            // 3. Kép mentése adatbázisba a megszerzett Kategória ID-val
            var galleryImage = new GalleryImage
            {
                ImagePath = fullImageUrl,
                CategoryId = galleryCategory.Id, // Itt kötjük össze!
                Title = file.FileName,
                UploadDate = DateTime.UtcNow
            };

            _context.GalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync();

            // Visszaküldjük a Frontend által várt formátumban
            return Ok(new
            {
                id = galleryImage.Id,
                imageUrl = galleryImage.ImagePath,
                category = galleryCategory.Name
            });
        }

        // DELETE: api/Gallery/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var image = await _context.GalleryImages.FindAsync(id);
            if (image == null) return NotFound();

            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}