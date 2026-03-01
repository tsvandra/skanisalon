using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using Soluvion.API.Models.DTOs;
using Soluvion.API.Services;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GalleryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IImageService _imageService; // ÚJ Szerviz
        private readonly ITenantContext _tenantContext;

        public GalleryController(AppDbContext context, IImageService imageService, ITenantContext tenantContext)
        {
            _context = context;
            _imageService = imageService;
            _tenantContext = tenantContext;
        }

        private int GetUserCompanyId()
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null && int.TryParse(companyClaim.Value, out int companyId))
            {
                return companyId;
            }
            return 0;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<GalleryImageDto>>> GetImages()
        {
            var currentCompany = _tenantContext.CurrentCompany;
            if (currentCompany == null) return BadRequest("Nem sikerült azonosítani a szalont.");

            var images = await _context.GalleryImages
                .Include(i => i.Category)
                .Where(i => i.Category.CompanyId == currentCompany.Id)
                .OrderBy(i => i.OrderIndex)
                .ThenByDescending(i => i.UploadDate)
                .ToListAsync();

            var result = images.Select(i => new GalleryImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImagePath,
                CategoryId = i.CategoryId,
                Category = i.Category != null ? i.Category.Name : new Dictionary<string, string> { { "hu", "Egyéb" } },
                Title = i.Title,
                OrderIndex = i.OrderIndex
            });

            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GalleryImageDto>> UploadImage(IFormFile file, [FromForm] string category)
        {
            if (file == null || file.Length == 0) return BadRequest("Nem választottál ki képet!");

            int companyId = GetUserCompanyId();
            if (companyId == 0) return Unauthorized("Érvénytelen token.");

            // 1. Feltöltés az ImageService segítségével (1920px limit)
            var uploadResult = await _imageService.UploadImageAsync(file, $"soluvion/company_{companyId}/gallery", 1920);
            if (uploadResult == null) return BadRequest("Hiba a feltöltés során.");

            // 2. Kategória kezelés
            string categoryNameString = category?.Trim() ?? "Egyéb";
            var existingCategories = await _context.GalleryCategories.Where(c => c.CompanyId == companyId).ToListAsync();
            var galleryCategory = existingCategories.FirstOrDefault(c => c.Name.ContainsKey("hu") && c.Name["hu"] == categoryNameString);

            if (galleryCategory == null)
            {
                galleryCategory = new GalleryCategory
                {
                    Name = new Dictionary<string, string> { { "hu", categoryNameString } },
                    CompanyId = companyId
                };
                _context.GalleryCategories.Add(galleryCategory);
                await _context.SaveChangesAsync();
            }

            // 3. Mentés DB-be
            var galleryImage = new GalleryImage
            {
                ImagePath = uploadResult.Value.Url, // <-- Új szerviz adata
                PublicId = uploadResult.Value.PublicId, // <-- Új szerviz adata
                CategoryId = galleryCategory.Id,
                Title = new Dictionary<string, string> { { "hu", file.FileName } },
                UploadDate = DateTime.UtcNow,
                OrderIndex = 9999
            };

            _context.GalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync();

            return Ok(new GalleryImageDto
            {
                Id = galleryImage.Id,
                ImageUrl = galleryImage.ImagePath,
                CategoryId = galleryCategory.Id,
                Category = galleryCategory.Name,
                OrderIndex = galleryImage.OrderIndex,
                Title = galleryImage.Title
            });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateImage(int id, [FromBody] GalleryImageUpdateDto dto)
        {
            // ... (Ez a rész változatlan marad az előző kódodhoz képest)
            if (id != dto.Id) return BadRequest();
            int companyId = GetUserCompanyId();
            if (companyId == 0) return Unauthorized();

            var image = await _context.GalleryImages.Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id);
            if (image == null) return NotFound();
            if (image.Category != null && image.Category.CompanyId != companyId) return Forbid();

            if (dto.Title != null) image.Title = dto.Title;
            image.OrderIndex = dto.OrderIndex;

            bool categoryChanged = false;
            if (dto.CategoryName != null && dto.CategoryName.Any())
            {
                if (image.Category == null) categoryChanged = true;
                else
                {
                    string name1 = image.Category.Name.ContainsKey("hu") ? image.Category.Name["hu"] : "";
                    string name2 = dto.CategoryName.ContainsKey("hu") ? dto.CategoryName["hu"] : "";
                    if (name1 != name2) categoryChanged = true;
                }
            }

            if (categoryChanged)
            {
                string targetHuName = dto.CategoryName.ContainsKey("hu") ? dto.CategoryName["hu"] : "Egyéb";
                var existingCategories = await _context.GalleryCategories.Where(c => c.CompanyId == companyId).ToListAsync();
                var newCategory = existingCategories.FirstOrDefault(c => c.Name.ContainsKey("hu") && c.Name["hu"] == targetHuName);

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

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int id)
        {
            int companyId = GetUserCompanyId();
            if (companyId == 0) return Unauthorized();

            var image = await _context.GalleryImages.Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id);
            if (image == null) return NotFound();
            if (image.Category != null && image.Category.CompanyId != companyId) return Forbid();

            // Törlés a felhőből az új Szervizzel
            if (!string.IsNullOrEmpty(image.PublicId))
            {
                await _imageService.DeleteImageAsync(image.PublicId);
            }

            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- KATEGÓRIÁK KEZELÉSE ---

        // GET: api/Gallery/categories (Publikus lista)
        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<GalleryCategoryDto>>> GetCategories()
        {
            var currentCompany = _tenantContext.CurrentCompany;
            if (currentCompany == null) return BadRequest();

            var categories = await _context.GalleryCategories
                .Where(c => c.CompanyId == currentCompany.Id)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();

            // DTO Mapping
            var result = categories.Select(c => new GalleryCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                OrderIndex = c.OrderIndex
            });

            return Ok(result);
        }

        // POST: api/Gallery/categories
        [HttpPost("categories")]
        [Authorize]
        public async Task<ActionResult<GalleryCategory>> CreateCategory([FromBody] CategoryDto dto)
        {
            int companyId = GetUserCompanyId();
            if (companyId == 0) return Unauthorized();

            int minIndex = await _context.GalleryCategories
                .Where(c => c.CompanyId == companyId)
                .MinAsync(c => (int?)c.OrderIndex) ?? 0;

            var newCat = new GalleryCategory
            {
                Name = dto.Name,
                CompanyId = companyId,
                OrderIndex = minIndex - 1
            };
            _context.GalleryCategories.Add(newCat);
            await _context.SaveChangesAsync();

            return Ok(newCat);
        }

        // PUT: api/Gallery/categories/5
        [HttpPut("categories/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            int companyId = GetUserCompanyId();
            var cat = await _context.GalleryCategories.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (cat == null) return NotFound();

            if (dto.Name != null && dto.Name.Any())
            {
                cat.Name = dto.Name;
            }
            cat.OrderIndex = dto.OrderIndex;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Gallery/categories/5
        [HttpDelete("categories/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            int companyId = GetUserCompanyId();
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