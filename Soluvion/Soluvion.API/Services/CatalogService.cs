using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.Domain.Models;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public CatalogService(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        private int GetCurrentCompanyId() => _tenantContext.CurrentCompany?.Id ?? 0;

        public async Task<IEnumerable<CatalogDto>> GetCatalogAsync()
        {
            // A Global Query Filter automatikusan szur a bérlore!
            var services = await _context.Services
                .Include(s => s.Variants).ThenInclude(v => v.DefaultProducts).ThenInclude(dp => dp.Product)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();

            return services.Select(MapToDto).ToList();
        }

        public async Task<CatalogDto?> CreateCatalogItemAsync(CatalogDto dto)
        {
            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return null;

            var service = new Service
            {
                CompanyId = companyId,
                Name = dto.Name ?? new Dictionary<string, string> { { "hu", "Új szolgáltatás" } },
                Category = dto.Category != null && dto.Category.Any() ? dto.Category : new Dictionary<string, string> { { "hu", "Egyéb" } },
                Description = dto.Description ?? new Dictionary<string, string>(),
                DefaultPrice = dto.DefaultPrice ?? 0,
                DefaultDuration = dto.DefaultDuration,
                OrderIndex = dto.OrderIndex,
                Variants = dto.Variants?.Select(v => new ServiceVariant
                {
                    VariantName = v.VariantName ?? new Dictionary<string, string> { { "hu", "Normál" } },
                    Price = v.Price ?? 0,
                    Duration = v.Duration,
                    // ÚJ: ProfileModifiers hozzáadása létrehozáskor
                    ProfileModifiers = v.ProfileModifiers ?? new Dictionary<string, string>(), DefaultProducts = v.DefaultProducts?.Select(dp => new ServiceVariantProduct { ProductId = dp.ProductId, DefaultQuantity = dp.DefaultQuantity }).ToList() ?? new List<ServiceVariantProduct>()
                }).ToList() ?? new List<ServiceVariant>()
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return MapToDto(service);
        }

        public async Task<CatalogDto?> UpdateCatalogItemAsync(int id, CatalogDto dto)
        {
            var existing = await _context.Services
                .Include(s => s.Variants).ThenInclude(v => v.DefaultProducts).ThenInclude(dp => dp.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existing == null) return null; // A szuro miatt csak a sajátját találja meg

            // 1. Alap adatok frissítése
            existing.Name = dto.Name;
            existing.Category = dto.Category;
            existing.Description = dto.Description;
            existing.DefaultPrice = dto.DefaultPrice ?? 0;
            existing.DefaultDuration = dto.DefaultDuration;
            existing.OrderIndex = dto.OrderIndex;

            // 2. Variánsok törlése
            var incomingIds = dto.Variants?.Select(v => v.Id).ToList() ?? new List<int>();
            var toDelete = existing.Variants.Where(v => v.Id != 0 && !incomingIds.Contains(v.Id)).ToList();
            _context.ServiceVariants.RemoveRange(toDelete);

            // 3. Variánsok hozzáadása / frissítése
            if (dto.Variants != null)
            {
                foreach (var vDto in dto.Variants)
                {
                    if (vDto.Id == 0)
                    {
                        existing.Variants.Add(new ServiceVariant
                        {
                            VariantName = vDto.VariantName,
                            Price = vDto.Price ?? 0,
                            Duration = vDto.Duration,
                            // ÚJ: ProfileModifiers mentése új variáns esetén
                            ProfileModifiers = vDto.ProfileModifiers ?? new Dictionary<string, string>()
                        });
                    }
                    else
                    {
                        var existVar = existing.Variants.FirstOrDefault(x => x.Id == vDto.Id);
                        if (existVar != null)
                        {
                            existVar.VariantName = vDto.VariantName;
                            existVar.Price = vDto.Price ?? 0;
                            existVar.Duration = vDto.Duration;
                            // ÚJ: ProfileModifiers mentése variáns módosításakor
                            existVar.ProfileModifiers = vDto.ProfileModifiers ?? new Dictionary<string, string>();
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return MapToDto(existing);
        }

        public async Task<bool> DeleteCatalogItemAsync(int id)
        {
            var s = await _context.Services.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return false;

            _context.Services.Remove(s);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateVariantRecipeAsync(int variantId, List<ServiceVariantProductDto> products) { var variant = await _context.ServiceVariants.Include(v => v.DefaultProducts).FirstOrDefaultAsync(v => v.Id == variantId); if (variant == null) return false; _context.ServiceVariantProducts.RemoveRange(variant.DefaultProducts); if (products != null && products.Any()) { var newLinks = products.Select(p => new ServiceVariantProduct { ServiceVariantId = variantId, ProductId = p.ProductId, DefaultQuantity = p.DefaultQuantity }).ToList(); _context.ServiceVariantProducts.AddRange(newLinks); } await _context.SaveChangesAsync(); return true; } // --- SEGÉDMETÓDUS ---
        private CatalogDto MapToDto(Service s)
        {
            return new CatalogDto
            {
                Id = s.Id,
                CompanyId = s.CompanyId,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                DefaultPrice = s.DefaultPrice,
                DefaultDuration = s.DefaultDuration,
                OrderIndex = s.OrderIndex,
                Variants = s.Variants?.Select(v => new CatalogVariantDto
                {
                    Id = v.Id,
                    VariantName = v.VariantName,
                    Price = v.Price,
                    Duration = v.Duration,
                    // ÚJ: ProfileModifiers lekérése az adatbázisból
                    ProfileModifiers = v.ProfileModifiers ?? new Dictionary<string, string>(), DefaultProducts = v.DefaultProducts?.Select(dp => new ServiceVariantProductDto { ProductId = dp.ProductId, ProductName = dp.Product?.Name, DefaultQuantity = dp.DefaultQuantity }).ToList() ?? new List<ServiceVariantProductDto>()
                }).ToList() ?? new List<CatalogVariantDto>()
            };
        }
    }
}
