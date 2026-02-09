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
                .Select(i => new
                {
                    id = i.Id,
                    imageUrl = i.ImagePath,
                    categoryId = i.CategoryId, // Fontos: ID alapján csoportosítunk
                    category = i.Category != null ? i.Category.Name : "Egyéb",
                    title = i.Title,
                    orderIndex = i.OrderIndex
                })
                .ToListAsync();

            return Ok(images);
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

            // Kategória váltás (ha változott)
            if (!string.IsNullOrEmpty(dto.CategoryName) &&
                (image.Category == null || image.Category.Name != dto.CategoryName))
            {
                var newCategory = await _context.GalleryCategories
                    .FirstOrDefaultAsync(c => c.Name == dto.CategoryName && c.CompanyId == companyId);

                if (newCategory == null)
                {
                    newCategory = new GalleryCategory { Name = dto.CategoryName, CompanyId = companyId };
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

        // --- KATEGÓRIÁK KEZELÉSE (ÚJ) ---

        // GET: api/Gallery/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<GalleryCategory>>> GetCategories([FromQuery] int companyId)
        {
            if (companyId <= 0) return BadRequest();
            return await _context.GalleryCategories
                .Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.Name) // Itt lehetne OrderIndex is, ha van
                .ToListAsync();
        }

        // POST: api/Gallery/categories (Új Galéria létrehozása)
        [HttpPost("categories")]
        [Authorize]
        public async Task<ActionResult<GalleryCategory>> CreateCategory([FromBody] CategoryDto dto)
        {
            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return Unauthorized();

            var newCat = new GalleryCategory
            {
                Name = dto.Name,
                CompanyId = companyId
            };
            _context.GalleryCategories.Add(newCat);
            await _context.SaveChangesAsync();

            return Ok(newCat);
        }

        // PUT: api/Gallery/categories/5 (Átnevezés)
        [HttpPut("categories/{id}")]
        [Authorize]
        public async Task<IActionResult> RenameCategory(int id, [FromBody] CategoryDto dto)
        {
            int companyId = GetCurrentCompanyId();
            var cat = await _context.GalleryCategories.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (cat == null) return NotFound();

            cat.Name = dto.Name;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Gallery/categories/5 (Galéria törlése)
        [HttpDelete("categories/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            int companyId = GetCurrentCompanyId();
            var cat = await _context.GalleryCategories
                .Include(c => c.Images) // Hogy lássuk, van-e benne kép
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (cat == null) return NotFound();

            // Csak akkor engedjük törölni, ha üres (biztonsági okokból)
            if (cat.Images != null && cat.Images.Any())
            {
                return BadRequest("A kategória nem törölhető, mert képeket tartalmaz! Előbb töröld vagy mozgasd át a képeket.");
            }

            _context.GalleryCategories.Remove(cat);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    // DTO-k
    //public class GalleryImageUpdateDto
    //{
    //    public int Id { get; set; }
    //    public string Title { get; set; } = string.Empty;
    //    public string CategoryName { get; set; } = string.Empty;
    //    public int OrderIndex { get; set; }
    //}

    //public class CategoryDto
    //{
    //    public string Name { get; set; } = string.Empty;
    //}
}