using Soluvion.API.Models.DTOs;

namespace Soluvion.API.Services
{
    public interface ICatalogService
    {
        Task<IEnumerable<CatalogDto>> GetCatalogAsync();
        Task<CatalogDto?> CreateCatalogItemAsync(CatalogDto dto);
        Task<CatalogDto?> UpdateCatalogItemAsync(int id, CatalogDto dto);
        Task<bool> DeleteCatalogItemAsync(int id);
    }
}