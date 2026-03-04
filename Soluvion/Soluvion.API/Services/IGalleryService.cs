using Microsoft.AspNetCore.Http;
using Soluvion.API.Models.DTOs;

namespace Soluvion.API.Services
{
    public interface IGalleryService
    {
        Task<IEnumerable<GalleryImageDto>> GetImagesAsync();
        Task<(GalleryImageDto? Image, string? ErrorMessage)> UploadImageAsync(IFormFile file, int categoryId);
        Task<bool> UpdateImageAsync(int id, GalleryImageUpdateDto dto);
        Task<bool> DeleteImageAsync(int id);

        Task<IEnumerable<GalleryCategoryDto>> GetCategoriesAsync();
        Task<GalleryCategoryDto?> CreateCategoryAsync(CategoryDto dto);
        Task<bool> UpdateCategoryAsync(int id, CategoryDto dto);
        Task<(bool Success, string? ErrorMessage)> DeleteCategoryAsync(int id);
    }
}