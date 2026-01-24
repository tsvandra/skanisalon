using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GalleryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment; // Ez segít megtalálni a mappákat a szerveren

        public GalleryController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // --- KATEGÓRIÁK (MAPPÁK) KEZELÉSE ---

        // GET: api/Gallery/categories?companyId=1
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<GalleryCategory>>> GetCategories(int companyId)
        {
            return await _context.GalleryCategories
                .Where(c => c.CompanyId == companyId)
                .ToListAsync();
        }

        // POST: api/Gallery/categories
        // Új mappa létrehozása
        [HttpPost("categories")]
        public async Task<ActionResult<GalleryCategory>> CreateCategory(GalleryCategory category)
        {
            _context.GalleryCategories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        // --- KÉPEK KEZELÉSE ---

        // GET: api/Gallery/images?categoryId=5
        // Listázza a képeket egy adott mappából
        [HttpGet("images")]
        public async Task<ActionResult<IEnumerable<GalleryImage>>> GetImages(int categoryId)
        {
            return await _context.GalleryImages
                .Where(i => i.CategoryId == categoryId)
                .OrderByDescending(i => i.UploadDate) // A legfrissebbek elől
                .ToListAsync();
        }

        // POST: api/Gallery/upload
        // KÉP FELTÖLTÉSE (Ez a legfontosabb rész!)
        [HttpPost("upload")]
        public async Task<ActionResult<GalleryImage>> UploadImage([FromForm]int categoryId, [FromForm] string title, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Nincs kiválasztva fájl.");

            // 1. Ellenőrizzük, létezik-e a kategória
            var category = await _context.GalleryCategories.FindAsync(categoryId);
            if (category == null)
                return NotFound("A kategória nem létezik.");

            // 2. Fájl elmentése a szerverre (wwwroot/uploads mappába)
            // Létrehozzuk a mappát, ha még nincs
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Egyedi nevet generálunk, hogy ne írják felül egymást (pl. kep.jpg -> 550e8400-e29b...jpg)
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // 3. Adatbázis bejegyzés létrehozása
            var galleryImage = new GalleryImage
            {
                CategoryId = categoryId,
                Title = title,
                // Így érjük majd el a böngészőből: https://localhost:7113/uploads/kepneve.jpg
                ImagePath = "/uploads/" + uniqueFileName,
                UploadDate = DateTime.UtcNow
            };

            _context.GalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync();

            return Ok(galleryImage);
        }

        // DELETE: api/Gallery/images/5
        // Kép törlése (adatbázisból ÉS a fájlrendszerből is)
        [HttpDelete("images/{id}")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var image = await _context.GalleryImages.FindAsync(id);
            if (image == null) return NotFound();

            // Fájl törlése a szerverről, hogy ne szemetelje tele a tárhelyet
            string fullPath = _environment.WebRootPath + image.ImagePath;
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}