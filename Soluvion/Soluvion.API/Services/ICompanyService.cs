using Soluvion.API.DTOs;
using Soluvion.API.Models.DTOs;

namespace Soluvion.API.Services
{
    public interface ICompanyService
    {
        Task<CompanyPublicProfileDto?> GetPublicConfigAsync();
        Task<CompanySettingsDto?> GetCompanySettingsAsync(int companyId);
        Task<CompanySettingsDto?> UpdateCompanyAsync(int companyId, CompanySettingsDto dto);
        Task<string?> UploadBrandingImageAsync(int companyId, IFormFile file, string type);
    }
}