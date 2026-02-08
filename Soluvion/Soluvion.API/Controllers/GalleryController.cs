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
        private readonly IHttpContextAccessor _httpContextAccessor; // URL generáláshoz

        public GalleryController(AppDbContext context, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
        }

        // Segédfüggvény: CompanyId kinyerése a tokenből
        private int GetCurrentCompanyId()
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null && int.TryParse(companyClaim.Value, out int companyId))
            {
                return companyId;
            }
            return 0;
        }

        // GET: api/Gallery?companyId=7
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetImages([FromQuery] int companyId)
        {
            if (companyId <= 0) return BadRequest("CompanyId megadása kötelező!");

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}{request?.PathBase}";

            var images = await _context.GalleryImages
                .Include(i => i.Category)
                .Where(i => i.Category.CompanyId == companyId)
                .OrderByDescending(i => i.UploadDate)
                .Select(i => new
                {
                    id = i.Id,
                    // Ha relatív az út, elé tesszük a domaint, ha abszolút (régi), hagyjuk
                    imageUrl = i.ImagePath.StartsWith("http") ? i.ImagePath : $"{baseUrl}{i.ImagePath}",
                    category = i.Category != null ? i.Category.Name : "Egyéb",
                    title = i.Title
                })
                .ToListAsync();

            return Ok(images);
        }

        // GET: api/Gallery/categories?companyId=7
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories([FromQuery] int companyId)
        {
            if (companyId <= 0) return BadRequest("CompanyId megadása kötelező!");

            var categories = await _context.GalleryCategories
                .Where(c => c.CompanyId == companyId)
                .Select(c => c.Name)
                .Distinct()
                .ToListAsync();

            return Ok(categories);
        }

        // POST: api/Gallery
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<object>> UploadImage(IFormFile file, [FromForm] string category)
        {
            if (file == null || file.Length == 0) return BadRequest("Nem választottál ki képet!");

            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return Unauthorized("Érvénytelen token.");

            string companyFolder = Path.Combine(_environment.WebRootPath, "images", companyId.ToString());
            if (!Directory.Exists(companyFolder)) Directory.CreateDirectory(companyFolder);

            // 2. Egyedi fájlnév
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(companyFolder, uniqueFileName);

            // 3. Mentés lemezre
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // 4. Relatív útvonal mentése (Hogy hordozható legyen az adatbázis)
            string relativePath = $"/images/{companyId}/{uniqueFileName}";

            // 5. Kategória kezelés
            string categoryName = category?.Trim() ?? "Egyéb";

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

            // 6. Mentés adatbázisba
            var galleryImage = new GalleryImage
            {
                ImagePath = relativePath, // Csak a relatív utat mentjük!
                CategoryId = galleryCategory.Id,
                Title = file.FileName,
                UploadDate = DateTime.UtcNow
            };

            _context.GalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync();

            // Válaszban visszaküldjük a teljes URL-t a frontendnek
            var request = _httpContextAccessor.HttpContext?.Request;
            var fullUrl = $"{request?.Scheme}://{request?.Host}{request?.PathBase}{relativePath}";

            return Ok(new { id = galleryImage.Id, imageUrl = fullUrl, category = galleryCategory.Name });
        }

        // DELETE: api/Gallery/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int id)
        {
            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return Unauthorized();

            var image = await _context.GalleryImages
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null) return NotFound();

            // BIZTONSÁG: Csak a saját cég képe törölhető
            if (image.Category != null && image.Category.CompanyId != companyId)
            {
                return Forbid("Nincs jogod más cég képét törölni!");
            }

            // 1. Fájl törlése a lemezről (Ha relatív útvonal)
            if (!string.IsNullOrEmpty(image.ImagePath) && !image.ImagePath.StartsWith("http"))
            {
                // A 'Starts with /' levágása, hogy a Path.Combine helyesen működjön
                string relativePath = image.ImagePath.TrimStart('/', '\\');
                string physicalPath = Path.Combine(_environment.WebRootPath, relativePath);

                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }

            // 2. Törlés DB-ből
            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}