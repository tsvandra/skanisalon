using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using Soluvion.API.Models.DTOs;

namespace Soluvion.API.Services
{
    public class GalleryService : IGalleryService
    {
        private readonly AppDbContext _context;
        private readonly IImageService _imageService;
        private readonly ITenantContext _tenantContext;

        public GalleryService(AppDbContext context, IImageService imageService, ITenantContext tenantContext)
        {
            _context = context;
            _imageService = imageService;
            _tenantContext = tenantContext;
        }

        private int GetCurrentCompanyId() => _tenantContext.CurrentCompany?.Id ?? 0;

        // --- KÉPEK KEZELÉSE ---

        public async Task<IEnumerable<GalleryImageDto>> GetImagesAsync()
        {
            // AsNoTracking() hozzáadva a teljesítmény optimalizálása érdekében!
            var images = await _context.GalleryImages
                .AsNoTracking()
                .Include(i => i.Category)
                .OrderBy(i => i.OrderIndex)
                .ThenByDescending(i => i.UploadDate)
                .ToListAsync();

            return images.Select(i => new GalleryImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImagePath,
                CategoryId = i.CategoryId,
                Category = i.Category != null ? i.Category.Name : new Dictionary<string, string> { { "hu", "Egyéb" } },
                Title = i.Title,
                OrderIndex = i.OrderIndex
            });
        }

        public async Task<(GalleryImageDto? Image, string? ErrorMessage)> UploadImageAsync(IFormFile file, int categoryId)
        {
            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return (null, "Érvénytelen szalon azonosító.");

            // Kategória kikeresése ID alapján név helyett
            var galleryCategory = await _context.GalleryCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (galleryCategory == null) return (null, "A megadott kategória nem létezik.");

            var uploadResult = await _imageService.UploadImageAsync(file, $"soluvion/company_{companyId}/gallery", 1920);
            if (uploadResult == null) return (null, "Hiba a feltöltés során.");

            var galleryImage = new GalleryImage
            {
                ImagePath = uploadResult.Value.Url,
                PublicId = uploadResult.Value.PublicId,
                CategoryId = galleryCategory.Id,
                Title = new Dictionary<string, string> { { "hu", file.FileName } },
                UploadDate = DateTime.UtcNow,
                OrderIndex = 9999
            };

            _context.GalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync();

            var dto = new GalleryImageDto
            {
                Id = galleryImage.Id,
                ImageUrl = galleryImage.ImagePath,
                CategoryId = galleryCategory.Id,
                Category = galleryCategory.Name,
                OrderIndex = galleryImage.OrderIndex,
                Title = galleryImage.Title
            };

            return (dto, null);
        }

        public async Task<bool> UpdateImageAsync(int id, GalleryImageUpdateDto dto)
        {
            var image = await _context.GalleryImages.Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id);
            if (image == null) return false; // Nincs meg, vagy másik cégé (az EF elrejti előlünk)

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
                var existingCategories = await _context.GalleryCategories.ToListAsync();
                var newCategory = existingCategories.FirstOrDefault(c => c.Name.ContainsKey("hu") && c.Name["hu"] == targetHuName);

                if (newCategory == null)
                {
                    newCategory = new GalleryCategory
                    {
                        Name = dto.CategoryName,
                        CompanyId = GetCurrentCompanyId()
                    };
                    _context.GalleryCategories.Add(newCategory);
                }
                image.Category = newCategory;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteImageAsync(int id)
        {
            var image = await _context.GalleryImages.FirstOrDefaultAsync(i => i.Id == id);
            if (image == null) return false;

            if (!string.IsNullOrEmpty(image.PublicId))
            {
                await _imageService.DeleteImageAsync(image.PublicId);
            }

            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- KATEGÓRIÁK KEZELÉSE ---

        public async Task<IEnumerable<GalleryCategoryDto>> GetCategoriesAsync()
        {
            // AsNoTracking() hozzáadva a teljesítmény optimalizálása érdekében!
            var categories = await _context.GalleryCategories
                .AsNoTracking()
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();

            return categories.Select(c => new GalleryCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                OrderIndex = c.OrderIndex
            });
        }

        public async Task<GalleryCategoryDto?> CreateCategoryAsync(CategoryDto dto)
        {
            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return null;

            int minIndex = await _context.GalleryCategories.MinAsync(c => (int?)c.OrderIndex) ?? 0;

            var newCat = new GalleryCategory
            {
                Name = dto.Name,
                CompanyId = companyId,
                OrderIndex = minIndex - 1
            };

            _context.GalleryCategories.Add(newCat);
            await _context.SaveChangesAsync();

            return new GalleryCategoryDto
            {
                Id = newCat.Id,
                Name = newCat.Name,
                OrderIndex = newCat.OrderIndex
            };
        }

        public async Task<bool> UpdateCategoryAsync(int id, CategoryDto dto)
        {
            var cat = await _context.GalleryCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return false;

            if (dto.Name != null && dto.Name.Any())
            {
                cat.Name = dto.Name;
            }
            cat.OrderIndex = dto.OrderIndex;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteCategoryAsync(int id)
        {
            var cat = await _context.GalleryCategories
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cat == null) return (false, null); // 404 NotFound

            if (cat.Images != null && cat.Images.Any())
            {
                return (false, "A kategória nem törölhető, mert képeket tartalmaz! Előbb töröld vagy mozgasd át a képeket.");
            }

            _context.GalleryImages.RemoveRange(cat.Images); // Biztos ami biztos alapon, de elvileg ezen a ponton nincs benne kép
            _context.GalleryCategories.Remove(cat);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}