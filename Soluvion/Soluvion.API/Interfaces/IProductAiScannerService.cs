using Soluvion.API.DTOs;

namespace Soluvion.API.Interfaces
{
    public interface IProductAiScannerService
    {
        Task<ProductAiScanResultDto> ScanProductImagesAsync(IEnumerable<IFormFile> images);
        Task<DeliveryNoteScanResultDto> ScanDeliveryNoteImagesAsync(IEnumerable<IFormFile> images);
    }
}

