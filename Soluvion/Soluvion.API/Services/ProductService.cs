using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;

namespace Soluvion.API.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public ProductService(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        private int GetCurrentCompanyId() => _tenantContext.CurrentCompany?.Id ?? 0;

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            return products.Select(MapToDto);
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return null;

            return MapToDto(product);
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                CompanyId = GetCurrentCompanyId(),
                Name = dto.Name,
                Description = dto.Description,
                EAN = dto.EAN,
                Shade = dto.Shade,
                ImageUrl = dto.ImageUrl,
                IsProfessional = dto.IsProfessional,
                IsRetail = dto.IsRetail,
                Unit = dto.Unit,
                PackageSize = dto.PackageSize,
                CostPrice = dto.CostPrice,
                RetailPrice = dto.RetailPrice,
                LowStockThreshold = dto.LowStockThreshold,
                CurrentStock = 0, // Új termék készlete mindig 0 induláskor
                CreationDate = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return MapToDto(product);
        }

        public async Task<bool> UpdateProductAsync(int id, UpdateProductDto dto)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            
            if (product == null) return false;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.EAN = dto.EAN;
            product.Shade = dto.Shade;
            product.ImageUrl = dto.ImageUrl;
            product.IsProfessional = dto.IsProfessional;
            product.IsRetail = dto.IsRetail;
            product.Unit = dto.Unit;
            product.PackageSize = dto.PackageSize;
            product.CostPrice = dto.CostPrice;
            product.RetailPrice = dto.RetailPrice;
            product.LowStockThreshold = dto.LowStockThreshold;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            
            if (product == null) return false;

            // Soft delete, mert lehetnek rajta korábbi raktári mozgások, amiket nem akarunk megzavarni
            product.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        // Segédfüggvény a leképzéshez
        private static ProductDto MapToDto(Product p)
        {
            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                EAN = p.EAN,
                Shade = p.Shade,
                ImageUrl = p.ImageUrl,
                IsProfessional = p.IsProfessional,
                IsRetail = p.IsRetail,
                Unit = p.Unit,
                PackageSize = p.PackageSize,
                CostPrice = p.CostPrice,
                RetailPrice = p.RetailPrice,
                CurrentStock = p.CurrentStock,
                LowStockThreshold = p.LowStockThreshold
            };
        }
    }
}

