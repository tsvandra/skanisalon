using Soluvion.API.DTOs;

namespace Soluvion.API.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<InventoryDocumentDto>> GetDocumentsAsync();
        Task<InventoryDocumentDto?> GetDocumentByIdAsync(int id);
        Task<ProductHistoryResponseDto?> GetProductHistoryAsync(int productId);
        Task<(bool Success, InventoryDocumentDto? Document, string? Error)> CreateDocumentAsync(CreateInventoryDocumentDto dto);
        Task<bool> UpdateDocumentNoteAsync(int documentId, string? note);
        Task<bool> UpdateDocumentItemNoteAsync(int itemId, string? itemNote);

        /// <summary>A foglalás napi zárásának visszavonása: a készletet visszaírja, és a foglalást újra lezárhatóvá teszi.</summary>
        Task<(bool Success, int ReversedDocuments, string? Error)> ReverseAppointmentClosingAsync(int appointmentId);

        /// <summary>Szállítólevél (dodací list) tételek bevételezése és új termékek automatikus létrehozása tranzakcióban.</summary>
        Task<(bool Success, ImportDeliveryNoteResultDto? Result, string? Error)> ImportDeliveryNoteAsync(ImportDeliveryNoteDto dto);
    }
}


