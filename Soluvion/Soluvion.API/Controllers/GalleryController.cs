using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using Soluvion.API.Models.DTOs; // Itt vannak az új DTO-k
using Soluvion.API.Services;    // ITenantContext
using System.Security.Claims;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GalleryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly ITenantContext _tenantContext; // ÚJ

        public GalleryController(AppDbContext context, Cloudinary cloudinary, ITenantContext tenantContext)
        {
            _context = context;
            _cloudinary = cloudinary;
            _tenantContext = tenantContext;
        }

        // Segéd: Admin műveleteknél a bejelentkezett felhasználó cégét nézzük
        private int GetUserCompanyId()
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null && int.TryParse(companyClaim.Value, out int companyId))
            {
                return companyId;
            }
            return 0;
        }

        // --- KÉPEK KEZELÉSE ---

        // GET: api/Gallery (Publikus lista)
        [HttpGet]
        [AllowAnonymous] // Vendég is láthatja!
        public async Task<ActionResult<IEnumerable<GalleryImageDto>>> GetImages()
        {
            // 1. Tenant Contextből olvassuk ki a céget
            var currentCompany = _tenantContext.CurrentCompany;
            if (currentCompany == null) return BadRequest("Nem sikerült azonosítani a szalont.");

            var images = await _context.GalleryImages
                .Include(i => i.Category)
                .Where(i => i.Category.CompanyId == currentCompany.Id)
                .OrderBy(i => i.OrderIndex)
                .ThenByDescending(i => i.UploadDate)
                .ToListAsync();

            // 2. Mapping DTO-ba (így elkerüljük a körkörös hivatkozást)
            var result = images.Select(i => new GalleryImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImagePath,
                CategoryId = i.CategoryId,
                // Ha nincs kategória, egy default dictionary-t adunk vissza
                Category = i.Category != null ? i.Category.Name : new Dictionary<string, string> { { "hu", "Egyéb" } },
                Title = i.Title,
                OrderIndex = i.OrderIndex
            });

            return Ok(result);
        }

        // POST: api/Gallery (Kép feltöltés)
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GalleryImageDto>> UploadImage(IFormFile file, [FromForm] string category)
        {
            if (file == null || file.Length == 0) return BadRequest("Nem választottál ki képet!");

            int companyId = GetUserCompanyId();
            if (companyId == 0) return Unauthorized("Érvénytelen token.");

            // 1. Cloudinary feltöltés
            var uploadResult = new ImageUploadResult();
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

            if (uploadResult.Error != null)
                return BadRequest($"Hiba a feltöltés során: {uploadResult.Error.Message}");

            // 2. Kategória kezelés
            string categoryNameString = category?.Trim() ?? "Egyéb";

            var existingCategories = await _context.GalleryCategories
                .Where(c => c.CompanyId == companyId)
                .ToListAsync();

            var galleryCategory = existingCategories
                .FirstOrDefault(c => c.Name.ContainsKey("hu") && c.Name["hu"] == categoryNameString);

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
                ImagePath = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId,
                CategoryId = galleryCategory.Id,
                Title = new Dictionary<string, string> { { "hu", file.FileName } },
                UploadDate = DateTime.UtcNow,
                OrderIndex = 9999
            };

            _context.GalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync();

            // DTO válasz
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

        // PUT: api/Gallery/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateImage(int id, [FromBody] GalleryImageUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();
            int companyId = GetUserCompanyId();
            if (companyId == 0) return Unauthorized();

            var image = await _context.GalleryImages
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null) return NotFound();
            if (image.Category != null && image.Category.CompanyId != companyId) return Forbid();

            if (dto.Title != null)
            {
                image.Title = dto.Title;
            }
            image.OrderIndex = dto.OrderIndex;

            // Kategória váltás logika
            bool categoryChanged = false;
            if (dto.CategoryName != null && dto.CategoryName.Any())
            {
                if (image.Category == null)
                {
                    categoryChanged = true;
                }
                else
                {
                    var dict1 = image.Category.Name;
                    var dict2 = dto.CategoryName;
                    string name1 = dict1.ContainsKey("hu") ? dict1["hu"] : "";
                    string name2 = dict2.ContainsKey("hu") ? dict2["hu"] : "";
                    if (name1 != name2) categoryChanged = true;
                }
            }

            if (categoryChanged)
            {
                string targetHuName = dto.CategoryName.ContainsKey("hu") ? dto.CategoryName["hu"] : "Egyéb";
                var existingCategories = await _context.GalleryCategories
                    .Where(c => c.CompanyId == companyId)
                    .ToListAsync();

                var newCategory = existingCategories
                    .FirstOrDefault(c => c.Name.ContainsKey("hu") && c.Name["hu"] == targetHuName);

                if (newCategory == null)
                {
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

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int id)
        {
            int companyId = GetUserCompanyId();
            if (companyId == 0) return Unauthorized();

            var image = await _context.GalleryImages.Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id);
            if (image == null) return NotFound();
            if (image.Category != null && image.Category.CompanyId != companyId) return Forbid();

            if (!string.IsNullOrEmpty(image.PublicId))
            {
                await _cloudinary.DestroyAsync(new DeletionParams(image.PublicId));
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