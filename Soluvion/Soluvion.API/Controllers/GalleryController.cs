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
        private readonly Cloudinary _cloudinary;

        public GalleryController(AppDbContext context, Cloudinary cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        private int GetCurrentCompanyId()
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null && int.TryParse(companyClaim.Value, out int companyId))
            {
                return companyId;
            }
            return 0;
        }

        // --- KÉPEK KEZELÉSE ---

        // GET: api/Gallery?companyId=7
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetImages([FromQuery] int companyId)
        {
            if (companyId <= 0) return BadRequest("CompanyId megadása kötelező!");

            var images = await _context.GalleryImages
                .Include(i => i.Category)
                .Where(i => i.Category.CompanyId == companyId)
                .OrderBy(i => i.OrderIndex)
                .ThenByDescending(i => i.UploadDate)
                .ToListAsync();

            // Memóriában vetítjük ki, hogy a Dictionary-t kezelni tudjuk
            var result = images.Select(i => new
            {
                id = i.Id,
                imageUrl = i.ImagePath,
                categoryId = i.CategoryId,
                // A kategória neve mostantól Dictionary objektum
                category = i.Category != null ? i.Category.Name : new Dictionary<string, string> { { "hu", "Egyéb" } },
                title = i.Title,
                orderIndex = i.OrderIndex
            });

            return Ok(result);
        }

        // POST: api/Gallery (Kép feltöltés)
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<object>> UploadImage(IFormFile file, [FromForm] string category)
        {
            if (file == null || file.Length == 0) return BadRequest("Nem választottál ki képet!");

            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return Unauthorized("Érvénytelen token.");

            // 1. Cloudinary feltöltés
            var uploadResult = new ImageUploadResult();
            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = $"soluvion/company_{companyId}/gallery",
                        Transformation = new Transformation().Width(1920).Crop("limit")
                    };
                    uploadResult = await _cloudinary.UploadAsync(uploadParams);
                }
            }

            if (uploadResult.Error != null)
                return BadRequest($"Hiba a feltöltés során: {uploadResult.Error.Message}");

            // 2. Kategória keresése vagy létrehozása
            // Mivel form-data-ból string jön, ezt "hu" (magyar) névként kezeljük.
            string categoryNameString = category?.Trim() ?? "Egyéb";

            // Memóriában keressük meg a "hu" kulcs alapján (EF Core ValueConversion limitation)
            var existingCategories = await _context.GalleryCategories
                .Where(c => c.CompanyId == companyId)
                .ToListAsync();

            var galleryCategory = existingCategories
                .FirstOrDefault(c => c.Name.ContainsKey("hu") && c.Name["hu"] == categoryNameString);

            if (galleryCategory == null)
            {
                galleryCategory = new GalleryCategory
                {
                    // Létrehozásnál beállítjuk a magyar nevet
                    Name = new Dictionary<string, string> { { "hu", categoryNameString } },
                    CompanyId = companyId
                };
                _context.GalleryCategories.Add(galleryCategory);
                await _context.SaveChangesAsync();
            }

            // 3. Mentés DB-be
            var galleryImage = new GalleryImage
            {
                ImagePath = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId,
                CategoryId = galleryCategory.Id,
                Title = file.FileName,
                UploadDate = DateTime.UtcNow,
                OrderIndex = 9999
            };

            _context.GalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = galleryImage.Id,
                imageUrl = galleryImage.ImagePath,
                categoryId = galleryCategory.Id,
                category = galleryCategory.Name,
                orderIndex = galleryImage.OrderIndex
            });
        }

        // PUT: api/Gallery/5 (Kép szerkesztése)
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateImage(int id, [FromBody] GalleryImageUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();
            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return Unauthorized();

            var image = await _context.GalleryImages
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null) return NotFound();
            if (image.Category != null && image.Category.CompanyId != companyId) return Forbid();

            // Adatok frissítése
            image.Title = dto.Title ?? "";
            image.OrderIndex = dto.OrderIndex;

            // Kategória váltás logikája (Dictionary összehasonlítással)
            // Ha a DTO-ban van kategória név, és az különbözik a jelenlegitől
            bool categoryChanged = false;
            if (dto.CategoryName != null && dto.CategoryName.Any())
            {
                if (image.Category == null)
                {
                    categoryChanged = true;
                }
                else
                {
                    // Összehasonlítjuk a két szótárat
                    var dict1 = image.Category.Name;
                    var dict2 = dto.CategoryName;

                    // Egyszerűsített összehasonlítás: Ha a "hu" kulcs különbözik, vagy a elemszám
                    // (A pontos Dictionary equality bonyolult, de MVP-re a "hu" egyezés elég)
                    string name1 = dict1.ContainsKey("hu") ? dict1["hu"] : "";
                    string name2 = dict2.ContainsKey("hu") ? dict2["hu"] : "";

                    if (name1 != name2) categoryChanged = true;
                }
            }

            if (categoryChanged)
            {
                // Megkeressük, létezik-e már ilyen kategória (a teljes dictionary alapján nehéz, "hu" alapján keresünk)
                string targetHuName = dto.CategoryName.ContainsKey("hu") ? dto.CategoryName["hu"] : "Egyéb";

                var existingCategories = await _context.GalleryCategories
                   .Where(c => c.CompanyId == companyId)
                   .ToListAsync();

                var newCategory = existingCategories
                    .FirstOrDefault(c => c.Name.ContainsKey("hu") && c.Name["hu"] == targetHuName);

                if (newCategory == null)
                {
                    // Ha nincs, létrehozzuk a DTO-ban kapott teljes Dictionary-vel
                    newCategory = new GalleryCategory
                    {
                        Name = dto.CategoryName,
                        CompanyId = companyId
                    };
                    _context.GalleryCategories.Add(newCategory);
                }
                image.Category = newCategory;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Sikeres mentés" });
        }

        // DELETE: api/Gallery/5 (Kép törlése)
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int id)
        {
            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return Unauthorized();

            var image = await _context.GalleryImages.Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id);
            if (image == null) return NotFound();
            if (image.Category != null && image.Category.CompanyId != companyId) return Forbid();

            // Törlés Cloudinary-ből
            if (!string.IsNullOrEmpty(image.PublicId))
            {
                await _cloudinary.DestroyAsync(new DeletionParams(image.PublicId));
            }

            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- KATEGÓRIÁK KEZELÉSE ---

        // GET: api/Gallery/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<GalleryCategory>>> GetCategories([FromQuery] int companyId)
        {
            if (companyId <= 0) return BadRequest();
            return await _context.GalleryCategories
                .Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();
        }

        // POST: api/Gallery/categories
        [HttpPost("categories")]
        [Authorize]
        public async Task<ActionResult<GalleryCategory>> CreateCategory([FromBody] CategoryDto dto)
        {
            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return Unauthorized();

            int minIndex = await _context.GalleryCategories
                .Where(c => c.CompanyId == companyId)
                .MinAsync(c => (int?)c.OrderIndex) ?? 0;

            var newCat = new GalleryCategory
            {
                Name = dto.Name, // Dictionary átvétele
                CompanyId = companyId,
                OrderIndex = minIndex - 1
            };
            _context.GalleryCategories.Add(newCat);
            await _context.SaveChangesAsync();

            return Ok(newCat);
        }

        // PUT: api/Gallery/categories/5 (Átnevezés VAGY Sorrendezés)
        [HttpPut("categories/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            int companyId = GetCurrentCompanyId();
            var cat = await _context.GalleryCategories.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (cat == null) return NotFound();

            // Ha jött név (Dictionary), frissítjük
            if (dto.Name != null && dto.Name.Any())
            {
                cat.Name = dto.Name;
            }

            // Mindig frissítjük az indexet
            cat.OrderIndex = dto.OrderIndex;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Gallery/categories/5
        [HttpDelete("categories/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            int companyId = GetCurrentCompanyId();
            var cat = await _context.GalleryCategories
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (cat == null) return NotFound();

            if (cat.Images != null && cat.Images.Any())
            {
                return BadRequest("A kategória nem törölhető, mert képeket tartalmaz! Előbb töröld vagy mozgasd át a képeket.");
            }

            _context.GalleryCategories.Remove(cat);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}