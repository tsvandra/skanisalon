using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Services
{
    public class ImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;

        public ImageService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<(string Url, string PublicId)?> UploadImageAsync(IFormFile file, string folder, int? maxWidth = null)
        {
            if (file == null || file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            if (maxWidth.HasValue)
            {
                uploadParams.Transformation = new Transformation().Width(maxWidth.Value).Crop("limit");
            }

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null) return null;

            return (result.SecureUrl.ToString(), result.PublicId);
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId)) return false;

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            return result.Result == "ok";
        }
    }
}