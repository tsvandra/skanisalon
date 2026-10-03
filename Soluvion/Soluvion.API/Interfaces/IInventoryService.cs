using Soluvion.API.DTOs;

namespace Soluvion.API.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<InventoryDocumentDto>> GetDocumentsAsync();
        Task<InventoryDocumentDto?> GetDocumentByIdAsync(int id);
        Task<(bool Success, InventoryDocumentDto? Document, string? Error)> CreateDocumentAsync(CreateInventoryDocumentDto dto);
    }
}

