using Soluvion.API.Data;
using Soluvion.API.DTOs;
using Soluvion.API.Models.DTOs;
using Soluvion.API.Models.Enums;

namespace Soluvion.API.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IImageService _imageService;

        public CompanyService(AppDbContext context, ITenantContext tenantContext, IImageService imageService)
        {
            _context = context;
            _tenantContext = tenantContext;
            _imageService = imageService;
        }

        public Task<CompanyPublicProfileDto?> GetPublicConfigAsync()
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null) return Task.FromResult<CompanyPublicProfileDto?>(null);

            var dto = new CompanyPublicProfileDto
            {
                Id = company.Id,
                Name = company.Name,
                LogoUrl = company.LogoUrl,
                LogoHeight = company.LogoHeight,
                FooterImageUrl = company.FooterImageUrl,
                FooterHeight = company.FooterHeight,
                HeroImageUrl = company.HeroImageUrl,
                PrimaryColor = company.PrimaryColor,
                SecondaryColor = company.SecondaryColor,
                OpeningHoursTitle = company.OpeningHoursTitle,
                OpeningHoursDescription = company.OpeningHoursDescription,
                OpeningTimeSlots = company.OpeningTimeSlots,
                OpeningExtraInfo = company.OpeningExtraInfo,
                Phone = company.Phone,
                Email = company.Email,
                FacebookUrl = company.FacebookUrl,
                InstagramUrl = company.InstagramUrl,
                TikTokUrl = company.TikTokUrl,
                MapEmbedUrl = company.MapEmbedUrl,
                State = company.State,
                City = company.City,
                StreetName = company.StreetName,
                HouseNumber = company.HouseNumber,
                PostalCode = company.PostalCode,
                DefaultLanguage = company.DefaultLanguage,
                SupportedLanguages = company.Languages
                    .Where(l => l.Status == TranslationStatus.Published)
                    .Select(l => l.LanguageCode)
                    .ToList()
            };

            if (!dto.SupportedLanguages.Contains(dto.DefaultLanguage))
            {
                dto.SupportedLanguages.Add(dto.DefaultLanguage);
            }

            return Task.FromResult<CompanyPublicProfileDto?>(dto);
        }

        public async Task<CompanySettingsDto?> GetCompanySettingsAsync(int companyId)
        {
            var c = await _context.Companies.FindAsync(companyId);
            if (c == null) return null;

            return new CompanySettingsDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                City = c.City,
                StreetName = c.StreetName,
                HouseNumber = c.HouseNumber,
                PostalCode = c.PostalCode,
                State = c.State,
                FacebookUrl = c.FacebookUrl,
                InstagramUrl = c.InstagramUrl,
                TikTokUrl = c.TikTokUrl,
                MapEmbedUrl = c.MapEmbedUrl,
                OpeningHoursTitle = c.OpeningHoursTitle,
                OpeningHoursDescription = c.OpeningHoursDescription,
                OpeningTimeSlots = c.OpeningTimeSlots,
                OpeningExtraInfo = c.OpeningExtraInfo,
                PrimaryColor = c.PrimaryColor,
                SecondaryColor = c.SecondaryColor,
                FooterHeight = c.FooterHeight,
                LogoHeight = c.LogoHeight,
                LogoUrl = c.LogoUrl,
                HeroImageUrl = c.HeroImageUrl,
                FooterImageUrl = c.FooterImageUrl
            };
        }

        public async Task<CompanySettingsDto?> UpdateCompanyAsync(int companyId, CompanySettingsDto dto)
        {
            var existing = await _context.Companies.FindAsync(companyId);
            if (existing == null) return null;

            existing.Name = dto.Name;
            existing.Email = dto.Email;
            existing.Phone = dto.Phone;
            existing.City = dto.City;
            existing.StreetName = dto.StreetName;
            existing.HouseNumber = dto.HouseNumber;
            existing.PostalCode = dto.PostalCode;
            existing.State = dto.State;

            existing.FacebookUrl = dto.FacebookUrl;
            existing.InstagramUrl = dto.InstagramUrl;
            existing.TikTokUrl = dto.TikTokUrl;
            existing.MapEmbedUrl = dto.MapEmbedUrl;

            existing.OpeningHoursTitle = dto.OpeningHoursTitle;
            existing.OpeningHoursDescription = dto.OpeningHoursDescription;
            existing.OpeningTimeSlots = dto.OpeningTimeSlots;
            existing.OpeningExtraInfo = dto.OpeningExtraInfo;

            existing.PrimaryColor = dto.PrimaryColor;
            existing.SecondaryColor = dto.SecondaryColor;
            existing.FooterHeight = dto.FooterHeight;
            existing.LogoHeight = dto.LogoHeight;

            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<string?> UploadBrandingImageAsync(int companyId, IFormFile file, string type)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null) return null;

            var uploadResult = await _imageService.UploadImageAsync(file, $"soluvion/company_{companyId}/branding");
            if (uploadResult == null) return null;

            string? oldPublicId = type switch
            {
                "logo" => company.LogoPublicId,
                "hero" => company.HeroImagePublicId,
                "footer" => company.FooterImagePublicId,
                _ => null
            };

            if (!string.IsNullOrEmpty(oldPublicId))
            {
                await _imageService.DeleteImageAsync(oldPublicId);
            }

            switch (type)
            {
                case "logo":
                    company.LogoUrl = uploadResult.Value.Url;
                    company.LogoPublicId = uploadResult.Value.PublicId;
                    break;
                case "hero":
                    company.HeroImageUrl = uploadResult.Value.Url;
                    company.HeroImagePublicId = uploadResult.Value.PublicId;
                    break;
                case "footer":
                    company.FooterImageUrl = uploadResult.Value.Url;
                    company.FooterImagePublicId = uploadResult.Value.PublicId;
                    break;
            }

            await _context.SaveChangesAsync();
            return uploadResult.Value.Url;
        }
    }
}