using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using System.Security.Claims;

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
        public async Task<ActionResult<IEnumerable<object>>> GetImages([FromQuery] int companyId = 7)
        {
            var images = await _context.GalleryImages
                .Include(i => i.Category)
                .Where(i => i.Category.CompanyId == companyId)
                .Select(i => new
                {
                    id = i.Id,
                    imageUrl = i.ImagePath,
                    category = i.Category != null ? i.Category.Name : "Egyéb",
                    title = i.Title
                })
                .ToListAsync();

            return Ok(images);
        }

        // --- ÚJ VÉGPONT: Kategóriák lekérése ---
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories([FromQuery] int companyId = 1)
        {
            var categories = await _context.GalleryCategories
                .Where(c => c.CompanyId == companyId)
                .Select(c => c.Name)
                .Distinct() // Hogy ne legyen duplikáció
                .ToListAsync();

            return Ok(categories);
        }

        // POST: api/Gallery
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<object>> UploadImage(IFormFile file, [FromForm] string category)
        {
            if (file == null || file.Length == 0) return BadRequest("Nem választottál ki képet!");

            string uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var fullImageUrl = $"{baseUrl}/images/{uniqueFileName}";

            int companyId = 1;
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null) int.TryParse(companyClaim.Value, out companyId);

            string categoryName = category?.Trim() ?? "Egyéb"; // Trim, hogy a szóközök ne zavarjanak

            // Kategória keresése vagy létrehozása
            var galleryCategory = await _context.GalleryCategories
                .FirstOrDefaultAsync(c => c.Name == categoryName && c.CompanyId == companyId);

            if (galleryCategory == null)
            {
                galleryCategory = new GalleryCategory
                {
                    Name = categoryName,
                    CompanyId = companyId
                };
                _context.GalleryCategories.Add(galleryCategory);
                await _context.SaveChangesAsync();
            }

            var galleryImage = new GalleryImage
            {
                ImagePath = fullImageUrl,
                CategoryId = galleryCategory.Id,
                Title = file.FileName,
                UploadDate = DateTime.UtcNow
            };

            _context.GalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync();

            return Ok(new { id = galleryImage.Id, imageUrl = galleryImage.ImagePath, category = galleryCategory.Name });
        }

        // DELETE: api/Gallery/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int id)
        {
            int companyId = 1;
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null) int.TryParse(companyClaim.Value, out companyId);

            var image = await _context.GalleryImages
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null) return NotFound();

            if (image.Category != null && image.Category.CompanyId != companyId)
            {
                return Forbid();
            }

            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}