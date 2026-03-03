using Microsoft.AspNetCore.Http;

namespace Soluvion.API.Services
{
    public interface IImageService
    {
        Task<(string Url, string PublicId)?> UploadImageAsync(IFormFile file, string folder, int? maxWidth = null);
        Task<bool> DeleteImageAsync(string publicId);
    }
}