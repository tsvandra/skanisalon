using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryDocumentDto>>> GetAll()
        {
            var docs = await _inventoryService.GetDocumentsAsync();
            return Ok(docs);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryDocumentDto>> GetById(int id)
        {
            var doc = await _inventoryService.GetDocumentByIdAsync(id);
            if (doc == null) return NotFound(new { errorCode = "DOCUMENT_NOT_FOUND" });
            return Ok(doc);
        }

        [HttpGet("product/{productId:int}/history")]
        public async Task<ActionResult<ProductHistoryResponseDto>> GetProductHistory(int productId)
        {
            var history = await _inventoryService.GetProductHistoryAsync(productId);
            if (history == null) return NotFound(new { errorCode = "PRODUCT_NOT_FOUND" });
            return Ok(history);
        }

        [HttpPatch("{documentId:int}/note")]
        public async Task<IActionResult> UpdateDocumentNote(int documentId, [FromBody] UpdateInventoryNoteDto dto)
        {
            var updated = await _inventoryService.UpdateDocumentNoteAsync(documentId, dto?.Note);
            if (!updated) return NotFound(new { errorCode = "DOCUMENT_NOT_FOUND" });
            return Ok(new { documentId, note = dto?.Note });
        }

        [HttpPatch("items/{itemId:int}/note")]
        public async Task<IActionResult> UpdateItemNote(int itemId, [FromBody] UpdateInventoryNoteDto dto)
        {
            var updated = await _inventoryService.UpdateDocumentItemNoteAsync(itemId, dto?.Note);
            if (!updated) return NotFound(new { errorCode = "DOCUMENT_ITEM_NOT_FOUND" });
            return Ok(new { itemId, itemNote = dto?.Note });
        }

        [HttpPost]
        public async Task<ActionResult<InventoryDocumentDto>> Create([FromBody] CreateInventoryDocumentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, document, error) = await _inventoryService.CreateDocumentAsync(dto);
            
            if (!success) return BadRequest(new { Error = error });

            return CreatedAtAction(nameof(GetById), new { id = document!.Id }, document);
        }

        /// <summary>Egy foglalás napi zárásának visszavonása (sztornó).</summary>
        [HttpPost("appointment/{appointmentId:int}/reverse")]
        public async Task<IActionResult> ReverseAppointmentClosing(int appointmentId)
        {
            var (success, reversedDocuments, error) = await _inventoryService.ReverseAppointmentClosingAsync(appointmentId);

            if (!success) return BadRequest(new { Error = error });

            return Ok(new { ReversedDocuments = reversedDocuments });
        }

        /// <summary>Szállítólevél (dodací list) fájlok (kép, PDF, Excel, Word) AI elemzése.</summary>
        [HttpPost("scan-delivery-note")]
        [RequestSizeLimit(35_000_000)]
        public async Task<ActionResult<DeliveryNoteScanResultDto>> ScanDeliveryNote(
            [FromForm] List<IFormFile> images,
            [FromServices] IProductAiScannerService aiScannerService)
        {
            var files = images ?? (Request.Form?.Files?.ToList() ?? new List<IFormFile>());
            if (files.Count == 0)
            {
                return BadRequest(new { errorCode = "DELIVERY_NOTE_FILES_REQUIRED" });
            }

            try
            {
                var result = await aiScannerService.ScanDeliveryNoteFilesAsync(files);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { errorCode = "INVALID_DOCUMENT", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { errorCode = "AI_CONFIGURATION_MISSING", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errorCode = "SCAN_FAILED", message = ex.Message });
            }
        }

        /// <summary>Szállítólevél alapján új termékek létrehozása és készlet bevételezése.</summary>
        [HttpPost("import-delivery-note")]
        public async Task<ActionResult<ImportDeliveryNoteResultDto>> ImportDeliveryNote([FromBody] ImportDeliveryNoteDto dto)
        {
            var (success, result, error) = await _inventoryService.ImportDeliveryNoteAsync(dto);

            if (!success)
            {
                return BadRequest(new { message = error });
            }

            return Ok(result);
        }
    }
}

