using Soluvion.API.DTOs;

namespace Soluvion.API.Interfaces
{
    public interface ICompanyService
    {
        Task<CompanyPublicProfileDto?> GetPublicConfigAsync();
        Task<CompanySettingsDto?> GetCompanySettingsAsync(int companyId);
        Task<CompanySettingsDto?> UpdateCompanyAsync(int companyId, CompanySettingsDto dto);
        Task<string?> UploadBrandingImageAsync(int companyId, IFormFile file, string type);
    }
}