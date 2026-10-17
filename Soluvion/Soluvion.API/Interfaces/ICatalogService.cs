using Soluvion.API.DTOs;

namespace Soluvion.API.Interfaces
{
    public interface ICatalogService
    {
        Task<IEnumerable<CatalogDto>> GetCatalogAsync();
        Task<CatalogDto?> CreateCatalogItemAsync(CatalogDto dto);
        Task<CatalogDto?> UpdateCatalogItemAsync(int id, CatalogDto dto);
        Task<bool> DeleteCatalogItemAsync(int id);
        Task<bool> UpdateVariantRecipeAsync(int variantId, List<ServiceVariantProductDto> products);
        Task<GeneratePictogramResponseDto> GeneratePictogramSvgAsync(GeneratePictogramRequestDto dto);
        Task<int> GenerateMissingPictogramsAsync(string? languageCode);
    }
}
