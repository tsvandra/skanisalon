using Soluvion.API.DTOs;

namespace Soluvion.API.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<InventoryDocumentDto>> GetDocumentsAsync();
        Task<InventoryDocumentDto?> GetDocumentByIdAsync(int id);
        Task<(bool Success, InventoryDocumentDto? Document, string? Error)> CreateDocumentAsync(CreateInventoryDocumentDto dto);

        /// <summary>A foglalás napi zárásának visszavonása: a készletet visszaírja, és a foglalást újra lezárhatóvá teszi.</summary>
        Task<(bool Success, int ReversedDocuments, string? Error)> ReverseAppointmentClosingAsync(int appointmentId);
    }
}

