using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.DTOs.CompanyAttributeDtos;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;

namespace Soluvion.API.Services
{
    public class CompanyAttributeService : ICompanyAttributeService
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public CompanyAttributeService(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        public async Task<List<CompanyAttributeDto>> GetAttributesAsync()
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var attributes = await _context.CompanyAttributes
                .Where(ca => ca.CompanyId == companyId)
                .OrderBy(ca => ca.Label)
                .ToListAsync();

            return attributes.Select(ca => new CompanyAttributeDto
            {
                Id = ca.Id,
                Key = ca.Key,
                Label = ca.Label,
                DataType = ca.DataType,
                Options = ca.Options,
                IsRequired = ca.IsRequired,
                ShowOnPublicBooking = ca.ShowOnPublicBooking,
                IsActive = ca.IsActive
            }).ToList();
        }

        public async Task<CompanyAttributeDto> CreateAttributeAsync(CreateUpdateCompanyAttributeDto dto)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            // Kulcs formázása (kisbetű, szóközök alulvonásra), ha a user nem jól írta volna be
            string formattedKey = dto.Key.ToLower().Replace(" ", "_").Trim();

            var exists = await _context.CompanyAttributes.AnyAsync(ca => ca.CompanyId == companyId && ca.Key == formattedKey);
            if (exists) throw new InvalidOperationException("Már létezik ilyen kulcsú jellemző.");

            var attribute = new CompanyAttribute
            {
                CompanyId = companyId,
                Key = formattedKey,
                Label = dto.Label,
                DataType = dto.DataType,
                Options = dto.Options ?? new List<string>(),
                IsRequired = dto.IsRequired,
                ShowOnPublicBooking = dto.ShowOnPublicBooking,
                IsActive = dto.IsActive
            };

            _context.CompanyAttributes.Add(attribute);
            await _context.SaveChangesAsync();

            return new CompanyAttributeDto { Id = attribute.Id, Key = attribute.Key, Label = attribute.Label, DataType = attribute.DataType, Options = attribute.Options, IsRequired = attribute.IsRequired, ShowOnPublicBooking = attribute.ShowOnPublicBooking, IsActive = attribute.IsActive };
        }

        public async Task<CompanyAttributeDto> UpdateAttributeAsync(int id, CreateUpdateCompanyAttributeDto dto)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var attribute = await _context.CompanyAttributes.FirstOrDefaultAsync(ca => ca.Id == id && ca.CompanyId == companyId);
            if (attribute == null) throw new KeyNotFoundException("Jellemző nem található.");

            attribute.Label = dto.Label;
            attribute.DataType = dto.DataType;
            attribute.Options = dto.Options ?? new List<string>();
            attribute.IsRequired = dto.IsRequired;
            attribute.ShowOnPublicBooking = dto.ShowOnPublicBooking;
            attribute.IsActive = dto.IsActive;
            // A Key-t (Kulcsot) szerkesztéskor nem engedjük változtatni, mert azzal eltörnénk a meglévő vendégprofilokat!

            await _context.SaveChangesAsync();

            return new CompanyAttributeDto { Id = attribute.Id, Key = attribute.Key, Label = attribute.Label, DataType = attribute.DataType, Options = attribute.Options, IsRequired = attribute.IsRequired, ShowOnPublicBooking = attribute.ShowOnPublicBooking, IsActive = attribute.IsActive };
        }

        public async Task DeleteAttributeAsync(int id)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var attribute = await _context.CompanyAttributes.FirstOrDefaultAsync(ca => ca.Id == id && ca.CompanyId == companyId);
            if (attribute == null) throw new KeyNotFoundException("Jellemző nem található.");

            _context.CompanyAttributes.Remove(attribute);
            await _context.SaveChangesAsync();
        }
    }
}