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
    [Route("api/[controller]")]
    [ApiController]
    public class GalleryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary; // Injektáljuk a Cloudinary-t

        public GalleryController(AppDbContext context, Cloudinary cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
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

            // Itt már a teljes URL-t kapjuk vissza az adatbázisból, nem kell machinálni a path-szal
            var images = await _context.GalleryImages
                .Include(i => i.Category)
                .Where(i => i.Category.CompanyId == companyId)
                .OrderBy(i => i.OrderIndex) // Már előkészítve a rendezéshez
                .ThenByDescending(i => i.UploadDate)
                .Select(i => new
                {
                    id = i.Id,
                    imageUrl = i.ImagePath, // Cloudinary URL
                    category = i.Category != null ? i.Category.Name : "Egyéb",
                    title = i.Title,
                    orderIndex = i.OrderIndex
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
                // Itt is jöhet majd OrderBy, ha a kategóriákat is sorrendezzük
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

            // 1. Feltöltés Cloudinary-be
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.FileName, stream),
                        // Mappastruktúra: soluvion/company_{id}/gallery
                        Folder = $"soluvion/company_{companyId}/gallery",
                        // Opcionális: Transzformáció (pl. max szélesség, hogy spóroljunk a hellyel)
                        Transformation = new Transformation().Width(1920).Crop("limit")
                    };
                    uploadResult = await _cloudinary.UploadAsync(uploadParams);
                }
            }

            if (uploadResult.Error != null)
            {
                return BadRequest($"Hiba a feltöltés során: {uploadResult.Error.Message}");
            }

            // 2. Kategória kezelés (ez maradt a régi)
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

            // 3. Mentés adatbázisba
            var galleryImage = new GalleryImage
            {
                ImagePath = uploadResult.SecureUrl.ToString(), // Teljes HTTPS URL
                PublicId = uploadResult.PublicId,              // Törléshez kell!
                CategoryId = galleryCategory.Id,
                Title = file.FileName,
                UploadDate = DateTime.UtcNow,
                OrderIndex = 9999 // Ideiglenesen a végére, amíg nincs rendezés implementálva
            };

            _context.GalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = galleryImage.Id,
                imageUrl = galleryImage.ImagePath,
                category = galleryCategory.Name,
                orderIndex = galleryImage.OrderIndex
            });
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

            if (image.Category != null && image.Category.CompanyId != companyId)
            {
                return Forbid("Nincs jogod más cég képét törölni!");
            }

            // 1. Törlés Cloudinary-ből (Ha van PublicId)
            if (!string.IsNullOrEmpty(image.PublicId))
            {
                var deletionParams = new DeletionParams(image.PublicId);
                var result = await _cloudinary.DestroyAsync(deletionParams);

                if (result.Result != "ok" && result.Result != "not found")
                {
                    // Logolhatnánk, de nem feltétlenül akarjuk megállítani a folyamatot
                    // return BadRequest("Hiba a Cloudinary kép törlésekor.");
                }
            }

            // 2. Törlés DB-ből
            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}